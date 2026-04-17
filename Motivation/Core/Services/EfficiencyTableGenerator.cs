using Microsoft.EntityFrameworkCore;
using Motivation.Core.Interfaces;
using Motivation.Data;
using Motivation.Helpers;
using Motivation.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Motivation.Core.Services
{
    public class EfficiencyTableGenerator
    {
        private readonly IRepository<Penalty> _penaltiesRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<Shift> _shiftsRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IEfficiencyCalculator _efficiencyCalculator;
        private readonly RankCalculator _rankCalculator;

        public EfficiencyTableGenerator(IRepository<Penalty> penaltiesRepository, 
            IEmployeesRepository employeesRepository,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IEfficiencyCalculator efficiencyCalculator,
            IRepository<Shift> shiftsRepository)
        {
            _penaltiesRepository = penaltiesRepository;
            _employeesRepository = employeesRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _efficiencyCalculator = efficiencyCalculator;
            _rankCalculator = new RankCalculator();
            _shiftsRepository = shiftsRepository;
        }

        public async Task GenerateForPositionForCurrentMonth(string path, int positionId)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var package = new ExcelPackage(fs))
            {
                var sheet = package.Workbook.Worksheets.Add("Оценка");
                var cells = sheet.Cells;

                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                var startPeriod = new DateTime(year, month, 1);
                var endPeriod = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                var penalties = _penaltiesRepository.Entries.Where(p => p.PositionId == positionId).OrderBy(p => p.Points).ToList();
                var employees = _employeesRepository.Entries.Where(e => e.PositionId == positionId).ToList();

                cells[1, 1].Value = $"Оценка сотрудников за период с {startPeriod:d} по {endPeriod:d}";

                cells[2, 1].Value = "№ п/п";
                cells[2, 2].Value = "Сотрудник";
                cells[2, 3].Value = "Количество смен";
                cells[2, 4].Value = "Эффективность";
                cells[2, 5].Value = "Динамика";
                cells[2, 6].Value = "Ранг";
                cells[2, 7].Value = "Предыдущий ранг";
                cells[2, 8].Value = "Расчетный ранг";
                cells[2, 9].Value = "Оценка сотрудника";
                cells[2, 10].Value = "Оценка классификации";
                cells[2, 11].Value = "Оценка замечаний";

                for (int i = 13; i < penalties.Count; i++)
                {
                    cells[2, i].Value = $"{penalties[i].Description} ({penalties[i].Points})";
                }

                for (int i = 3; i < employees.Count; i++)
                { 
                    var employee = employees[i - 2];
                    var shiftsCount = await _shiftsRepository.Entries.CountAsync(s => s.EmployeeId == employee.Id && s.Started.Year == year && s.Started.Month == month);
                    var efficiency = await _efficiencyCalculator.CalculateForEmployeeForCurrentMonthAsync(employee.Id);
                    var employeePenalties = _employeePenaltiesRepository.Entries.Where(p => p.EmployeeId == employee.Id && p.Created.Year == year && p.Created.Month == month);
                    var employeePenaltiesCount = await employeePenalties.CountAsync();
                    var employeePenaltyPoints = await employeePenalties.SumAsync(p => p.Penalty.Points);
                    var qualificationPoints = employee.Qualification.Points;
                    var qualificationName = employee.Qualification.Name;
                    var score = _rankCalculator.CalculateScore(employeePenaltyPoints, qualificationPoints);
                    var previousRank = employee.Rank.Number;
                    var calculatedRank = _rankCalculator.CalculateRankNumber(score);
                    var newRank = _rankCalculator.CalculateNewRankWithLimits(previousRank, calculatedRank);

                    cells[i, 1].Value = i - 2;
                    cells[i, 2].Value = employee.GetFullName();
                    cells[i, 3].Value = shiftsCount;
                    cells[i, 4].Value = efficiency;
                    cells[i, 5].Value = "";
                    cells[i, 6].Value = newRank;
                    cells[i, 7].Value = previousRank;
                    cells[i, 8].Value = calculatedRank;
                    cells[i, 9].Value = score;
                    cells[i, 10].Value = $"{qualificationName} ({qualificationPoints})";
                    cells[i, 11].Value = employeePenaltiesCount;

                    for (int j = 12; j < penalties.Count; j++)
                    {
                        var penalty = penalties[j];
                        var penaltyCount = await employeePenalties.CountAsync(p => p.PenaltyId == penalty.Id);
                        cells[i, j].Value = $"{penaltyCount} ({penaltyCount * penalty.Points})";
                    }
                }

                var endColumnLetter = ExcelCellAddress.GetColumnLetter(12 + penalties.Count);
                var usedRange = cells[$"B1:{endColumnLetter}{employees.Count + 2}"];
                ExcelHelper.MakeBorderOfCells(usedRange);
                usedRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                package.SaveAs(fs);
            }
        }
    }
}
