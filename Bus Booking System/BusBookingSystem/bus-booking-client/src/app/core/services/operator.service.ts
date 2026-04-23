import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Bus } from '../models';

@Injectable({ providedIn: 'root' })
export class OperatorService {
  private readonly busApiUrl = `${environment.apiUrl}/bus`;

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
}
