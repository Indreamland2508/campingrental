using System;
using BAOCAOWEBNANGCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAOCAOWEBNANGCAO.Migrations
{
    [DbContext(typeof(CampingDbContext))]
    [Migration("20261107133000_HardenGuestCheckout")]
    public partial class HardenGuestCheckout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundStatus",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundProcessedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundNotes",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationResolvedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationResolution",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "AmountPaid" = CASE
                    WHEN "PaymentStatus" = 'Paid' THEN "TotalAmount"
                    WHEN "PaymentStatus" = 'Deposited' THEN "DepositAmount"
                    ELSE 0
                END;

                UPDATE "Orders"
                SET "ReservationExpiresAt" = "OrderDate" + INTERVAL '30 minutes'
                WHERE "PaymentStatus" = 'Unpaid'
                  AND "ReservationExpiresAt" IS NULL
                  AND "Status" NOT IN ('Cancelled', 'Completed', 'Expired');
                """);

            migrationBuilder.CreateTable(
                name: "SePayTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SePayId = table.Column<long>(type: "bigint", nullable: false),
                    Gateway = table.Column<string>(type: "text", nullable: true),
                    TransactionDate = table.Column<string>(type: "text", nullable: true),
                    AccountNumber = table.Column<string>(type: "text", nullable: true),
                    ReferenceCode = table.Column<string>(type: "text", nullable: true),
                    TransferType = table.Column<string>(type: "text", nullable: true),
                    TransferAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    AppliedToOrder = table.Column<bool>(type: "boolean", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SePayTransactions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "OrderLookupChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    ProtectedCode = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_OrderLookupChallenges", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_SePayTransactions_SePayId",
                table: "SePayTransactions",
                column: "SePayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLookupChallenges_OrderId_Email_ExpiresAt",
                table: "OrderLookupChallenges",
                columns: new[] { "OrderId", "Email", "ExpiresAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SePayTransactions");
            migrationBuilder.DropTable(name: "OrderLookupChallenges");

            migrationBuilder.DropColumn(name: "AmountPaid", table: "Orders");
            migrationBuilder.DropColumn(name: "ReservationExpiresAt", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundStatus", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundAmount", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundProcessedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundNotes", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationRequestedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationReason", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationResolvedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "CancellationResolution", table: "Orders");
        }
    }
}
