import { isPlatformBrowser } from '@angular/common';
import { ApplicationRef, ChangeDetectorRef, Component, NgZone, OnInit, PLATFORM_ID, afterNextRender, inject } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Subscription, forkJoin, finalize, timeout } from 'rxjs';
import { Bus, OperatorBookingItem, OperatorBookingSummary } from '../../core/models';
import { OperatorService } from '../../core/services/operator.service';
import { BookingService } from '../../core/services/booking.service';
import { BusService } from '../../core/services/bus.service';
import { httpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-operator-dashboard',
  templateUrl: './operator-dashboard.component.html',
  styleUrl: './operator-dashboard.component.scss',
  standalone: false
})
export class OperatorDashboardComponent implements OnInit {
  private readonly appRef = inject(ApplicationRef);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly ngZone = inject(NgZone);
  private readonly fb = inject(FormBuilder);
  private readonly platformId = inject(PLATFORM_ID);

  /** Defer `<select>` until after hydration so options bind correctly (SSR + dynamic lists). */
  assignmentSelectsReady = false;

  readonly routeAssignForm: FormGroup = this.fb.group({
    busId: [''],
    routeId: ['']
  });

  buses: Bus[] = [];
  layouts: Array<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number }> = [];
  bookings: OperatorBookingItem[] = [];
  summary: OperatorBookingSummary | null = null;
  routes: Array<{ id: string; sourceCity: string; destinationCity: string }> = [];
  locations: Array<{ id: string; city: string; addressLine: string; landmark?: string; state?: string; pinCode?: string }> = [];
  newLayoutName = '';
  newLayoutRows = 10;
  newLayoutCols = 4;
  newBusNumber = '';
  newBusName = '';
  newBusLayoutId = '';
  newBusFare = 500;
  newLocationCity = '';
  newLocationAddressLine = '';
  newLocationLandmark = '';
  newLocationState = '';
  newLocationPinCode = '';
  loading = false;
  error = '';
  layoutSuccess = '';
  layoutError = '';
  busSuccess = '';
  busError = '';
  locationSuccess = '';
  locationError = '';
  /** Inline feedback under the route-assignment form (errors were easy to miss at the top of the page). */
  routeAssignmentSuccess = '';
  routeAssignmentError = '';
  private loadSub?: Subscription;
  private loadRequestId = 0;

  constructor(
    private operatorService: OperatorService,
    private bookingService: BookingService,
    private busService: BusService
  ) {
    afterNextRender(() => {
      this.assignmentSelectsReady = true;
      this.ngZone.run(() => {
        this.cdr.detectChanges();
        this.scheduleSelectRefresh();
      });

      // Always trigger the first real data load after the first browser render.
      // (Some hydration paths won't run the earlier init timing reliably until the user interacts.)
      if (isPlatformBrowser(this.platformId)) {
        this.loadAll();
      }
    });
  }

  ngOnInit(): void {
    // SSR should not call operator APIs (JWT lives in localStorage). Load in the browser after hydration.
    if (!isPlatformBrowser(this.platformId)) return;
    // Fallback: if afterNextRender is delayed for any reason, still kick off a load on init.
    // Safe because loadAll cancels in-flight requests.
    queueMicrotask(() => this.loadAll());
  }

  loadAll(): void {
    this.loadSub?.unsubscribe();
    const requestId = ++this.loadRequestId;
    this.ngZone.run(() => {
      this.loading = true;
      this.error = '';
      this.cdr.detectChanges();
    });

    const request$ = forkJoin({
      buses: this.operatorService.getMyBuses(),
      layouts: this.operatorService.getMyLayouts(),
      operatorBookings: this.bookingService.getOperatorBookings(),
      routes: this.busService.getRoutes(),
      locations: this.operatorService.getLocations()
    });

    this.loadSub = request$
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.ngZone.run(() => {
            if (requestId !== this.loadRequestId) return;
            // Defer UI flip to the next tick to avoid NG0100 during hydration / fast responses.
            queueMicrotask(() => {
              if (requestId !== this.loadRequestId) return;
              this.loading = false;
              this.cdr.detectChanges();
            });
          });
        })
      )
      .subscribe({
        next: ({ buses, layouts, operatorBookings, routes, locations }) => {
          this.ngZone.run(() => {
            if (requestId !== this.loadRequestId) return;
            // Defer assignments to the next tick to avoid ExpressionChangedAfterItHasBeenCheckedError (NG0100).
            queueMicrotask(() => {
              if (requestId !== this.loadRequestId) return;
              // Remove is a soft-delete (status = Removed). Hide removed buses from the operator list.
              this.buses = (buses ?? []).filter((b) => b.status !== 'Removed');
              this.layouts = layouts ?? [];
              this.summary = operatorBookings.summary;
              this.bookings = operatorBookings.bookings ?? [];
              this.routes = routes ?? [];
              this.locations = locations ?? [];
              this.cdr.detectChanges();
              this.scheduleSelectRefresh();
            });
          });
        },
        error: (err) => {
          this.ngZone.run(() => {
            if (requestId !== this.loadRequestId) return;
            queueMicrotask(() => {
              if (requestId !== this.loadRequestId) return;
              const e = err as { name?: string };
              if (e?.name === 'TimeoutError') {
                this.error = 'Dashboard load timed out. Make sure the API is running on http://localhost:5000.';
              } else {
                this.error = httpErrorMessage(err, 'Failed to load dashboard.');
              }
              this.cdr.detectChanges();
            });
          });
        }
      });
  }

  toggleBusStatus(bus: Bus): void {
    const action$ =
      bus.status === 'TemporarilyUnavailable'
        ? this.operatorService.markBusAvailable(bus.id)
        : this.operatorService.markBusUnavailable(bus.id);
    action$.subscribe({
      next: () => this.loadAll(),
      error: (err) => {
        this.error = httpErrorMessage(err, 'Failed to update bus status.');
        this.ngZone.run(() => {
          this.appRef.tick();
          this.cdr.detectChanges();
        });
      }
    });
  }

  removeBus(busId: string): void {
    if (!window.confirm('Remove this bus permanently?')) return;
    this.operatorService.removeBus(busId).subscribe({
      next: () => this.loadAll(),
      error: (err) => {
        this.error = httpErrorMessage(err, 'Failed to remove bus.');
        this.ngZone.run(() => {
          this.appRef.tick();
          this.cdr.detectChanges();
        });
      }
    });
  }

  private buildLayoutJson(rows: number, cols: number): string {
    const seats: Array<{ seatNumber: string; row: number; column: number; deck: string; type: string }> = [];
    for (let r = 1; r <= rows; r++) {
      for (let c = 1; c <= cols; c++) {
        seats.push({ seatNumber: `R${r}C${c}`, row: r, column: c, deck: 'lower', type: 'Seater' });
      }
    }
    return JSON.stringify(seats);
  }

  createLayout(): void {
    this.layoutSuccess = '';
    this.layoutError = '';
    const rows = Math.max(1, Math.min(50, Math.floor(Number(this.newLayoutRows))));
    const cols = Math.max(1, Math.min(10, Math.floor(Number(this.newLayoutCols))));
    const totalSeats = rows * cols;
    const layoutJson = this.buildLayoutJson(rows, cols);
    const layoutName = (this.newLayoutName || `Layout ${rows}×${cols}`).trim();
    this.operatorService
      .createLayout({
        layoutName,
        totalSeats,
        rows,
        columns: cols,
        hasUpperDeck: false,
        layoutJson
      })
      .subscribe({
        next: (created) => {
          this.ngZone.run(() => {
            this.error = '';
            this.layoutError = '';
            this.layoutSuccess = `Layout created: ${created.layoutName} (${created.totalSeats} seats).`;
            this.newLayoutName = '';
            this.cdr.detectChanges();
            this.loadAll();
          });
        },
        error: (err) =>
          this.ngZone.run(() => {
            const msg = err?.error?.message ?? 'Could not create layout.';
            this.error = msg;
            this.layoutSuccess = '';
            this.layoutError = msg;
            this.cdr.detectChanges();
          })
      });
  }

  createBus(): void {
    this.busSuccess = '';
    this.busError = '';
    if (!this.newBusNumber.trim() || !this.newBusName.trim() || !this.newBusLayoutId) {
      this.error = 'Bus number, bus name, and layout are required.';
      this.busError = this.error;
      return;
    }
    this.operatorService
      .createBus({
        busNumber: this.newBusNumber.trim().toUpperCase(),
        busName: this.newBusName.trim(),
        layoutId: this.newBusLayoutId,
        baseFare: Number(this.newBusFare) || 0
      })
      .subscribe({
        next: (res) => {
          this.ngZone.run(() => {
            this.error = '';
            this.busError = '';
            this.busSuccess = `Bus submitted for admin approval. Bus ID: ${res.busId ?? '(unknown)'}.`;
            this.newBusNumber = '';
            this.newBusName = '';
            this.newBusLayoutId = '';
            this.newBusFare = 500;
            this.cdr.detectChanges();
            this.loadAll();
          });
        },
        error: (err) =>
          this.ngZone.run(() => {
            const msg = err?.error?.message ?? 'Could not register bus.';
            this.error = msg;
            this.busSuccess = '';
            this.busError = msg;
            this.cdr.detectChanges();
          })
      });
  }

  addLocation(): void {
    this.locationSuccess = '';
    this.locationError = '';
    const city = this.newLocationCity.trim();
    const addressLine = this.newLocationAddressLine.trim();
    if (!city || !addressLine) {
      const msg = 'City and address are required.';
      this.locationError = msg;
      this.error = msg;
      return;
    }
    this.operatorService
      .addLocation({
        city,
        addressLine,
        landmark: this.newLocationLandmark.trim() || undefined,
        state: this.newLocationState.trim() || undefined,
        pinCode: this.newLocationPinCode.trim() || undefined
      })
      .subscribe({
        next: () => {
          this.locationSuccess = `Location added for ${city}.`;
          this.newLocationCity = '';
          this.newLocationAddressLine = '';
          this.newLocationLandmark = '';
          this.newLocationState = '';
          this.newLocationPinCode = '';
          this.loadAll();
        },
        error: (err) => {
          const msg = httpErrorMessage(err, 'Could not add location.');
          this.locationError = msg;
          this.error = msg;
        }
      });
  }

  removeLocation(locationId: string): void {
    if (!window.confirm('Remove this location?')) return;
    this.operatorService.deleteLocation(locationId).subscribe({
      next: () => {
        this.locationSuccess = 'Location removed.';
        this.locationError = '';
        this.loadAll();
      },
      error: (err) => {
        const msg = httpErrorMessage(err, 'Could not remove location.');
        this.locationError = msg;
        this.error = msg;
      }
    });
  }

  requestRouteAssignment(): void {
    this.routeAssignmentSuccess = '';
    this.routeAssignmentError = '';
    const busId = (this.routeAssignForm.get('busId')?.value as string | null)?.trim() ?? '';
    const routeId = (this.routeAssignForm.get('routeId')?.value as string | null)?.trim() ?? '';
    if (!busId || !routeId) {
      const msg = 'Select both a bus and a route before submitting.';
      this.error = msg;
      this.routeAssignmentError = msg;
      return;
    }
    this.operatorService
      .requestRouteAssignment({
        busId,
        routeId,
        departureTime: '09:00:00',
        arrivalTime: '13:00:00',
        durationMinutes: 240,
        baseFare: 500
      })
      .subscribe({
        next: () => {
          this.error = '';
          this.routeAssignmentError = '';
          this.routeAssignmentSuccess =
            'Request submitted. An admin must approve it before the route appears on this bus.';
          this.routeAssignForm.patchValue({ busId: '', routeId: '' }, { emitEvent: false });
          this.loadAll();
        },
        error: (err) => {
          const msg = httpErrorMessage(err, 'Route assignment request failed.');
          this.error = msg;
          this.routeAssignmentSuccess = '';
          this.routeAssignmentError = msg;
        }
      });
  }

  /** Native `<select>` + hydration can skip painting options until another control is touched; force a follow-up CD pass. */
  private scheduleSelectRefresh(): void {
    Promise.resolve().then(() =>
      this.ngZone.run(() => {
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      })
    );
    requestAnimationFrame(() =>
      this.ngZone.run(() => {
        this.cdr.detectChanges();
      })
    );
  }
}
