using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace A2APaymentsApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTokensColToOrganisationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Organisations");
            migrationBuilder.AddColumn<string>(
                name: "AccessToken",
                table: "Organisations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Organisations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessToken",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Organisations");
        }
    }
}
