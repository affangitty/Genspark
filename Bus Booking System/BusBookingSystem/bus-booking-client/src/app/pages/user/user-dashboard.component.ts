import { ApplicationRef, Component, OnInit, inject } from '@angular/core';
import { finalize, timeout } from 'rxjs';
import { Booking } from '../../core/models';
import { BookingService } from '../../core/services/booking.service';

@Component({
  selector: 'app-user-dashboard',
  templateUrl: './user-dashboard.component.html',
  styleUrl: './user-dashboard.component.scss',
  standalone: false
})
export class UserDashboardComponent implements OnInit {
  private readonly appRef = inject(ApplicationRef);
  tab: 'upcoming' | 'past' | 'cancelled' = 'upcoming';
  bookings: Booking[] = [];
  loading = false;
  message = '';
  error = '';

  constructor(private bookingService: BookingService) {}

  ngOnInit(): void {
    this.load();
  }

  setTab(tab: 'upcoming' | 'past' | 'cancelled'): void {
    this.tab = tab;
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = '';
    const request$ =
      this.tab === 'upcoming'
        ? this.bookingService.getUpcomingBookings()
        : this.tab === 'past'
        ? this.bookingService.getPastBookings()
        : this.bookingService.getCancelledBookings();

    request$
      .pipe(
        timeout(25_000),
        finalize(() => {
          this.loading = false;
          this.appRef.tick();
        })
      )
      .subscribe({
        next: (res) => (this.bookings = res),
        error: (err) => {
          if (err?.name === 'TimeoutError') {
            this.error = 'Request timed out. Check that the API is running on http://localhost:5153.';
            return;
          }
          this.error = err?.error?.detail ?? err?.error?.message ?? 'Failed to load bookings.';
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
      }
    });
  }
}
