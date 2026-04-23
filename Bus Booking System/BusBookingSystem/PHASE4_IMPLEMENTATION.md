# Phase 4 — Search & Booking API Documentation

## Overview
Phase 4 implements the complete search and booking workflow for the Bus Booking System, including:
- Bus search with fuzzy location matching
- Real-time seat locking via SignalR
- Multi-passenger booking with payment processing
- PDF ticket generation
- SMTP email confirmations

---

## Task 4.1 — Bus Search API (with Fuzzy Logic)

### Endpoint
```
POST /api/bus/search
```

### Authentication
- **Required**: No (publicly accessible)

### Request Body
```json
{
  "sourceCity": "Mumbai",
  "destinationCity": "Pune",
  "journeyDate": "2024-12-25",
  "passengerCount": 2
}
```

### Response
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "busNumber": "MH-01-1234",
    "busName": "Sharma Travels Express",
    "operatorName": "Sharma Travels",
    "sourceCity": "Mumbai",
    "destinationCity": "Pune",
    "departureTime": "18:30:00",
    "arrivalTime": "22:45:00",
    "totalSeats": 40,
    "availableSeats": 35,
    "baseFare": 500.00,
    "convenienceFee": 25.00,
    "totalFare": 525.00,
    "status": "Active"
  }
]
```

### Features
- **Fuzzy Matching**: Uses Levenshtein distance algorithm to match city names
  - Handles typos and spelling variations
  - Threshold: 80% similarity
- **Seat Availability**: Filters out buses without enough seats
- **Real-time Pricing**: Calculates convenience fee based on platform config
- **Sorted Results**: Returns buses sorted by total fare (ascending)

### Implementation Details
- **Handler**: `SearchBusesQueryHandler`
- **Query**: `SearchBusesQuery`
- **Fuzzy Algorithm**: Levenshtein distance with 80% threshold
- **Performance**: Loads routes and buses from cache-like queries

---

## Task 4.2 — Seat Locking (Real-time)

### SignalR Hub
```
WebSocket Connection: /hubs/seats
```

### SignalR Methods (Client → Server)

#### 1. Lock Seat
```javascript
connection.invoke("LockSeat", busId, seatId, journeyDate)
  .then((success) => {
    if (success) {
      console.log("Seat locked successfully");
    }
  });
```

**Server Response**: Broadcasts to all clients viewing the bus
```javascript
// Received by all clients in the bus group
connection.on("SeatLocked", (seatId, userId) => {
  console.log(`Seat ${seatId} locked by user ${userId}`);
});
```

#### 2. Unlock Seat
```javascript
connection.invoke("UnlockSeat", busId, seatId, journeyDate)
  .then((success) => {
    if (success) {
      console.log("Seat unlocked");
    }
  });
```

**Server Response**:
```javascript
connection.on("SeatUnlocked", (seatId) => {
  console.log(`Seat ${seatId} is now available`);
});
```

#### 3. Extend Lock
```javascript
connection.invoke("ExtendLock", seatId, 300) // Extend by 5 minutes
  .then((success) => {
    if (success) {
      console.log("Lock extended");
    }
  });
```

#### 4. Join Bus Group
```javascript
connection.invoke("JoinBusGroup", busId, journeyDate);
```

#### 5. Leave Bus Group
```javascript
connection.invoke("LeaveBusGroup", busId, journeyDate);
```

### Features
- **Lock Duration**: Default 10 minutes (configurable via PlatformConfig)
- **Auto-expiry**: Expired locks are cleaned up automatically
- **Real-time Updates**: All clients viewing a bus see seat updates instantly
- **User-specific**: Locks are tied to user ID (prevents other users from unlocking)
- **Conflict Prevention**: System prevents double-booking scenarios

### Lock Lifecycle
1. User clicks seat → `LockSeat()` called
2. Seat is locked for 10 minutes (or configured duration)
3. User proceeds to checkout (can extend lock)
4. If lock expires, seat becomes available again
5. On successful booking, lock is released

### Database
- **Table**: `SeatLocks`
- **Fields**: 
  - `SeatId` (Guid)
  - `UserId` (Guid) 
  - `BusId` (Guid)
  - `JourneyDate` (DateTime)
  - `LockedAt` (DateTime)
  - `ExpiresAt` (DateTime)
  - `IsReleased` (bool)

---

## Task 4.3 — Booking API

### Endpoint: Create Booking
```
POST /api/booking/create
```

### Authentication
- **Required**: Yes (JWT Token)
- **Role**: User

### Request Body
```json
{
  "busId": "550e8400-e29b-41d4-a716-446655440000",
  "journeyDate": "2024-12-25",
  "passengers": [
    {
      "passengerName": "John Doe",
      "age": 28,
      "gender": "Male",
      "seatId": "550e8400-e29b-41d4-a716-446655440001"
    },
    {
      "passengerName": "Jane Smith",
      "age": 26,
      "gender": "Female",
      "seatId": "550e8400-e29b-41d4-a716-446655440002"
    }
  ]
}
```

### Response
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440050",
  "bookingReference": "BB-20241225-A1B2",
  "busNumber": "MH-01-1234",
  "busName": "Sharma Travels Express",
  "sourceCity": "Mumbai",
  "destinationCity": "Pune",
  "journeyDate": "2024-12-25",
  "boardingAddress": "Central Bus Station, Mumbai",
  "dropOffAddress": "Pune Bus Depot, Pune",
  "baseFareTotal": 1000.00,
  "convenienceFee": 50.00,
  "totalAmount": 1050.00,
  "status": "Confirmed",
  "createdAt": "2024-12-20T10:30:45Z",
  "passengers": [
    {
      "passengerName": "John Doe",
      "age": 28,
      "gender": "Male",
      "seatNumber": "A1"
    },
    {
      "passengerName": "Jane Smith",
      "age": 26,
      "gender": "Female",
      "seatNumber": "A2"
    }
  ]
}
```

