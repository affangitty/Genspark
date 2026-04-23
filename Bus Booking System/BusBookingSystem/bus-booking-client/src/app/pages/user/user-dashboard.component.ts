import { ApplicationRef, ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription, finalize, timeout } from 'rxjs';
import { Booking } from '../../core/models';
import { BookingService } from '../../core/services/booking.service';
import { UserDashboardReloadService } from '../../core/services/user-dashboard-reload.service';
import { httpErrorMessage } from '../../core/utils/http-error-message';

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.component.html',
  styleUrl: './user-dashboard.component.scss',
  standalone: false
})
export class UserDashboardComponent implements OnInit {
  private readonly appRef = inject(ApplicationRef);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dashboardReload = inject(UserDashboardReloadService);
  private readonly ngZone = inject(NgZone);

  tab: 'upcoming' | 'past' | 'cancelled' = 'upcoming';
  bookings: Booking[] = [];
  loading = false;
  message = '';
  error = '';

  private loadSub?: Subscription;

  constructor(private bookingService: BookingService) {}

  ngOnInit(): void {
    this.dashboardReload.onReload.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  setTab(tab: 'upcoming' | 'past' | 'cancelled'): void {
    this.tab = tab;
    this.load();
  }

  load(): void {
    this.loadSub?.unsubscribe();
    this.loading = true;
    this.error = '';
    this.cdr.detectChanges();

    const request$ =
      this.tab === 'upcoming'
        ? this.bookingService.getUpcomingBookings()
        : this.tab === 'past'
          ? this.bookingService.getPastBookings()
          : this.bookingService.getCancelledBookings();

    this.loadSub = request$
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.ngZone.run(() => {
            this.loading = false;
            this.loadSub = undefined;
            this.appRef.tick();
            this.cdr.detectChanges();
          });
        })
      )
      .subscribe({
        next: (res) => {
          this.bookings = res;
          this.cdr.detectChanges();
        },
        error: (err) => {
          if (err?.name === 'TimeoutError') {
            this.error = 'Request timed out. Check that the API is running on http://localhost:5153.';
            return;
          }
          if (err?.status === 401 || err?.status === 403) {
            this.error = 'You are not signed in as a user, or your session expired. Please sign in again.';
            return;
          }
          this.error = httpErrorMessage(err, 'Failed to load bookings.');
          this.cdr.detectChanges();
        }
      });
  }

  cancel(booking: Booking): void {
    const confirmed = window.confirm('Cancel this booking? Refund depends on cancellation window.');
    if (!confirmed) return;
    this.bookingService.cancelBooking(booking.id, 'Cancelled by user from dashboard').subscribe({
      next: (res) => {
        this.message = `Booking cancelled. Refund: ${res.refundPercentage}% (₹${res.refundAmount})`;
        this.load();
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Cancellation failed.';
        this.cdr.detectChanges();
      }
    });
  }
}
