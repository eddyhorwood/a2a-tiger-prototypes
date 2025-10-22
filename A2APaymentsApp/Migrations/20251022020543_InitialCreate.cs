using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A2APaymentsApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignUpWithXeroUsers",
                columns: table => new
                {
                    XeroUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    GivenName = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyName = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantName = table.Column<string>(type: "TEXT", nullable: false),
                    AuthEventId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionCreatedDateUtc = table.Column<string>(type: "TEXT", nullable: false),
                    TenantShortCode = table.Column<string>(type: "TEXT", nullable: false),
                    TenantCountryCode = table.Column<string>(type: "TEXT", nullable: false),
                    AccountCreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<string>(type: "TEXT", nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignUpWithXeroUsers", x => x.XeroUserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignUpWithXeroUsers");
        }
    }
}
