import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Booking,
  CancellationResponse,
  OperatorBookingItem,
  OperatorBookingSummary,
  PassengerRequest
} from '../models';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private readonly apiUrl = `${environment.apiUrl}/booking`;

  constructor(private http: HttpClient) { }

  createBooking(bookingData: { busId: string; journeyDate: string; passengers: PassengerRequest[] }): Observable<Booking> {
    return this.http.post<Booking>(`${this.apiUrl}/create`, bookingData);
  }

  getUpcomingBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/history/upcoming`);
  }

  getPastBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/history/past`);
  }

  getCancelledBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/history/cancelled`);
  }

  getBooking(bookingId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${bookingId}`);
  }

  cancelBooking(bookingId: string, reason: string): Observable<CancellationResponse> {
    return this.http.post<CancellationResponse>(`${this.apiUrl}/${bookingId}/cancel`, { reason });
  }

  lockSeat(seatId: string, busId: string, journeyDate: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/lock-seat`, { seatId, busId, journeyDate });
  }

  unlockSeat(seatId: string, busId: string, journeyDate: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/unlock-seat`, { seatId, busId, journeyDate });
  }

  extendLock(seatId: string, journeyDate: string, additionalSeconds = 300): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/extend-lock`, null, {
      params: { seatId, journeyDate, additionalSeconds }
    });
  }

  getBookedSeats(busId: string, journeyDate: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/bus/${busId}/booked-seats`, {
      params: { journeyDate }
    });
  }

  getOperatorBookings(): Observable<{ summary: OperatorBookingSummary; bookings: OperatorBookingItem[] }> {
    return this.http.get<{ summary: OperatorBookingSummary; bookings: OperatorBookingItem[] }>(`${this.apiUrl}/operator`);
  }

  downloadTicket(bookingId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${bookingId}/ticket`, { responseType: 'blob' });
  }
}
