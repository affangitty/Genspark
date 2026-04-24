import { isPlatformBrowser } from '@angular/common';
import { ApplicationRef, ChangeDetectorRef, Component, DestroyRef, NgZone, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subscription, catchError, finalize, timeout } from 'rxjs';
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
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dashboardReload = inject(UserDashboardReloadService);
  private readonly platformId = inject(PLATFORM_ID);

  tab: 'upcoming' | 'past' | 'cancelled' = 'upcoming';
  bookings: Booking[] = [];
  loading = false;
  message = '';
  error = '';

  private loadSub?: Subscription;
  private loadingWatchdog?: number;

  constructor(private bookingService: BookingService) {}

  ngOnInit(): void {
    // SSR has no localStorage JWT; calling the API from Node can hang or 401. Load only in the browser.
    if (!isPlatformBrowser(this.platformId)) {
      this.loading = false;
      return;
    }
    this.dashboardReload.onReload.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());
    this.load();
  }

  setTab(tab: 'upcoming' | 'past' | 'cancelled'): void {
    this.tab = tab;
    this.load();
  }

  load(): void {
    try {
      this.loadSub?.unsubscribe();
      this.loading = true;
      this.error = '';
      this.cdr.detectChanges();

      // Fail-safe: if the browser request hangs, never spin forever.
      const t = globalThis.setTimeout?.bind(globalThis) ?? setTimeout;
      const clearT = globalThis.clearTimeout?.bind(globalThis) ?? clearTimeout;
      if (this.loadingWatchdog) {
        clearT(this.loadingWatchdog);
      }
      this.loadingWatchdog = t(() => {
        if (!this.loading) return;
        this.loading = false;
        this.error = 'Loading bookings is taking too long. Please refresh the page.';
        this.cdr.detectChanges();
      }, 35_000) as unknown as number;

      const request$ =
        this.tab === 'upcoming'
          ? this.bookingService.getUpcomingBookings()
          : this.tab === 'past'
            ? this.bookingService.getPastBookings()
            : this.bookingService.getCancelledBookings();

      this.loadSub = request$
        .pipe(
          timeout(25_000),
          catchError((err) => {
            if (err?.name === 'TimeoutError') {
              this.error = 'Request timed out. Check that the API is running.';
              this.cdr.detectChanges();
              return EMPTY;
            }
            if (err?.status === 401 || err?.status === 403) {
              this.error = 'You are not signed in as a user, or your session expired. Please sign in again.';
              this.cdr.detectChanges();
              return EMPTY;
            }
            this.error = httpErrorMessage(err, 'Failed to load bookings.');
            this.cdr.detectChanges();
            return EMPTY;
          }),
          finalize(() => {
            if (this.loadingWatchdog) {
              clearT(this.loadingWatchdog);
              this.loadingWatchdog = undefined;
            }
            this.loading = false;
            this.loadSub = undefined;
            this.cdr.detectChanges();
          })
        )
        .subscribe((res) => {
          this.bookings = res;
          this.cdr.detectChanges();
        });
    } catch (e) {
      this.loading = false;
      this.error = (e as any)?.message ?? 'Failed to load bookings.';
      this.cdr.detectChanges();
    }
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

  downloadTicket(booking: Booking): void {
    this.error = '';
    this.bookingService.downloadTicket(booking.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Ticket_${booking.bookingReference}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Failed to download ticket.';
        this.cdr.detectChanges();
      }
    });
  }
}
