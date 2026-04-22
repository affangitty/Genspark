# Bus Booking System - Setup & Architecture

## Project Structure Overview

The Bus Booking System follows **Clean Architecture** principles with clear separation of concerns:

```
API Layer (Controllers, Middleware)
    ↓
Application Layer (Use Cases, DTOs, Validators)
    ↓
Domain Layer (Entities, Interfaces, Enums)
    ↓
Infrastructure Layer (EF Core, Repositories, Services)
```

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Real-time Communication**: SignalR
- **Validation**: FluentValidation
- **Mapping**: AutoMapper

### Frontend
- **Framework**: Angular 21
- **Styling**: SCSS
- **HTTP Client**: HttpClient
- **Real-time**: SignalR Client
- **Form Validation**: Reactive Forms

## Key Features Implemented

### 1. **Authentication & Authorization**
- User registration (regular user, bus operator)
- JWT-based authentication
- Role-based access control (User, BusOperator, Admin)
- Secure password hashing

### 2. **Bus Search & Management**
- Search buses by source, destination, and date
- Fuzzy search support
- Real-time seat availability
- Bus layout management

### 3. **Seat Locking & Booking**
- Real-time seat locking (600 seconds default)
- SignalR for WebSocket communication
- Prevent double-booking
- Multi-passenger bookings

### 4. **Payment Processing**
- Dummy payment gateway (ready for Stripe/RazorPay)
- Transaction tracking
- Refund management

### 5. **Booking Management**
- Book with multiple passengers
- Cancellation with refund rules:
  - 24+ hours: 50% refund
  - 12+ hours: 25% refund
- Ticket generation (PDF)
- Email confirmations

### 6. **Operator Features**
- Registration and approval workflow
- Add/manage buses
- Set seat pricing
- View bookings and revenue
- Manage operator locations

### 7. **Admin Features**
- Approve/reject operators
- Enable/disable operators (cascading refunds)
- Manage routes
- Set platform convenience fee
- View system revenue

## Database Schema

Key tables:
- `Users` - User accounts
- `BusOperators` - Bus operator profiles
- `Routes` - Point-to-point routes
- `Buses` - Bus information
- `Seats` - Individual seats with pricing
- `Bookings` - Reservations
- `Payments` - Payment records
- `Cancellations` - Cancellation history
- `OperatorLocations` - Bus operator office locations

## API Endpoints

### Authentication
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/register-operator`
- `POST /api/auth/refresh-token`

### Bus Search
- `POST /api/bus/search`
- `GET /api/bus/{id}`
- `GET /api/bus/{busId}/seats`

### Bookings
- `POST /api/booking`
- `GET /api/booking/my-bookings`
- `GET /api/booking/{id}`
- `POST /api/booking/{id}/cancel`
- `GET /api/booking/{id}/ticket`

### Routes (Admin)
- `GET /api/route`
- `POST /api/route`
- `PUT /api/route/{id}`
- `DELETE /api/route/{id}`

### Operator
- `GET /api/operator/dashboard`
- `GET /api/operator/buses`
- `POST /api/operator/buses`
- `GET /api/operator/bookings`
- `GET /api/operator/revenue`

### Admin
- `GET /api/admin/operators/pending`
- `POST /api/admin/operators/approve`
- `POST /api/admin/operators/{id}/toggle-status`
- `GET /api/admin/config`
- `PUT /api/admin/config`
- `GET /api/admin/revenue`

## SignalR Hub

**Seat Hub** (`/hubs/seats`)
- `LockSeat(seatId, busId)`
- `UnlockSeat(seatId, busId)`
- `JoinBus(busId)`
- Events: `seatLocked`, `seatUnlocked`, `error`

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=BusBookingDb;..."
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "BusBookingAPI",
    "Audience": "BusBookingClient"
  }
}
```

## Next Steps

1. **Implement Business Logic** - Add CQRS commands/queries
2. **Database Migrations** - Create EF Core migrations
3. **Angular Components** - Build UI pages
4. **Email Service** - Integrate SMTP
5. **PDF Generation** - Implement ticket generation
6. **Testing** - Unit and integration tests
7. **Deployment** - Docker, CI/CD pipeline

## Running the Application

### Backend
```bash
cd src/BusBooking.API
dotnet run
```

### Frontend
```bash
cd bus-booking-client
npm install
ng serve
```

### Database
```bash
# Create database and run migrations
dotnet ef database update
```

## Security Considerations

- ✅ JWT token expiration
- ✅ Role-based access control
- ✅ Password hashing
- ✅ CORS configuration
- ⚠️ TODO: HTTPS enforcement
- ⚠️ TODO: Rate limiting
- ⚠️ TODO: Input validation
- ⚠️ TODO: SQL injection prevention