### Booking Process
1. **Validation**: 
   - Check bus exists and is active
   - Verify all requested seats are available/unlocked
   - Validate passenger data

2. **Fare Calculation**:
   - Base Fare = Bus.BaseFare × Number of Passengers
   - Convenience Fee = Base Fare × PlatformConfig.ConvenienceFeePercentage / 100
   - Total Amount = Base Fare + Convenience Fee

3. **Seat Reservation**: Lock selected seats for the booking

4. **Payment Processing**: Call PaymentService
   - Generate transaction ID
   - Process payment (see Task 4.4)
   - Update booking status

5. **Post-Booking Actions**:
   - Generate PDF ticket
   - Send confirmation email
   - Release individual seat locks

### Lock/Unlock Seat Endpoints

#### Lock Seat
```
POST /api/booking/lock-seat
```

**Request**:
```json
{
  "seatId": "550e8400-e29b-41d4-a716-446655440001",
  "busId": "550e8400-e29b-41d4-a716-446655440000",
  "journeyDate": "2024-12-25"
}
```

**Response**:
```json
true
```

#### Unlock Seat
```
POST /api/booking/unlock-seat
```

**Request**: Same as lock-seat

**Response**:
```json
true
```

#### Extend Lock
```
POST /api/booking/extend-lock?seatId=xxx&additionalSeconds=300
```

**Response**:
```json
true
```

---

## Task 4.4 — Dummy Payment Gateway

### Payment Processing
The `PaymentService` simulates a payment gateway with 90% success rate for demonstration.

### Implementation Details
- **Service**: `PaymentService`
- **Interface**: `IPaymentService`
- **Transaction ID Format**: `TXN-{GUID}`

### Simulated Payment Flow

```csharp
public async Task<(bool success, string transactionId)> ProcessPaymentAsync(
    Guid bookingId, 
    decimal amount)
{
    // Simulate payment with 90% success rate
    var isSuccess = Random.Next(100) < 90;
    var transactionId = $"TXN-{Guid.NewGuid():N}";
    
    // Create/Update payment record
    var payment = new Payment
    {
        BookingId = bookingId,
        Amount = amount,
        TransactionId = transactionId,
        Status = isSuccess ? PaymentStatus.Completed : PaymentStatus.Failed,
        PaymentMethod = "DummyGateway",
        PaidAt = isSuccess ? DateTime.UtcNow : null
    };
    
    return (isSuccess, transactionId);
}
```

### Payment Statuses
- **Pending**: Awaiting payment
- **Completed**: Payment successful
- **Failed**: Payment declined
- **Refunded**: Refund processed

### Database
- **Table**: `Payments`
- **Fields**:
  - `BookingId` (Guid)
  - `TransactionId` (string)
  - `Amount` (decimal)
  - `Status` (PaymentStatus enum)
  - `PaymentMethod` (string)
  - `PaidAt` (DateTime?)
  - `RefundAmount` (decimal?)
  - `RefundedAt` (DateTime?)
  - `FailureReason` (string?)

### Refund Processing
```csharp
public async Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount)
{
    var payment = GetPayment(paymentId);
    
    payment.RefundAmount = amount;
    payment.RefundedAt = DateTime.UtcNow;
    payment.Status = PaymentStatus.Refunded;
    
    await SaveChangesAsync();
    return true;
}
```

---

## Task 4.5 — Ticket Generation

### Endpoint: Download Ticket
```
GET /api/booking/{bookingId}/ticket
```

### Authentication
- **Required**: Yes (JWT Token)

### Response
- **Content-Type**: `application/pdf`
- **Filename**: `Ticket_{BookingId}.pdf`

### Ticket Content
The ticket includes:
- Booking reference number
- Bus details (number, name, operator)
- Journey details (route, date, times)
- Boarding and drop-off addresses
- Passenger list with seat numbers
- Pricing breakdown
- Booking status and date

### Implementation Details
- **Service**: `TicketService`
- **Interface**: `ITicketService`
- **Format**: HTML (can be extended to PDF with iTextSharp)
- **Storage**: `/Tickets/` folder in application directory

