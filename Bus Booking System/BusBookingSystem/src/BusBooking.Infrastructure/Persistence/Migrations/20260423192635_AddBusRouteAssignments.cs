using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBooking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusRouteAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "BusRouteAssignments" (
                    "Id" uuid NOT NULL,
                    "BusId" uuid NOT NULL,
                    "RouteId" uuid NOT NULL,
                    "OperatorId" uuid NOT NULL,
                    "DepartureTime" interval NOT NULL,
                    "ArrivalTime" interval NOT NULL,
                    "DurationMinutes" integer NOT NULL,
                    "BaseFare" numeric(10,2) NOT NULL,
                    "IsApproved" boolean NOT NULL,
                    "IsRejected" boolean NOT NULL,
                    "AdminNotes" character varying(500),
                    "ReviewedAt" timestamp with time zone,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "UpdatedAt" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_BusRouteAssignments" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_BusRouteAssignments_Buses_BusId" FOREIGN KEY ("BusId") REFERENCES "Buses" ("Id") ON DELETE CASCADE,
                    CONSTRAINT "FK_BusRouteAssignments_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_BusRouteAssignments_BusId"
                ON "BusRouteAssignments" ("BusId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_BusRouteAssignments_RouteId"
                ON "BusRouteAssignments" ("RouteId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusRouteAssignments");
        }
    }
}
