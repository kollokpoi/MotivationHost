using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrations.ApplicationDb
{
    /// <inheritdoc />
    public partial class FullSystemMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Budget = table.Column<decimal>(type: "numeric", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PointsOfInterest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsOfInterest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Salary = table.Column<decimal>(type: "numeric", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Penalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Penalties_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qualifications_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ranks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    SalaryBonus = table.Column<decimal>(type: "numeric", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ranks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ranks_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    QualificationId = table.Column<int>(type: "integer", nullable: false),
                    RankId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    MiddleName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Photo = table.Column<string>(type: "text", nullable: false),
                    IsManager = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Ranks_RankId",
                        column: x => x.RankId,
                        principalTable: "Ranks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    PenaltyId = table.Column<int>(type: "integer", nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePenalties_Employees_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePenalties_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePenalties_Penalties_PenaltyId",
                        column: x => x.PenaltyId,
                        principalTable: "Penalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ended = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTasks_Employees_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    RankId = table.Column<int>(type: "integer", nullable: false),
                    NewRankId = table.Column<int>(type: "integer", nullable: false),
                    CalculatedRankId = table.Column<int>(type: "integer", nullable: false),
                    QualificationId = table.Column<int>(type: "integer", nullable: false),
                    StartPeriod = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndPeriod = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Efficiency = table.Column<int>(type: "integer", nullable: false),
                    PenaltyPoints = table.Column<double>(type: "double precision", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    ShiftsCount = table.Column<int>(type: "integer", nullable: false),
                    WorkingTime = table.Column<int>(type: "integer", nullable: false),
                    Salary = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreSheets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreSheets_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreSheets_Ranks_CalculatedRankId",
                        column: x => x.CalculatedRankId,
                        principalTable: "Ranks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreSheets_Ranks_NewRankId",
                        column: x => x.NewRankId,
                        principalTable: "Ranks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreSheets_Ranks_RankId",
                        column: x => x.RankId,
                        principalTable: "Ranks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftRules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    Started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ended = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastPauseStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PauseTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    LegalStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LegalEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Budget", "Created", "Name", "ParentId", "Updated" },
                values: new object[] { 1, 100000m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9002), "Главное подразделение", 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9005) });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "Created", "Name", "Salary", "Updated" },
                values: new object[] { 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9007), "Менеджер", 1000m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9008) });

            migrationBuilder.InsertData(
                table: "Qualifications",
                columns: new[] { "Id", "Created", "Name", "Points", "PositionId", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9254), "Низкая", 0, 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9254) },
                    { 2, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9274), "Средняя", 10, 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9274) },
                    { 3, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9282), "Высокая", 20, 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9283) }
                });

            migrationBuilder.InsertData(
                table: "Ranks",
                columns: new[] { "Id", "Created", "Number", "PositionId", "SalaryBonus", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9344), 1, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9344) },
                    { 2, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9364), 2, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9364) },
                    { 3, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9372), 3, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9373) },
                    { 4, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9380), 4, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9380) },
                    { 5, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9387), 5, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9388) },
                    { 6, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9397), 6, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9397) },
                    { 7, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9405), 7, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9405) },
                    { 8, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9412), 8, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9412) },
                    { 9, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9419), 9, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9419) },
                    { 10, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9427), 10, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9428) },
                    { 11, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9435), 11, 1, 100m, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9435) }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Created", "DepartmentId", "Email", "EndTime", "FirstName", "IsManager", "LastName", "MiddleName", "Photo", "PositionId", "QualificationId", "RankId", "StartTime", "Status", "Updated", "UserId" },
                values: new object[] { 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9534), 1, "ivan@test.ru", new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), "Иван", true, "Иванов", "Иванович", "/images/profile.png", 1, 1, 1, new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9534), "" });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "Id", "Created", "EmployeeId", "Ended", "LastPauseStart", "LegalEndTime", "LegalStartTime", "PauseTime", "Started", "Updated" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9575), 1, new DateTime(2024, 2, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 1, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9575) },
                    { 2, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9595), 1, new DateTime(2024, 2, 2, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 2, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9595) },
                    { 3, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9607), 1, new DateTime(2024, 2, 5, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 5, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9607) },
                    { 4, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9617), 1, new DateTime(2024, 2, 6, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 6, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9617) },
                    { 5, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9627), 1, new DateTime(2024, 2, 7, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 7, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9627) },
                    { 6, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9637), 1, new DateTime(2024, 2, 8, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 8, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9638) },
                    { 7, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9647), 1, new DateTime(2024, 2, 9, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 9, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9647) },
                    { 8, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9658), 1, new DateTime(2024, 2, 12, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 12, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9658) },
                    { 9, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9668), 1, new DateTime(2024, 2, 13, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 13, 8, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9668) },
                    { 10, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9679), 1, new DateTime(2024, 2, 14, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 14, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9679) },
                    { 11, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9689), 1, new DateTime(2024, 2, 15, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 15, 6, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9689) },
                    { 12, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9761), 1, new DateTime(2024, 2, 16, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 16, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9762) },
                    { 13, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9776), 1, new DateTime(2024, 2, 19, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 19, 5, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9776) },
                    { 14, new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9785), 1, new DateTime(2024, 2, 20, 14, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(1, 1, 1, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(1, 1, 1, 6, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0), new DateTime(2024, 2, 20, 7, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 2, 20, 18, 38, 19, 525, DateTimeKind.Utc).AddTicks(9786) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentId",
                table: "Departments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePenalties_AuthorId",
                table: "EmployeePenalties",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePenalties_EmployeeId",
                table: "EmployeePenalties",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePenalties_PenaltyId",
                table: "EmployeePenalties",
                column: "PenaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_QualificationId",
                table: "Employees",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RankId",
                table: "Employees",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_AuthorId",
                table: "EmployeeTasks",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_EmployeeId",
                table: "EmployeeTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_PositionId",
                table: "Penalties",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Qualifications_PositionId",
                table: "Qualifications",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Ranks_PositionId",
                table: "Ranks",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSheets_CalculatedRankId",
                table: "ScoreSheets",
                column: "CalculatedRankId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSheets_EmployeeId",
                table: "ScoreSheets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSheets_NewRankId",
                table: "ScoreSheets",
                column: "NewRankId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSheets_QualificationId",
                table: "ScoreSheets",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreSheets_RankId",
                table: "ScoreSheets",
                column: "RankId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftRules_EmployeeId",
                table: "ShiftRules",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_EmployeeId",
                table: "Shifts",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePenalties");

            migrationBuilder.DropTable(
                name: "EmployeeTasks");

            migrationBuilder.DropTable(
                name: "PointsOfInterest");

            migrationBuilder.DropTable(
                name: "ScoreSheets");

            migrationBuilder.DropTable(
                name: "ShiftRules");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Penalties");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Qualifications");

            migrationBuilder.DropTable(
                name: "Ranks");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
