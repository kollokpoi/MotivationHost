using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddedPriceToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bonuses_Bonuses_BonusId",
                table: "Bonuses");

            migrationBuilder.DropIndex(
                name: "IX_Bonuses_BonusId",
                table: "Bonuses");

            migrationBuilder.DropColumn(
                name: "BonusId",
                table: "Bonuses");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "EmployeeTasks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "EmployeeTasks");

            migrationBuilder.AddColumn<int>(
                name: "BonusId",
                table: "Bonuses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bonuses_BonusId",
                table: "Bonuses",
                column: "BonusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bonuses_Bonuses_BonusId",
                table: "Bonuses",
                column: "BonusId",
                principalTable: "Bonuses",
                principalColumn: "Id");
        }
    }
}
