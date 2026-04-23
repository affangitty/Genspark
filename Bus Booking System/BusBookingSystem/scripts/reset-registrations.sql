-- Reset customer & operator sign-ups while keeping seeded admin and core catalog data.
-- UserRole enum in DB: User=0, Operator=1, Admin=2 — keep only Admin users.
-- Run: psql -h localhost -p 5432 -U postgres -d BusBookingDb -v ON_ERROR_STOP=1 -f scripts/reset-registrations.sql

BEGIN;

-- Bookings reference Users with ON DELETE RESTRICT — remove first (cascades passengers, payments, cancellations).
DELETE FROM "Bookings" b
WHERE b."UserId" IN (SELECT u."Id" FROM "Users" u WHERE u."Role" <> 2);

-- Seat locks cascade when user is deleted; safe to clear any remaining for removed users (none if above ran first).
DELETE FROM "SeatLocks" sl
WHERE sl."UserId" IN (SELECT u."Id" FROM "Users" u WHERE u."Role" <> 2);

DELETE FROM "Users" WHERE "Role" <> 2;

-- Operator registrations with no buses yet (typical pending sign-up demo data).
DELETE FROM "OperatorLocations" ol
WHERE ol."OperatorId" IN (
  SELECT o."Id" FROM "BusOperators" o
  WHERE NOT EXISTS (SELECT 1 FROM "Buses" b WHERE b."OperatorId" = o."Id")
);

DELETE FROM "BusOperators" o
WHERE NOT EXISTS (SELECT 1 FROM "Buses" b WHERE b."OperatorId" = o."Id");

COMMIT;
