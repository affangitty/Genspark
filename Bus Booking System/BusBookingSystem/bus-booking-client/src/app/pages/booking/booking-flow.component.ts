import { Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { BookingStateService } from '../../core/services/booking-state.service';
import { BookingService } from '../../core/services/booking.service';
import { PassengerRequest } from '../../core/models';

@Component({
  selector: 'app-booking-flow',
  templateUrl: './booking-flow.component.html',
  styleUrl: './booking-flow.component.scss',
  standalone: false
})
export class BookingFlowComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  loading = false;
  error = '';
  paymentSuccess = false;

  form = this.fb.group({
    passengers: this.fb.array([])
  });

  constructor(
    private bookingStateService: BookingStateService,
    private bookingService: BookingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const state = this.bookingStateService.getPendingBooking();
    if (!state) {
      this.router.navigate(['/']);
      return;
    }

    const array = this.form.get('passengers') as FormArray;
    state.selectedSeats.forEach(() => {
      array.push(
        this.fb.group({
          passengerName: ['', Validators.required],
          age: [18, [Validators.required, Validators.min(1)]],
          gender: ['Male', Validators.required]
        })
      );
    });
  }

  get passengers(): FormArray {
    return this.form.get('passengers') as FormArray;
  }

  get totalAmount(): number {
    const state = this.bookingStateService.getPendingBooking();
    if (!state) return 0;
    return state.selectedSeats.length * state.bus.totalFare;
  }

  payAndBook(): void {
    const state = this.bookingStateService.getPendingBooking();
    if (!state || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';

    const passengers: PassengerRequest[] = state.selectedSeats.map((seat, index) => {
      const formValue = this.passengers.at(index).getRawValue();
      return {
        passengerName: formValue.passengerName ?? '',
        age: Number(formValue.age),
        gender: formValue.gender ?? 'Male',
        seatId: seat.id
      };
    });

    this.bookingService
      .createBooking({
        busId: state.bus.id,
        journeyDate: state.journeyDate,
        passengers
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: () => {
          this.paymentSuccess = true;
          this.bookingStateService.clear();
          setTimeout(() => this.router.navigate(['/user/dashboard']), 1200);
        },
        error: (err) => {
          const body = err?.error;
          this.error =
            typeof body === 'string'
              ? body
              : body?.detail ?? body?.message ?? err?.message ?? 'Booking failed.';
        }
      });
  }
}
