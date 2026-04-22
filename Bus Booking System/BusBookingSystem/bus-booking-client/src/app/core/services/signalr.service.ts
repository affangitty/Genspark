import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;

  constructor() { }

  startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/seats`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch(err => console.error('Error starting connection: ' + err));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  lockSeat(seatId: string, busId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('LockSeat', seatId, busId).catch(err => console.error(err));
    }
  }

  unlockSeat(seatId: string, busId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('UnlockSeat', seatId, busId).catch(err => console.error(err));
    }
  }

  joinBus(busId: string): void {
    if (this.hubConnection) {
      this.hubConnection.invoke('JoinBus', busId).catch(err => console.error(err));
    }
  }

  onSeatLocked(callback: (seatId: string, userId: string) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on('seatLocked', callback);
    }
  }

  onSeatUnlocked(callback: (seatId: string) => void): void {
    if (this.hubConnection) {
      this.hubConnection.on('seatUnlocked', callback);
    }
  }
}
