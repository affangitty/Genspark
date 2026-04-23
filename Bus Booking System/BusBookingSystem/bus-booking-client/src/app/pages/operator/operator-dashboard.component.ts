import { Component, OnInit } from '@angular/core';
import { forkJoin, finalize } from 'rxjs';
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
  buses: Bus[] = [];
  layouts: Array<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number }> = [];
  bookings: OperatorBookingItem[] = [];
  summary: OperatorBookingSummary | null = null;
  routes: Array<{ id: string; sourceCity: string; destinationCity: string }> = [];
  selectedBusId = '';
  selectedRouteId = '';
  newLayoutName = '';
  newLayoutRows = 10;
  newLayoutCols = 4;
  newBusNumber = '';
  newBusName = '';
  newBusLayoutId = '';
  newBusFare = 500;
  loading = false;
  error = '';
  /** Inline feedback under the route-assignment form (errors were easy to miss at the top of the page). */
  routeAssignmentSuccess = '';
  routeAssignmentError = '';

  constructor(
    private operatorService: OperatorService,
    private bookingService: BookingService,
    private busService: BusService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading = true;
    this.error = '';
    forkJoin({
      buses: this.operatorService.getMyBuses(),
      layouts: this.operatorService.getMyLayouts(),
      operatorBookings: this.bookingService.getOperatorBookings(),
      routes: this.busService.getRoutes()
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ buses, layouts, operatorBookings, routes }) => {
          this.buses = buses;
          this.layouts = layouts;
          this.summary = operatorBookings.summary;
          this.bookings = operatorBookings.bookings;
          this.routes = routes;
        },
        error: (err) => {
          const body = err?.error;
          this.error =
            typeof body === 'string'
              ? body
              : body?.detail ?? body?.message ?? err?.message ?? 'Failed to load dashboard.';
        }
      });
  }

  toggleBusStatus(bus: Bus): void {
    const action$ =
      bus.status === 'TemporarilyUnavailable'
        ? this.operatorService.markBusAvailable(bus.id)
        : this.operatorService.markBusUnavailable(bus.id);
    action$.subscribe(() => this.loadAll());
  }

  removeBus(busId: string): void {
    if (!window.confirm('Remove this bus permanently?')) return;
    this.operatorService.removeBus(busId).subscribe(() => this.loadAll());
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
        next: () => {
          this.error = '';
          this.newLayoutName = '';
          this.loadAll();
        },
        error: (err) => (this.error = err?.error?.message ?? 'Could not create layout.')
      });
  }

  createBus(): void {
    if (!this.newBusNumber.trim() || !this.newBusName.trim() || !this.newBusLayoutId) {
      this.error = 'Bus number, bus name, and layout are required.';
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
        next: () => {
          this.error = '';
          this.newBusNumber = '';
          this.newBusName = '';
          this.newBusLayoutId = '';
          this.newBusFare = 500;
          this.loadAll();
        },
        error: (err) => (this.error = err?.error?.message ?? 'Could not register bus.')
      });
  }

  requestRouteAssignment(): void {
    this.routeAssignmentSuccess = '';
    this.routeAssignmentError = '';
    if (!this.selectedBusId?.trim() || !this.selectedRouteId?.trim()) {
      const msg = 'Select both a bus and a route before submitting.';
      this.error = msg;
      this.routeAssignmentError = msg;
      return;
    }
    this.operatorService
      .requestRouteAssignment({
        busId: this.selectedBusId,
        routeId: this.selectedRouteId,
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
          this.selectedBusId = '';
          this.selectedRouteId = '';
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
}
