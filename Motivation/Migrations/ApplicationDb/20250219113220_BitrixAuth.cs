using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class BitrixAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Qualifications",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Qualifications",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Shifts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Qualifications",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Ranks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Positions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Deadline",
                table: "EmployeeTasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "BitrixUserId",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BitrixUserId",
                table: "Employees");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Deadline",
                table: "EmployeeTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Budget", "Created", "Name", "ParentId", "Updated" },
                values: new object[] { 1, 100000m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2507), "Главное подразделение", 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2511) });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "Created", "Name", "Salary", "Updated" },
                values: new object[] { 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2513), "Менеджер", 1000m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2514) });

            migrationBuilder.InsertData(
                table: "Qualifications",
                columns: new[] { "Id", "Created", "Name", "Points", "PositionId", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2877), "Низкая", 0, 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2878) },
                    { 2, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2910), "Средняя", 10, 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2910) },
                    { 3, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2923), "Высокая", 20, 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2923) }
                });

            migrationBuilder.InsertData(
                table: "Ranks",
                columns: new[] { "Id", "Created", "Number", "PositionId", "SalaryBonus", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2950), 1, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(2951) },
                    { 2, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3068), 2, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3068) },
                    { 3, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3080), 3, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3080) },
                    { 4, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3091), 4, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3091) },
                    { 5, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3101), 5, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3102) },
                    { 6, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3116), 6, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3117) },
                    { 7, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3127), 7, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3128) },
                    { 8, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3139), 8, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3139) },
                    { 9, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3150), 9, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3151) },
                    { 10, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3163), 10, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3163) },
                    { 11, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3174), 11, 1, 100m, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3175) }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Created", "DepartmentId", "Email", "EndTime", "FirstName", "IsManager", "LastName", "MiddleName", "Photo", "PositionId", "QualificationId", "RankId", "StartTime", "Status", "Updated", "UserId" },
                values: new object[] { 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3282), 1, "ivan@test.ru", new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), "Иван", true, "Иванов", "Иванович", "/images/profile.png", 1, 1, 1, new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3283), "" });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "Id", "Created", "EmployeeId", "Ended", "LastPauseStart", "LegalEndTime", "LegalStartTime", "PauseTime", "Started", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3339), 1, new DateTime(2024, 4, 1, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 4, 1, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3339) },
                    { 2, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3364), 1, new DateTime(2024, 4, 2, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 4, 2, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3364) },
                    { 3, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3378), 1, new DateTime(2024, 4, 3, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 4, 3, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3379) },
                    { 4, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3392), 1, new DateTime(2024, 4, 4, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 4, 4, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3393) },
                    { 5, new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3407), 1, new DateTime(2024, 4, 5, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 4, 5, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 4, 5, 13, 7, 41, 297, DateTimeKind.Utc).AddTicks(3407) }
                });
        }
    }
}
