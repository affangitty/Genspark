import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, finalize } from 'rxjs';
import { Bus, Seat } from '../../core/models';
import { BusService } from '../../core/services/bus.service';
import { SignalRService } from '../../core/services/signalr.service';
import { BookingStateService } from '../../core/services/booking-state.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-seat-selection',
  templateUrl: './seat-selection.component.html',
  styleUrl: './seat-selection.component.scss',
  standalone: false
})
export class SeatSelectionComponent implements OnInit, OnDestroy {
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

  async ngOnInit(): Promise<void> {
    this.busId = this.route.snapshot.paramMap.get('busId') ?? '';
    this.journeyDate = this.route.snapshot.queryParamMap.get('journeyDate') ?? '';
    if (!this.busId || !this.journeyDate) {
      this.error = 'Invalid route data.';
      return;
    }

    this.loading = true;
    forkJoin({
      bus: this.busService.getBusDetails(this.busId),
      seats: this.busService.getAvailableSeats(this.busId, this.journeyDate)
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ bus, seats }) => {
          this.bus = bus;
          this.seats = seats;
        },
        error: (err) => {
          const body = err?.error;
          this.error =
            typeof body === 'string'
              ? body
              : body?.detail ?? body?.message ?? err?.message ?? 'Failed to load bus.';
        }
      });

    if (this.authService.isLoggedIn()) {
      await this.signalRService.startConnection();
      await this.signalRService.joinBus(this.busId, this.journeyDate);
      this.signalRService.onSeatLocked((seatId) => {
        this.markSeat(seatId, true);
      });
      this.signalRService.onSeatUnlocked((seatId) => {
        this.markSeat(seatId, false);
      });
    }
  }

  ngOnDestroy(): void {
    if (this.busId && this.journeyDate) {
      this.signalRService.leaveBus(this.busId, this.journeyDate).catch(() => undefined);
    }
  }

  async toggleSeat(seat: Seat): Promise<void> {
    if (!seat.isAvailable && !this.selectedSeats.has(seat.id)) return;

    if (this.selectedSeats.has(seat.id)) {
      this.selectedSeats.delete(seat.id);
      seat.isLocked = false;
      await this.signalRService.unlockSeat(this.busId, seat.id, this.journeyDate);
      return;
    }

    const success = await this.signalRService.lockSeat(this.busId, seat.id, this.journeyDate);
    if (success) {
      this.selectedSeats.add(seat.id);
      seat.isLocked = true;
    }
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
    const seat = this.seats.find((s) => s.id === seatId);
    if (!seat) return;
    seat.isLocked = locked;
    seat.isAvailable = !locked;
  }
}
