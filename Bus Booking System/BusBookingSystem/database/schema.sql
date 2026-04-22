-- ============================================================
-- Bus Booking System - PostgreSQL Schema
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ── Users ────────────────────────────────────────────────────
CREATE TABLE "Users" (
    "Id"           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "FullName"     VARCHAR(150)  NOT NULL,
    "Email"        VARCHAR(200)  NOT NULL UNIQUE,
    "PhoneNumber"  VARCHAR(15),
    "PasswordHash" TEXT          NOT NULL,
    "Role"         INTEGER       NOT NULL DEFAULT 0,  -- 0=User,1=Operator,2=Admin
    "IsActive"     BOOLEAN       NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ── Bus Operators ─────────────────────────────────────────────
CREATE TABLE "BusOperators" (
    "Id"                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "CompanyName"       VARCHAR(200) NOT NULL,
    "ContactPersonName" VARCHAR(150) NOT NULL,
    "Email"             VARCHAR(200) NOT NULL UNIQUE,
    "PhoneNumber"       VARCHAR(15),
    "PasswordHash"      TEXT         NOT NULL,
    "Status"            INTEGER      NOT NULL DEFAULT 0, -- 0=Pending,1=Approved,2=Rejected,3=Disabled
    "RejectionReason"   VARCHAR(500),
    "AdminNotes"        TEXT,
    "ApprovedAt"        TIMESTAMPTZ,
    "DisabledAt"        TIMESTAMPTZ,
    "CreatedAt"         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"         TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ── Operator Locations ────────────────────────────────────────
CREATE TABLE "OperatorLocations" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "OperatorId"  UUID         NOT NULL REFERENCES "BusOperators"("Id") ON DELETE CASCADE,
    "City"        VARCHAR(100) NOT NULL,
    "AddressLine" VARCHAR(300) NOT NULL,
    "Landmark"    VARCHAR(200),
    "State"       VARCHAR(100),
    "PinCode"     VARCHAR(10),
    "CreatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ── Routes ────────────────────────────────────────────────────
CREATE TABLE "Routes" (
    "Id"               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "SourceCity"       VARCHAR(100) NOT NULL,
    "DestinationCity"  VARCHAR(100) NOT NULL,
    "SourceState"      VARCHAR(100),
    "DestinationState" VARCHAR(100),
    "IsActive"         BOOLEAN      NOT NULL DEFAULT TRUE,
    "CreatedAt"        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE ("SourceCity", "DestinationCity")
);

-- ── Bus Layouts ───────────────────────────────────────────────
CREATE TABLE "BusLayouts" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "LayoutName"  VARCHAR(100) NOT NULL,
    "TotalSeats"  INTEGER      NOT NULL,
    "Rows"        INTEGER      NOT NULL,
    "Columns"     INTEGER      NOT NULL,
    "HasUpperDeck" BOOLEAN     NOT NULL DEFAULT FALSE,
    "LayoutJson"  JSONB        NOT NULL DEFAULT '[]',
    "OperatorId"  UUID         NOT NULL REFERENCES "BusOperators"("Id") ON DELETE RESTRICT,
    "CreatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ── Buses ─────────────────────────────────────────────────────
CREATE TABLE "Buses" (
    "Id"              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BusNumber"       VARCHAR(20)     NOT NULL UNIQUE,
    "BusName"         VARCHAR(200)    NOT NULL,
    "Status"          INTEGER         NOT NULL DEFAULT 0,
    "OperatorId"      UUID            NOT NULL REFERENCES "BusOperators"("Id") ON DELETE RESTRICT,
    "LayoutId"        UUID            NOT NULL REFERENCES "BusLayouts"("Id") ON DELETE RESTRICT,
    "RouteId"         UUID            REFERENCES "Routes"("Id") ON DELETE SET NULL,
    "DepartureTime"   TIME,
    "ArrivalTime"     TIME,
    "DurationMinutes" INTEGER,
    "BaseFare"        DECIMAL(10,2)   NOT NULL DEFAULT 0,
    "ApprovedAt"      TIMESTAMPTZ,
    "AdminNotes"      TEXT,
    "CreatedAt"       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    "UpdatedAt"       TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

-- ── Seats ─────────────────────────────────────────────────────
CREATE TABLE "Seats" (
    "Id"         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BusId"      UUID        NOT NULL REFERENCES "Buses"("Id") ON DELETE CASCADE,
    "SeatNumber" VARCHAR(10) NOT NULL,
    "Row"        INTEGER     NOT NULL,
    "Column"     INTEGER     NOT NULL,
    "Deck"       VARCHAR(10) NOT NULL DEFAULT 'lower',
    "SeatType"   INTEGER     NOT NULL DEFAULT 0,
    "IsActive"   BOOLEAN     NOT NULL DEFAULT TRUE,
    "CreatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE ("BusId", "SeatNumber")
);

-- ── Seat Locks ────────────────────────────────────────────────
CREATE TABLE "SeatLocks" (
    "Id"          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "SeatId"      UUID        NOT NULL REFERENCES "Seats"("Id") ON DELETE CASCADE,
    "UserId"      UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "BusId"       UUID        NOT NULL,
    "JourneyDate" DATE        NOT NULL,
    "LockedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ExpiresAt"   TIMESTAMPTZ NOT NULL,
    "IsReleased"  BOOLEAN     NOT NULL DEFAULT FALSE,
    "CreatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_SeatLocks_SeatId_JourneyDate_IsReleased"
    ON "SeatLocks" ("SeatId", "JourneyDate", "IsReleased");

-- ── Bookings ──────────────────────────────────────────────────
CREATE TABLE "Bookings" (
    "Id"               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingReference" VARCHAR(30)   NOT NULL UNIQUE,
    "UserId"           UUID          NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "BusId"            UUID          NOT NULL REFERENCES "Buses"("Id") ON DELETE RESTRICT,
    "JourneyDate"      DATE          NOT NULL,
    "Status"           INTEGER       NOT NULL DEFAULT 0,
    "BaseFareTotal"    DECIMAL(10,2) NOT NULL,
    "ConvenienceFee"   DECIMAL(10,2) NOT NULL,
    "TotalAmount"      DECIMAL(10,2) NOT NULL,
    "BoardingAddress"  VARCHAR(400),
    "DropOffAddress"   VARCHAR(400),
    "CreatedAt"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"        TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);
CREATE INDEX "IX_Bookings_UserId" ON "Bookings" ("UserId");
CREATE INDEX "IX_Bookings_BusId_JourneyDate" ON "Bookings" ("BusId", "JourneyDate");

-- ── Booking Passengers ────────────────────────────────────────
CREATE TABLE "BookingPassengers" (
    "Id"            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"     UUID        NOT NULL REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "SeatId"        UUID        NOT NULL REFERENCES "Seats"("Id") ON DELETE RESTRICT,
    "PassengerName" VARCHAR(150) NOT NULL,
    "Age"           INTEGER     NOT NULL,
    "Gender"        VARCHAR(10) NOT NULL,
    "SeatNumber"    VARCHAR(10) NOT NULL,
    "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── Payments ──────────────────────────────────────────────────
CREATE TABLE "Payments" (
    "Id"            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"     UUID          NOT NULL UNIQUE REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "TransactionId" VARCHAR(100),
    "Amount"        DECIMAL(10,2) NOT NULL,
    "Status"        INTEGER       NOT NULL DEFAULT 0,
    "PaymentMethod" VARCHAR(50)   NOT NULL DEFAULT 'DummyGateway',
    "PaidAt"        TIMESTAMPTZ,
    "RefundAmount"  DECIMAL(10,2),
    "RefundedAt"    TIMESTAMPTZ,
    "FailureReason" VARCHAR(300),
    "CreatedAt"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"     TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ── Cancellations ─────────────────────────────────────────────
CREATE TABLE "Cancellations" (
    "Id"               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "BookingId"        UUID          NOT NULL UNIQUE REFERENCES "Bookings"("Id") ON DELETE CASCADE,
    "CancelledAt"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "Reason"           VARCHAR(500),
    "RefundPercentage" DECIMAL(5,2)  NOT NULL DEFAULT 0,
    "RefundAmount"     DECIMAL(10,2) NOT NULL DEFAULT 0,
    "IsAdminInitiated" BOOLEAN       NOT NULL DEFAULT FALSE,
    "CreatedAt"        TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "UpdatedAt"        TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

-- ── Platform Config ───────────────────────────────────────────
CREATE TABLE "PlatformConfigs" (
    "Id"                       UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "ConvenienceFeePercentage" DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    "SeatLockDurationMinutes"  INTEGER      NOT NULL DEFAULT 10,
    "UpdatedByAdminId"         VARCHAR(100),
    "CreatedAt"                TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "UpdatedAt"                TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Seed default platform config
INSERT INTO "PlatformConfigs" ("Id", "ConvenienceFeePercentage", "SeatLockDurationMinutes")
VALUES (uuid_generate_v4(), 5.00, 10);