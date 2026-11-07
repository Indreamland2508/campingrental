using BAOCAOWEBNANGCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BAOCAOWEBNANGCAO.Migrations
{
    [DbContext(typeof(CampingDbContext))]
    [Migration("20261107201200_RecordCheckoutConsent")]
    public partial class RecordCheckoutConsent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyConsentVersion",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RentalPolicyAcceptedAt",
                table: "Orders",
                type: "timestamp without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PrivacyConsentAt", table: "Orders");
            migrationBuilder.DropColumn(name: "PrivacyConsentVersion", table: "Orders");
            migrationBuilder.DropColumn(name: "RentalPolicyAcceptedAt", table: "Orders");
        }
    }
}
