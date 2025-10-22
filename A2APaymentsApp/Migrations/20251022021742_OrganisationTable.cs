using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A2APaymentsApp.Migrations
{
    /// <inheritdoc />
    public partial class OrganisationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantShortCode = table.Column<string>(type: "TEXT", nullable: false),
                    BankAccountNumber = table.Column<string>(type: "TEXT", nullable: false),
                    AccountIdForPayment = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Organisations");
        }
    }
}
