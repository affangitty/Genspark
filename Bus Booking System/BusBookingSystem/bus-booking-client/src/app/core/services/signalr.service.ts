import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;

  constructor(private authService: AuthService) { }

  async startConnection(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/seats`, {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    await this.hubConnection.start();
  }

  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  async lockSeat(busId: string, seatId: string, journeyDate: string): Promise<boolean> {
    if (this.hubConnection) {
      return this.hubConnection.invoke('LockSeat', busId, seatId, journeyDate);
    }
    return false;
  }

  async unlockSeat(busId: string, seatId: string, journeyDate: string): Promise<boolean> {
    if (this.hubConnection) {
      return this.hubConnection.invoke('UnlockSeat', busId, seatId, journeyDate);
    }
    return false;
  }

  async extendLock(seatId: string, journeyDate: string, additionalSeconds = 300): Promise<boolean> {
    if (this.hubConnection) {
      return this.hubConnection.invoke('ExtendLock', seatId, journeyDate, additionalSeconds);
    }
    return false;
  }

  async joinBus(busId: string, journeyDate: string): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.invoke('JoinBusGroup', busId, journeyDate);
    }
  }

  async leaveBus(busId: string, journeyDate: string): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.invoke('LeaveBusGroup', busId, journeyDate);
    }
  }

  onSeatLocked(callback: (seatId: string, userId: string) => void): void {
    this.hubConnection?.on('SeatLocked', callback);
  }

  onSeatUnlocked(callback: (seatId: string) => void): void {
    this.hubConnection?.on('SeatUnlocked', callback);
  }
}
