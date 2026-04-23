import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

/**
 * Example Angular Service for Phase 4 - Search & Booking
 * This service demonstrates how to interact with the Bus Booking API
 */

export interface BusSearchRequest {
  sourceCity: string;
  destinationCity: string;
  journeyDate: Date;
  passengerCount?: number;
}

export interface BusResponse {
  id: string;
  busNumber: string;
  busName: string;
  operatorName: string;
  sourceCity: string;
  destinationCity: string;
  departureTime: string;
  arrivalTime: string;
  totalSeats: number;
  availableSeats: number;
  baseFare: number;
  convenienceFee: number;
  totalFare: number;
  status: string;
}

export interface PassengerDto {
  passengerName: string;
  age: number;
  gender: string;
  seatId: string;
}

export interface CreateBookingRequest {
  busId: string;
  journeyDate: Date;
  passengers: PassengerDto[];
}

export interface SeatDto {
  id: string;
  seatNumber: string;
  row: number;
  column: number;
  deck: string;
  seatType: string;
  isAvailable: boolean;
  isLocked: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BusBookingService {
  private apiUrl = 'http://localhost:5000/api';
  private hubConnection: HubConnection | null = null;

  // SignalR Observables
  seatLockedSubject = new Subject<{ seatId: string; userId: string }>();
  seatUnlockedSubject = new Subject<{ seatId: string }>();
  availableSeatsUpdatedSubject = new Subject<string[]>();

  constructor(private http: HttpClient) {
    this.initializeSignalR();
  }

  // ── TASK 4.1: BUS SEARCH ──────────────────────────────────────

  /**
   * Search available buses with fuzzy location matching
   * @param request Search criteria
   * @returns Observable of available buses
   */
  searchBuses(request: BusSearchRequest): Observable<BusResponse[]> {
    return this.http.post<BusResponse[]>(
      `${this.apiUrl}/bus/search`,
      {
        ...request,
        journeyDate: request.journeyDate.toISOString().split('T')[0]
      }
    );
  }

  // ── TASK 4.2: SEAT LOCKING ───────────────────────────────────

  /**
   * Initialize SignalR connection for real-time seat updates
   */
  private initializeSignalR(): void {
    const token = localStorage.getItem('token');
    
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${this.apiUrl.replace('/api', '')}/hubs/seats`, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('SeatLocked', (seatId: string, userId: string) => {
      this.seatLockedSubject.next({ seatId, userId });
    });

    this.hubConnection.on('SeatUnlocked', (seatId: string) => {
      this.seatUnlockedSubject.next({ seatId });
    });

    this.hubConnection.on('AvailableSeatsUpdated', (seatIds: string[]) => {
      this.availableSeatsUpdatedSubject.next(seatIds);
    });

    this.hubConnection.start().catch(err => console.error('SignalR Error:', err));
  }

  /**
   * Lock a seat for the current user
   * @param busId Bus ID
   * @param seatId Seat ID to lock
   * @param journeyDate Journey date
   * @returns Promise that resolves to success status
   */
  async lockSeat(busId: string, seatId: string, journeyDate: Date): Promise<boolean> {
    if (!this.hubConnection) {
      throw new Error('SignalR connection not established');
    }

    try {
      const result = await this.hubConnection.invoke(
        'LockSeat',
        busId,
        seatId,
        journeyDate
      );
      return result;
    } catch (error) {
      console.error('Error locking seat:', error);
      return false;
    }
  }

  /**
   * Unlock a previously locked seat
   * @param busId Bus ID
   * @param seatId Seat ID to unlock
   * @param journeyDate Journey date
   * @returns Promise that resolves to success status
   */
  async unlockSeat(busId: string, seatId: string, journeyDate: Date): Promise<boolean> {
    if (!this.hubConnection) {
      throw new Error('SignalR connection not established');
    }

    try {
      const result = await this.hubConnection.invoke(
        'UnlockSeat',
        busId,
        seatId,
        journeyDate
      );
      return result;
    } catch (error) {
      console.error('Error unlocking seat:', error);
      return false;
    }
  }

  /**
   * Extend seat lock duration
   * @param seatId Seat ID
   * @param additionalSeconds Additional lock duration in seconds
   * @returns Promise that resolves to success status
   */
  async extendLock(seatId: string, additionalSeconds: number = 300): Promise<boolean> {
    if (!this.hubConnection) {
      throw new Error('SignalR connection not established');
    }

    try {
      const result = await this.hubConnection.invoke(
        'ExtendLock',
        seatId,
        additionalSeconds
      );
      return result;
    } catch (error) {
      console.error('Error extending lock:', error);
      return false;
    }
  }

  /**
   * Join a bus viewing group for real-time updates
   * @param busId Bus ID
   * @param journeyDate Journey date
   */
  async joinBusGroup(busId: string, journeyDate: Date): Promise<void> {
    if (!this.hubConnection) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.hubConnection.invoke('JoinBusGroup', busId, journeyDate);
    } catch (error) {
      console.error('Error joining bus group:', error);
    }
  }

  /**
   * Leave a bus viewing group
   * @param busId Bus ID
   * @param journeyDate Journey date
   */
  async leaveBusGroup(busId: string, journeyDate: Date): Promise<void> {
    if (!this.hubConnection) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.hubConnection.invoke('LeaveBusGroup', busId, journeyDate);
    } catch (error) {
      console.error('Error leaving bus group:', error);
    }
  }

  /**
   * Get available seats for a bus on a specific date
   * @param busId Bus ID
   * @param journeyDate Journey date
   * @returns Observable of seat details
   */
  getBusSeats(busId: string, journeyDate: Date): Observable<SeatDto[]> {
    const dateStr = journeyDate.toISOString().split('T')[0];
    return this.http.get<SeatDto[]>(
      `${this.apiUrl}/bus/${busId}/seats?journeyDate=${dateStr}`
    );
  }

  // ── TASK 4.3: BOOKING ─────────────────────────────────────────

  /**
   * Create a new booking with multiple passengers
   * @param request Booking request with passengers and seats
   * @returns Observable of booking confirmation
   */
  createBooking(request: CreateBookingRequest): Observable<any> {
    return this.http.post(
      `${this.apiUrl}/booking/create`,
      {
        ...request,
        journeyDate: request.journeyDate.toISOString()
      }
    );
  }

  /**
   * Lock a seat (via REST API)
   * @param seatId Seat ID
   * @param busId Bus ID
   * @param journeyDate Journey date
   * @returns Observable of lock result
   */
  lockSeatRest(seatId: string, busId: string, journeyDate: Date): Observable<boolean> {
    return this.http.post<boolean>(
      `${this.apiUrl}/booking/lock-seat`,
      {
        seatId,
        busId,
        journeyDate: journeyDate.toISOString()
      }
    );
  }

  /**
   * Unlock a seat (via REST API)
   * @param seatId Seat ID
   * @param busId Bus ID
   * @param journeyDate Journey date
   * @returns Observable of unlock result
   */
  unlockSeatRest(seatId: string, busId: string, journeyDate: Date): Observable<boolean> {
    return this.http.post<boolean>(
      `${this.apiUrl}/booking/unlock-seat`,
      {
        seatId,
        busId,
        journeyDate: journeyDate.toISOString()
      }
    );
  }

  /**
   * Extend seat lock (via REST API)
   * @param seatId Seat ID
   * @param additionalSeconds Additional duration in seconds
   * @returns Observable of extend result
   */
  extendLockRest(seatId: string, additionalSeconds: number = 300): Observable<boolean> {
    return this.http.post<boolean>(
      `${this.apiUrl}/booking/extend-lock?seatId=${seatId}&additionalSeconds=${additionalSeconds}`,
      {}
    );
  }

  // ── TASK 4.5: TICKET GENERATION ───────────────────────────────

  /**
   * Download ticket PDF for a booking
   * @param bookingId Booking ID
   * @returns Observable of PDF blob
   */
  downloadTicket(bookingId: string): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/booking/${bookingId}/ticket`,
      { responseType: 'blob' }
    );
  }

