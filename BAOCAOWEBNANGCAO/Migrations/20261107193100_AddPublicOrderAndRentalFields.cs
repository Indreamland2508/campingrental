using BAOCAOWEBNANGCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAOCAOWEBNANGCAO.Migrations
{
    [DbContext(typeof(CampingDbContext))]
    [Migration("20261107193100_AddPublicOrderAndRentalFields")]
    public partial class AddPublicOrderAndRentalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicOrderCode",
                table: "Orders",
                type: "character varying(19)",
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalChangeRequestedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedRentalStartDate",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedRentalEndDate",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalChangeReason",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalChangeResolvedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalChangeResolution",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxGuests",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "PublicOrderCode" = 'CR-' || upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 16))
                WHERE "PublicOrderCode" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PublicOrderCode",
                table: "Orders",
                type: "character varying(19)",
                maxLength: 19,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(19)",
                oldMaxLength: 19,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PublicOrderCode",
                table: "Orders",
                column: "PublicOrderCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PublicOrderCode",
                table: "Orders");

            migrationBuilder.DropColumn(name: "PublicOrderCode", table: "Orders");
            migrationBuilder.DropColumn(name: "RentalChangeRequestedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "RequestedRentalStartDate", table: "Orders");
            migrationBuilder.DropColumn(name: "RequestedRentalEndDate", table: "Orders");
            migrationBuilder.DropColumn(name: "RentalChangeReason", table: "Orders");
            migrationBuilder.DropColumn(name: "RentalChangeResolvedAt", table: "Orders");
            migrationBuilder.DropColumn(name: "RentalChangeResolution", table: "Orders");
            migrationBuilder.DropColumn(name: "MaxGuests", table: "Products");
        }
    }
}
