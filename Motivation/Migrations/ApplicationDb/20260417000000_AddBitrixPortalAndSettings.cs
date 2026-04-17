using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motivation.Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class AddBitrixPortalAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Добавляем столбец PortalId в таблицу Departments
            migrationBuilder.AddColumn<int>(
                name: "PortalId",
                table: "Departments",
                type: "integer",
                nullable: true);

            // Добавляем столбец PortalId в таблицу Employees
            migrationBuilder.AddColumn<int>(
                name: "PortalId",
                table: "Employees",
                type: "integer",
                nullable: true);

            // Добавляем столбец PortalId в таблицу EmployeeTasks
            migrationBuilder.AddColumn<int>(
                name: "PortalId",
                table: "EmployeeTasks",
                type: "integer",
                nullable: true);

            // Создаем таблицу BitrixPortals
            migrationBuilder.CreateTable(
                name: "BitrixPortals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PortalUrl = table.Column<string>(type: "text", nullable: false),
                    WebhookUrl = table.Column<string>(type: "text", nullable: false),
                    IncomingSecret = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitrixPortals", x => x.Id);
                });

            // Создаем таблицу BitrixSettings
            migrationBuilder.CreateTable(
                name: "BitrixSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookUrl = table.Column<string>(type: "text", nullable: false),
                    EncryptedIncomingSecret = table.Column<string>(type: "text", nullable: true),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    SyncTasks = table.Column<bool>(type: "boolean", nullable: false),
                    SyncDeals = table.Column<bool>(type: "boolean", nullable: false),
                    TwoWaySync = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BitrixSettings", x => x.Id);
                });

            // Добавляем внешний ключ для Departments -> BitrixPortals
            migrationBuilder.CreateIndex(
                name: "IX_Departments_PortalId",
                table: "Departments",
                column: "PortalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_BitrixPortals_PortalId",
                table: "Departments",
                column: "PortalId",
                principalTable: "BitrixPortals",
                principalColumn: "Id");

            // Добавляем внешний ключ для Employees -> BitrixPortals
            migrationBuilder.CreateIndex(
                name: "IX_Employees_PortalId",
                table: "Employees",
                column: "PortalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_BitrixPortals_PortalId",
                table: "Employees",
                column: "PortalId",
                principalTable: "BitrixPortals",
                principalColumn: "Id");

            // Добавляем внешний ключ для EmployeeTasks -> BitrixPortals
            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_PortalId",
                table: "EmployeeTasks",
                column: "PortalId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeTasks_BitrixPortals_PortalId",
                table: "EmployeeTasks",
                column: "PortalId",
                principalTable: "BitrixPortals",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем внешние ключи
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeTasks_BitrixPortals_PortalId",
                table: "EmployeeTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_BitrixPortals_PortalId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_BitrixPortals_PortalId",
                table: "Departments");

            // Удаляем индексы
            migrationBuilder.DropIndex(
                name: "IX_EmployeeTasks_PortalId",
                table: "EmployeeTasks");

            migrationBuilder.DropIndex(
                name: "IX_Employees_PortalId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Departments_PortalId",
                table: "Departments");

            // Удаляем таблицы
            migrationBuilder.DropTable(
                name: "BitrixSettings");

            migrationBuilder.DropTable(
                name: "BitrixPortals");

            // Удаляем столбцы PortalId
            migrationBuilder.DropColumn(
                name: "PortalId",
                table: "EmployeeTasks");

            migrationBuilder.DropColumn(
                name: "PortalId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PortalId",
                table: "Departments");
        }
    }
}
