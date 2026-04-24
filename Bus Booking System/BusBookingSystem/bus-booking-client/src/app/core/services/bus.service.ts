import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bus, Seat } from '../models';

@Injectable({
  providedIn: 'root'
})
export class BusService {
  private readonly apiUrl = `${environment.apiUrl}/bus`;
  private readonly routeApiUrl = `${environment.apiUrl}/route`;

  constructor(private http: HttpClient) { }

  searchBuses(
    source: string,
    destination: string,
    date: string,
    passengerCount?: number | string | null
  ): Observable<Bus[]> {
    const raw = passengerCount ?? null;
    const n = raw === null || raw === '' ? NaN : Number(raw);
    const passengerCountBody = Number.isFinite(n) && n >= 1 ? n : null;
    return this.http.post<Bus[]>(`${this.apiUrl}/search`, {
      sourceCity: source.trim(),
      destinationCity: destination.trim(),
      journeyDate: date,
      passengerCount: passengerCountBody
    });
  }

  getBusDetails(busId: string): Observable<Bus> {
    return this.http.get<Bus>(`${this.apiUrl}/${busId}`);
  }

  getAvailableSeats(busId: string, date: string): Observable<Seat[]> {
    return this.http.get<Seat[]>(`${this.apiUrl}/${busId}/seats`, {
      params: { journeyDate: date }
    });
  }

  getRoutes(): Observable<Array<{ id: string; sourceCity: string; destinationCity: string }>> {
    return this.http.get<Array<{ id: string; sourceCity: string; destinationCity: string }>>(this.routeApiUrl);
  }

  getSourceCities(): Observable<Array<{ sourceCity: string; sourceState: string }>> {
    return this.http.get<Array<{ sourceCity: string; sourceState: string }>>(`${this.routeApiUrl}/cities/source`);
  }

  getDestinationCities(sourceCity: string): Observable<Array<{ destinationCity: string; destinationState: string }>> {
    return this.http.get<Array<{ destinationCity: string; destinationState: string }>>(`${this.routeApiUrl}/cities/destination`, {
      params: { sourceCity }
    });
  }
}
