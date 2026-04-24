import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bus } from '../models';

@Injectable({ providedIn: 'root' })
export class OperatorService {
  private readonly busApiUrl = `${environment.apiUrl}/bus`;
  private readonly operatorApiUrl = `${environment.apiUrl}/operator`;

  constructor(private http: HttpClient) {}

  getMyBuses(): Observable<Bus[]> {
    return this.http.get<Bus[]>(`${this.busApiUrl}/my-buses`);
  }

  markBusUnavailable(busId: string): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/${busId}/unavailable`, {});
  }

  markBusAvailable(busId: string): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/${busId}/available`, {});
  }

  removeBus(busId: string): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/${busId}/remove`, {});
  }

  updateFare(busId: string, baseFare: number): Observable<unknown> {
    return this.http.put(`${this.busApiUrl}/${busId}/fare`, { baseFare });
  }

  createLayout(payload: {
    layoutName: string;
    totalSeats: number;
    rows: number;
    columns: number;
    hasUpperDeck: boolean;
    layoutJson: string;
  }): Observable<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number; hasUpperDeck: boolean; layoutJson: string }> {
    return this.http.post<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number; hasUpperDeck: boolean; layoutJson: string }>(
      `${this.busApiUrl}/layouts`,
      payload
    );
  }

  getMyLayouts(): Observable<
    Array<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number; hasUpperDeck: boolean; layoutJson: string }>
  > {
    return this.http.get<Array<{ id: string; layoutName: string; totalSeats: number; rows: number; columns: number; hasUpperDeck: boolean; layoutJson: string }>>(
      `${this.busApiUrl}/layouts`
    );
  }

  createBus(payload: {
    busNumber: string;
    busName: string;
    layoutId: string;
    routeId?: string | null;
    departureTime?: string | null;
    arrivalTime?: string | null;
    baseFare: number;
  }): Observable<{ busId: string }> {
    return this.http.post<{ busId: string }>(`${this.busApiUrl}`, {
      busNumber: payload.busNumber,
      busName: payload.busName,
      layoutId: payload.layoutId,
      routeId: payload.routeId ?? null,
      departureTime: payload.departureTime ?? null,
      arrivalTime: payload.arrivalTime ?? null,
      baseFare: payload.baseFare
    });
  }

  requestRouteAssignment(payload: {
    busId: string;
    routeId: string;
    departureTime: string;
    arrivalTime: string;
    durationMinutes: number;
    baseFare: number;
  }): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/${payload.busId}/assign-route`, {
      routeId: payload.routeId,
      departureTime: payload.departureTime,
      arrivalTime: payload.arrivalTime,
      durationMinutes: payload.durationMinutes,
      baseFare: payload.baseFare
    });
  }

  getLocations(): Observable<Array<{ id: string; city: string; addressLine: string; landmark?: string; state?: string; pinCode?: string }>> {
    return this.http.get<Array<{ id: string; city: string; addressLine: string; landmark?: string; state?: string; pinCode?: string }>>(
      `${this.operatorApiUrl}/locations`
    );
  }

  addLocation(payload: { city: string; addressLine: string; landmark?: string; state?: string; pinCode?: string }): Observable<unknown> {
    return this.http.post(`${this.operatorApiUrl}/locations`, payload);
  }

  deleteLocation(locationId: string): Observable<unknown> {
    return this.http.delete(`${this.operatorApiUrl}/locations/${locationId}`);
  }
}