### Current Implementation
HTML-based tickets with CSS styling. For production use one of:
- **iTextSharp** (free/.NET)
- **PdfSharpCore** (open source)
- **SelectPdf** (commercial)

### Future Enhancements
- QR code containing booking reference
- Barcode for validation
- Multiple language support
- Digital signature

---

## Task 4.6 — SMTP Email Confirmation

### Configuration
Update `appsettings.json`:
```json
{
  "SMTP": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "Email": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### Email Templates

#### 1. Booking Confirmation
**Sent on**: Successful booking
**Recipients**: User email
**Includes**: Ticket PDF attachment

```
Subject: Booking Confirmation - Ref: BB-20241225-A1B2

Dear Customer,

Your bus booking has been confirmed successfully!

Booking Reference: BB-20241225-A1B2

Your ticket has been attached to this email. Please save it for your records.

Please arrive at the boarding point 15 minutes before departure.

Thank you for choosing our bus booking service!

Best regards,
Bus Booking Team
```

#### 2. Cancellation Notice
**Sent on**: Booking cancellation
**Recipients**: User email

```
Subject: Booking Cancelled - Ref: BB-20241225-A1B2

Dear Customer,

Your booking has been cancelled.

Booking Reference: BB-20241225-A1B2
Refund Amount: ₹1050.00

The refund will be processed within 5-7 business days to your original payment method.

If you have any questions, please contact our support team.

Best regards,
Bus Booking Team
```

#### 3. Operator Disabled Notice
**Sent on**: Operator account disabled
**Recipients**: Operator email

```
Subject: Account Status Update - Bus Booking Platform

Dear [Operator Name],

Your operator account on the Bus Booking Platform has been disabled by the administrator.

If you believe this is an error, please contact our support team immediately.

Best regards,
Bus Booking Administrator
```

### Service Methods

```csharp
public async Task SendEmailAsync(string to, string subject, string body)
{
    // Generic email sending
}

public async Task SendBookingConfirmationAsync(
    string to, 
    string bookingId, 
    string ticketPath)
{
    // Sends confirmation with ticket attachment
}

public async Task SendCancellationNoticeAsync(
    string to, 
    string bookingId, 
    decimal refundAmount)
{
    // Sends cancellation notice
}

public async Task SendOperatorDisabledNoticeAsync(
    string to, 
    string operatorName, 
    string? alternativeOperators = null)
{
    // Notifies operator of account disabling
}
```

### SMTP Providers
- **Gmail**: Port 587, enable "App Passwords"
- **SendGrid**: Port 587
- **Mailgun**: Port 587
- **AWS SES**: Port 587

---

## Error Handling

### Common Error Responses

#### 400 Bad Request
```json
{
  "message": "Seat is already locked or unavailable"
}
```

#### 401 Unauthorized
```json
{
  "message": "User not authenticated"
}
```

#### 404 Not Found
```json
{
  "message": "Bus not found"
}
```

#### 409 Conflict
```json
{
  "message": "Payment failed. Please try again."
}
```

#### 500 Internal Server Error
```json
{
  "message": "An error occurred while creating the booking",
  "error": "Error details..."
}
```

---

## Database Entities

### SeatLock
- Manages temporary seat reservations
- Auto-expires after configured duration
- Cleaned up periodically

### Booking
- Core booking record
- Statuses: Pending, Confirmed, Cancelled, Failed
- Links to bus, user, passengers, and payment

### BookingPassenger
- Individual passenger records within a booking
- Links to specific seats
- Denormalized seat number for display

### Payment
- Payment transaction record
- Statuses: Pending, Completed, Failed, Refunded
- Stores transaction ID and refund info

---

## Performance Considerations

1. **Seat Queries**: Use indexed queries for available/booked seats
2. **SignalR Groups**: Organize by `Bus_{BusId}_{Date}` groups
3. **Lock Cleanup**: Run periodic job to clean expired locks
4. **Email Async**: Send emails asynchronously to prevent blocking
5. **Caching**: Cache platform config (rarely changes)

---

## Testing Checklist

- [ ] Search with fuzzy matching (typos, variations)
- [ ] Seat lock expires after duration
- [ ] Multiple users can't lock same seat
- [ ] Booking validates all seats are available
- [ ] Payment processing works (90% success)
- [ ] Ticket PDF generates correctly
- [ ] Email sent with ticket attachment
- [ ] SignalR updates all connected clients
- [ ] Proper error handling for edge cases
- [ ] Concurrency: multiple simultaneous bookings

---

## Future Enhancements (Phase 5+)

1. **Payment Gateway Integration**: Stripe, RazorPay, PayU
2. **Cancellation Policy**: Variable refund percentages
3. **Seat Selection UI**: Drag-and-drop seat selection
4. **Analytics Dashboard**: Booking trends, revenue reports
5. **Notification Preferences**: SMS, WhatsApp notifications
6. **Waitlist Management**: Automatic promotion when seats available
7. **Group Bookings**: Special pricing for groups
8. **Integration**: Travel packages, hotel bookings
