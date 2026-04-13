using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aerarium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Recurrence",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceCount",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurrenceEndDate",
                table: "Transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceGroupId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RecurrenceGroupId",
                table: "Transactions",
                column: "RecurrenceGroupId",
                filter: "\"RecurrenceGroupId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_RecurrenceGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Recurrence",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurrenceCount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurrenceEndDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurrenceGroupId",
                table: "Transactions");
        }
    }
}
