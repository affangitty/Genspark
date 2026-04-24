import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectorRef, Component, NgZone, OnDestroy, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, finalize, timeout } from 'rxjs';
import { Bus, Seat } from '../../core/models';
import { BusService } from '../../core/services/bus.service';
import { SignalRService } from '../../core/services/signalr.service';
import { BookingStateService } from '../../core/services/booking-state.service';
import { AuthService } from '../../core/services/auth.service';
import { httpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-seat-selection',
  templateUrl: './seat-selection.component.html',
  styleUrl: './seat-selection.component.scss',
  standalone: false
})
export class SeatSelectionComponent implements OnInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly ngZone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);

  busId = '';
  journeyDate = '';
  bus: Bus | null = null;
  seats: Seat[] = [];
  selectedSeats = new Set<string>();
  loading = false;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private busService: BusService,
    private signalRService: SignalRService,
    private bookingStateService: BookingStateService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.busId = this.route.snapshot.paramMap.get('busId') ?? '';
    this.journeyDate = this.route.snapshot.queryParamMap.get('journeyDate') ?? '';
    if (!this.busId || !this.journeyDate) {
      this.error = 'Invalid route data.';
      return;
    }

    // SSR has no browser APIs / wrong host for API; load only in the browser (same pattern as user dashboard).
    if (!isPlatformBrowser(this.platformId)) {
      this.loading = false;
      return;
    }

    this.loading = true;
    this.cdr.detectChanges();

    forkJoin({
      bus: this.busService.getBusDetails(this.busId),
      seats: this.busService.getAvailableSeats(this.busId, this.journeyDate)
    })
      .pipe(
        timeout(30_000),
        finalize(() => {
          this.ngZone.run(() => {
            this.loading = false;
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: ({ bus, seats }) => {
          this.ngZone.run(() => {
            this.bus = bus;
            this.seats = seats;
            this.cdr.detectChanges();
          });
          void this.connectSignalRIfLoggedIn();
        },
        error: (err: unknown) => {
          this.ngZone.run(() => {
            const e = err as { name?: string };
            if (e?.name === 'TimeoutError') {
              this.error = 'Loading timed out. Is the API running on http://localhost:5153?';
            } else {
              this.error = httpErrorMessage(err, 'Failed to load bus.');
            }
            this.cdr.detectChanges();
          });
        }
      });
  }

  /** After bus + seats are loaded so the page is not blocked on SignalR (negotiation can stall). */
  private async connectSignalRIfLoggedIn(): Promise<void> {
    if (!this.authService.isLoggedIn()) return;
    try {
      await this.signalRService.startConnection();
      await this.signalRService.joinBus(this.busId, this.journeyDate);
      this.signalRService.onSeatLocked((seatId) => {
        this.markSeat(seatId, true);
      });
      this.signalRService.onSeatUnlocked((seatId) => {
        this.markSeat(seatId, false);
      });
      this.ngZone.run(() => this.cdr.detectChanges());
    } catch {
      // Live locks are optional; seat list still works from HTTP.
    }
  }

  ngOnDestroy(): void {
    if (this.busId && this.journeyDate) {
      this.signalRService.leaveBus(this.busId, this.journeyDate).catch(() => undefined);
    }
  }

  async toggleSeat(seat: Seat): Promise<void> {
    if (!seat.isAvailable && !this.selectedSeats.has(seat.id)) return;

    // Seat selection is only meaningful for signed-in users because we rely on seat locks (SignalR)
    // to prevent two users selecting the same seat at once.
    if (!this.authService.isLoggedIn()) {
      this.error = 'Please log in to select seats.';
      this.cdr.detectChanges();
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: this.router.url }
      });
      return;
    }

    // Ensure SignalR is connected before attempting to lock/unlock.
    // Connection can fail on first load; we try again here so the click is not a no-op.
    try {
      await this.signalRService.startConnection();
      await this.signalRService.joinBus(this.busId, this.journeyDate);
    } catch {
      this.error = 'Live seat locking is unavailable. Please refresh and try again.';
      this.cdr.detectChanges();
      return;
    }

    if (this.selectedSeats.has(seat.id)) {
      this.selectedSeats.delete(seat.id);
      seat.isLocked = false;
      const ok = await this.signalRService.unlockSeat(this.busId, seat.id, this.journeyDate);
      if (!ok) {
        this.error = 'Failed to unlock seat. Please refresh and try again.';
      }
      this.cdr.detectChanges();
      return;
    }

    const success = await this.signalRService.lockSeat(this.busId, seat.id, this.journeyDate);
    if (success) {
      this.selectedSeats.add(seat.id);
      seat.isLocked = true;
      this.error = '';
    } else {
      this.error = 'Failed to lock seat (it may have been taken). Please refresh.';
    }
    this.cdr.detectChanges();
  }

  continue(): void {
    if (!this.bus || this.selectedSeats.size === 0) {
      return;
    }
    const seats = this.seats.filter((s) => this.selectedSeats.has(s.id));
    this.bookingStateService.setPendingBooking({
      bus: this.bus,
      journeyDate: this.journeyDate,
      selectedSeats: seats
    });
    this.router.navigate(['/booking/flow']);
  }

  private markSeat(seatId: string, locked: boolean): void {
    this.ngZone.run(() => {
      const seat = this.seats.find((s) => s.id === seatId);
      if (!seat) return;
      seat.isLocked = locked;
      seat.isAvailable = !locked;
      this.cdr.detectChanges();
    });
  }

  isLadiesSeat(seat: Seat): boolean {
    const t = seat.seatType as unknown;
    return t === 'Ladies' || t === 3 || t === '3';
  }
}
