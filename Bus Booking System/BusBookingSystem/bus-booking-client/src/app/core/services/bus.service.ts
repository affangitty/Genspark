import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BusService {
  private apiUrl = `${environment.apiUrl}/bus`;

  constructor(private http: HttpClient) { }

  searchBuses(source: string, destination: string, date: Date): Observable<any> {
    return this.http.post(`${this.apiUrl}/search`, {
      sourceCity: source,
      destinationCity: destination,
      departureDate: date
    });
  }

  getBusDetails(busId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/${busId}`);
  }

  getAvailableSeats(busId: string, date: Date): Observable<any> {
    return this.http.get(`${this.apiUrl}/${busId}/seats`, {
      params: { date: date.toISOString() }
    });
  }
}
