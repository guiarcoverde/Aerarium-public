using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aerarium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionRecurrenceAndSalarySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalarySchedule_BusinessDayNumber",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalarySchedule_FixedDay",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalarySchedule_Mode",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalarySchedule_SplitFirstAmount",
                table: "Transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalarySchedule_SplitFirstPercentage",
                table: "Transactions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalarySchedule_BusinessDayNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SalarySchedule_FixedDay",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SalarySchedule_Mode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SalarySchedule_SplitFirstAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SalarySchedule_SplitFirstPercentage",
                table: "Transactions");
        }
    }
}
