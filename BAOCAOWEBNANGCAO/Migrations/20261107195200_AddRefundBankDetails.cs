using BAOCAOWEBNANGCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAOCAOWEBNANGCAO.Migrations
{
    [DbContext(typeof(CampingDbContext))]
    [Migration("20261107195200_AddRefundBankDetails")]
    public partial class AddRefundBankDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundBankName",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankAccountNumber",
                table: "Orders",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankAccountHolder",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "RefundStatus" = 'DetailsRequired'
                WHERE "RefundAmount" > 0 AND "RefundStatus" = 'Pending';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RefundBankName", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundBankAccountNumber", table: "Orders");
            migrationBuilder.DropColumn(name: "RefundBankAccountHolder", table: "Orders");

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "RefundStatus" = 'Pending'
                WHERE "RefundStatus" = 'DetailsRequired';
                """);
        }
    }
}
