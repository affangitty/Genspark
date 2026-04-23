import { Injectable } from '@angular/core';
import { Bus, Seat } from '../models';

export interface PendingBookingState {
  bus: Bus;
  journeyDate: string;
  selectedSeats: Seat[];
}

@Injectable({ providedIn: 'root' })
export class BookingStateService {
  private pendingBooking: PendingBookingState | null = null;

  setPendingBooking(state: PendingBookingState): void {
    this.pendingBooking = state;
  }

  getPendingBooking(): PendingBookingState | null {
    return this.pendingBooking;
  }

  clear(): void {
    this.pendingBooking = null;
  }
}
