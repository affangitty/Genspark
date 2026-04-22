export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'User' | 'BusOperator' | 'Admin';
}

export interface LoginResponse {
  userId: string;
  email: string;
  firstName: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
}

export interface Bus {
  id: string;
  busNumber: string;
  operatorName: string;
  source: string;
  destination: string;
  totalSeats: number;
  availableSeats: number;
  basePrice: number;
  platformFee: number;
}

export interface Booking {
  id: string;
  busNumber: string;
  departureDate: Date;
  passengerCount: number;
  totalPrice: number;
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed';
}

export interface Seat {
  id: string;
  seatNumber: number;
  row: number;
  column: number;
  isAvailable: boolean;
  isLocked: boolean;
  price: number;
}