  /**
   * Download ticket and open in browser
   * @param bookingId Booking ID
   */
  viewTicket(bookingId: string): void {
    this.downloadTicket(bookingId).subscribe(
      (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url);
      },
      error => console.error('Error downloading ticket:', error)
    );
  }

  /**
   * Download ticket as file
   * @param bookingId Booking ID
   * @param bookingReference Booking reference for filename
   */
  downloadTicketFile(bookingId: string, bookingReference: string): void {
    this.downloadTicket(bookingId).subscribe(
      (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Ticket_${bookingReference}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error => console.error('Error downloading ticket:', error)
    );
  }

  // ── CLEANUP ───────────────────────────────────────────────────

  /**
   * Clean up SignalR connection
   */
  disconnect(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }
}

/**
 * Example Component Usage
 */
export class BusSearchComponent {
  buses: BusResponse[] = [];
  selectedSeats: Map<string, SeatDto> = new Map();
  passengers: PassengerDto[] = [];

  constructor(
    private busService: BusBookingService
  ) {}

  // Search buses
  searchBuses(source: string, destination: string, date: Date, count: number): void {
    const request: BusSearchRequest = {
      sourceCity: source,
      destinationCity: destination,
      journeyDate: date,
      passengerCount: count
    };

    this.busService.searchBuses(request).subscribe(
      (buses) => {
        this.buses = buses;
        console.log('Found buses:', buses);
      },
      (error) => {
        console.error('Search error:', error);
      }
    );
  }

