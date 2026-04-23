export interface User {
  id: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  role: 'User' | 'Operator' | 'Admin';
  isActive?: boolean;
}

export interface LoginResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface Bus {
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

export interface Booking {
  id: string;
  bookingReference: string;
  busNumber: string;
  busName: string;
  sourceCity: string;
  destinationCity: string;
  journeyDate: string;
  boardingAddress: string;
  dropOffAddress: string;
  baseFareTotal: number;
  convenienceFee: number;
  totalAmount: number;
  status: 'Pending' | 'Confirmed' | 'Cancelled' | 'CancelledByAdmin';
  createdAt: string;
  passengers: PassengerResponse[];
}

export interface Seat {
  id: string;
  seatNumber: string;
  row: number;
  column: number;
  deck: string;
  seatType: string;
  isAvailable: boolean;
  isLocked: boolean;
}

export interface PassengerRequest {
  passengerName: string;
  age: number;
  gender: string;
  seatId: string;
}

export interface PassengerResponse {
  passengerName: string;
  age: number;
  gender: string;
  seatNumber: string;
}

export interface CancellationResponse {
  bookingId: string;
  bookingReference: string;
  refundPercentage: number;
  refundAmount: number;
  cancelledAt: string;
}

export interface OperatorBookingSummary {
  totalBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  grossRevenue: number;
  totalRefunds: number;
  netRevenue: number;
}

export interface OperatorBookingItem {
  bookingId: string;
  bookingReference: string;
  journeyDate: string;
  userName: string;
  userEmail: string;
  busNumber: string;
  route: string;
  totalAmount: number;
  refundAmount: number;
  status: string;
  passengers: PassengerResponse[];
}

export interface AdminApprovalQueueItem {
  type: 'Operator' | 'Bus';
  id: string;
  displayName: string;
  requestedBy: string;
  requestedAt: string;
  status: string;
  additionalContext?: string;
}

export interface AdminRevenueDashboard {
  totalGrossRevenue: number;
  totalRefunds: number;
  totalNetRevenue: number;
  totalBookings: number;
  confirmedBookings: number;
  cancelledBookings: number;
  operatorRevenue: {
    operatorId: string;
    operatorName: string;
    totalBookings: number;
    confirmedBookings: number;
    cancelledBookings: number;
    grossRevenue: number;
    totalRefunds: number;
    netRevenue: number;
  }[];
}
