import { Component, OnInit } from '@angular/core';
import { forkJoin, finalize } from 'rxjs';
import { Bus, OperatorBookingItem, OperatorBookingSummary } from '../../core/models';
import { OperatorService } from '../../core/services/operator.service';
import { BookingService } from '../../core/services/booking.service';
import { BusService } from '../../core/services/bus.service';

@Component({
  selector: 'app-operator-dashboard',
  templateUrl: './operator-dashboard.component.html',
  styleUrl: './operator-dashboard.component.scss',
  standalone: false
})
export class OperatorDashboardComponent implements OnInit {
  buses: Bus[] = [];
  bookings: OperatorBookingItem[] = [];
  summary: OperatorBookingSummary | null = null;
  routes: Array<{ id: string; sourceCity: string; destinationCity: string }> = [];
  selectedBusId = '';
  selectedRouteId = '';
  loading = false;
  error = '';

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
      operatorBookings: this.bookingService.getOperatorBookings(),
      routes: this.busService.getRoutes()
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ buses, operatorBookings, routes }) => {
          this.buses = buses;
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

  requestRouteAssignment(): void {
    if (!this.selectedBusId || !this.selectedRouteId) return;
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
        next: () => (this.error = ''),
        error: (err) => (this.error = err?.error?.message ?? 'Route assignment request failed.')
      });
  }
}
