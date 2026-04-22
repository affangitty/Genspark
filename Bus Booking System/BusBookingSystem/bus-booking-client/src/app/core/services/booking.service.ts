import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private apiUrl = `${environment.apiUrl}/booking`;

  constructor(private http: HttpClient) { }

  createBooking(bookingData: any): Observable<any> {
    return this.http.post(this.apiUrl, bookingData);
  }

  getMyBookings(): Observable<any> {
    return this.http.get(`${this.apiUrl}/my-bookings`);
  }

  getBooking(bookingId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${bookingId}`);
  }

  cancelBooking(bookingId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${bookingId}/cancel`, { reason });
  }

  lockSeat(seatId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/lock-seat`, seatId);
  }

  downloadTicket(bookingId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${bookingId}/ticket`);
  }
}