  // Get bus seats and subscribe to updates
  viewBusSeats(busId: string, journeyDate: Date): void {
    // Load initial seats
    this.busService.getBusSeats(busId, journeyDate).subscribe(
      (seats) => {
        console.log('Available seats:', seats);
      }
    );

    // Join SignalR group
    this.busService.joinBusGroup(busId, journeyDate);

    // Subscribe to real-time updates
    this.busService.seatLockedSubject.subscribe((event) => {
      console.log(`Seat ${event.seatId} locked by user ${event.userId}`);
      // Update UI to mark seat as locked
    });

    this.busService.seatUnlockedSubject.subscribe((event) => {
      console.log(`Seat ${event.seatId} unlocked`);
      // Update UI to mark seat as available
    });
  }

  // Lock seat when user clicks it
  async selectSeat(seat: SeatDto, busId: string, journeyDate: Date): Promise<void> {
    const locked = await this.busService.lockSeat(busId, seat.id, journeyDate);
    
    if (locked) {
      this.selectedSeats.set(seat.id, seat);
      console.log(`Seat ${seat.seatNumber} locked for 10 minutes`);
    } else {
      console.error('Could not lock seat - it might be taken');
    }
  }

  // Deselect seat
  async deselectSeat(seatId: string, busId: string, journeyDate: Date): Promise<void> {
    const unlocked = await this.busService.unlockSeat(busId, seatId, journeyDate);
    
    if (unlocked) {
      this.selectedSeats.delete(seatId);
      console.log('Seat unlocked');
    }
  }

  // Create booking
  async createBooking(busId: string, journeyDate: Date): Promise<void> {
    if (this.passengers.length === 0 || this.selectedSeats.size === 0) {
      console.error('Please add passengers and select seats');
      return;
    }

    const request: CreateBookingRequest = {
      busId,
      journeyDate,
      passengers: this.passengers
    };

    this.busService.createBooking(request).subscribe(
      (bookingResponse) => {
        console.log('Booking confirmed!', bookingResponse);
        console.log('Booking Reference:', bookingResponse.bookingReference);
        
        // Download ticket
        this.busService.downloadTicketFile(
          bookingResponse.id,
          bookingResponse.bookingReference
        );
        
        // Clear selections
        this.selectedSeats.clear();
        this.passengers = [];
      },
      (error) => {
        console.error('Booking error:', error);
      }
    );
  }
}

/**
 * Example Seat Selection Component
 */
export class SeatSelectionComponent {
  seats: SeatDto[][] = []; // 2D array for seat layout
  selectedSeatIds: Set<string> = new Set();

  constructor(
    private busService: BusBookingService
  ) {}

  // Load seats and arrange in grid
  loadSeats(busId: string, journeyDate: Date, rows: number, cols: number): void {
    this.busService.getBusSeats(busId, journeyDate).subscribe(
      (seatsFlat) => {
        // Arrange seats in 2D grid for UI display
        this.seats = [];
        for (let row = 0; row < rows; row++) {
          this.seats[row] = [];
          for (let col = 0; col < cols; col++) {
            const index = row * cols + col;
            this.seats[row][col] = seatsFlat[index] || null;
          }
        }
      }
    );
  }

  // Click seat
  async onSeatClick(seat: SeatDto, busId: string, journeyDate: Date): Promise<void> {
    if (!seat.isAvailable) {
      console.log('Seat is not available');
      return;
    }

    if (this.selectedSeatIds.has(seat.id)) {
      // Deselect
      await this.busService.unlockSeat(busId, seat.id, journeyDate);
      this.selectedSeatIds.delete(seat.id);
    } else {
      // Select
      const locked = await this.busService.lockSeat(busId, seat.id, journeyDate);
      if (locked) {
        this.selectedSeatIds.add(seat.id);
      }
    }
  }

  // Get seat CSS class for styling
  getSeatClass(seat: SeatDto): string {
    if (!seat) return 'seat-empty';
    if (!seat.isAvailable) return 'seat-booked';
    if (seat.isLocked) return 'seat-locked';
    if (this.selectedSeatIds.has(seat.id)) return 'seat-selected';
    return 'seat-available';
  }
}
