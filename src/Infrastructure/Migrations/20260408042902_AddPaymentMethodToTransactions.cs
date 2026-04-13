using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aerarium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Transactions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Transactions");
        }
    }
}
