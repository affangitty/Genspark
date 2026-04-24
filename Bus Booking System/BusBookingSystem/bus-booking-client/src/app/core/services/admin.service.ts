import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminApprovalQueueItem, AdminRevenueDashboard } from '../models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly apiUrl = `${environment.apiUrl}/admin`;
  private readonly busApiUrl = `${environment.apiUrl}/bus`;
  private readonly routeApiUrl = `${environment.apiUrl}/route`;

  constructor(private http: HttpClient) {}

  getApprovalQueue(): Observable<{
    totalPending: number;
    pendingOperators: number;
    pendingBuses: number;
    pendingRouteAssignments: number;
    items: AdminApprovalQueueItem[];
  }> {
    return this.http.get<{
      totalPending: number;
      pendingOperators: number;
      pendingBuses: number;
      pendingRouteAssignments: number;
      items: AdminApprovalQueueItem[];
    }>(`${this.apiUrl}/approvals/queue`);
  }

  approveOperator(operatorId: string, isApproved: boolean, rejectionReason?: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/operators/${operatorId}/approve`, {
      isApproved,
      rejectionReason
    });
  }

  approveBus(busId: string, isApproved: boolean, adminNotes?: string): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/${busId}/approve`, {
      isApproved,
      adminNotes
    });
  }

  approveRouteAssignment(assignmentId: string, isApproved: boolean, adminNotes?: string): Observable<unknown> {
    return this.http.post(`${this.busApiUrl}/route-assignments/${assignmentId}/approve`, {
      isApproved,
      adminNotes
    });
  }

  getApprovedRouteAssignments(): Observable<
    Array<{
      id: string;
      busId: string;
      busNumber: string;
      busName: string;
      busStatus: string;
      seatCount: number;
      operatorId: string;
      operatorName: string;
      operatorStatus: string;
      routeId: string;
      sourceCity: string;
      destinationCity: string;
      departureTime: string;
      arrivalTime: string;
      durationMinutes: number;
      baseFare: number;
      reviewedAt: string | null;
      adminNotes: string | null;
    }>
  > {
    return this.http.get<
      Array<{
        id: string;
        busId: string;
        busNumber: string;
        busName: string;
        busStatus: string;
        seatCount: number;
        operatorId: string;
        operatorName: string;
        operatorStatus: string;
        routeId: string;
        sourceCity: string;
        destinationCity: string;
        departureTime: string;
        arrivalTime: string;
        durationMinutes: number;
        baseFare: number;
        reviewedAt: string | null;
        adminNotes: string | null;
      }>
    >(`${this.busApiUrl}/route-assignments/approved`);
  }

  getRevenueDashboard(): Observable<AdminRevenueDashboard> {
    return this.http.get<AdminRevenueDashboard>(`${this.apiUrl}/revenue-dashboard`);
  }

  getPlatformConfig(): Observable<{
    convenienceFeePercentage: number;
    useFlatConvenienceFee: boolean;
    flatConvenienceFeePerPassenger: number;
    seatLockDurationMinutes: number;
  }> {
    return this.http.get<{
      convenienceFeePercentage: number;
      useFlatConvenienceFee: boolean;
      flatConvenienceFeePerPassenger: number;
      seatLockDurationMinutes: number;
    }>(`${this.apiUrl}/platform-config`);
  }

  updatePlatformConfig(payload: {
    convenienceFeePercentage: number;
    useFlatConvenienceFee: boolean;
    flatConvenienceFeePerPassenger: number;
    seatLockDurationMinutes: number;
  }): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/platform-config`, payload);
  }

  getRoutes(): Observable<Array<{ id: string; sourceCity: string; destinationCity: string; sourceState: string; destinationState: string; isActive: boolean }>> {
    return this.http.get<Array<{ id: string; sourceCity: string; destinationCity: string; sourceState: string; destinationState: string; isActive: boolean }>>(
      `${this.routeApiUrl}/all`
    );
  }

  createRoute(payload: { sourceCity: string; destinationCity: string; sourceState: string; destinationState: string }): Observable<unknown> {
    return this.http.post(this.routeApiUrl, payload);
  }
}
