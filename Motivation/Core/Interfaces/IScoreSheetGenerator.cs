using System.Text;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using Motivation.Controllers.MobileApi;
using Motivation.Core.Services;
using Motivation.Data;
using Motivation.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Motivation.Core.Interfaces
{
    public interface IScoreSheetGenerator
    {
        Task CreateForEmployee(Employee employee, int year, int month);
        Task CreateSummarySheet(FileStream fileStream, IEnumerable<ScoreSheet> scoreSheet);
        Task GenerateExcelForEmployee(
            FileStream fileStream,
            ScoreSheet scoreSheet,
            IEnumerable<EmployeePenalty> penalties
        );
        Task GeneratePdfForEmployee(
            FileStream fileStream,
            ScoreSheet scoreSheet,
            IEnumerable<EmployeePenalty> penalties
        );
        Task<ScoreSheet> CreateScoreSheetForEmployee(Employee employee, int year, int month);
    }

    public class ScoreSheetGenerator : IScoreSheetGenerator
    {
        private readonly ILogger<ScoreSheetsController> _logger;
        private readonly IRepository<Rank> _ranksRepository;
        private readonly IRepository<Shift> _shiftsRepository;
        private readonly IRepository<ScoreSheet> _scoreSheetsRepository;
        private readonly IRepository<Qualification> _qualificationRepository;
        private readonly IRepository<EmployeePenalty> _employeePenaltiesRepository;
        private readonly IRepository<EmployeeTask> _employeeTasksRepository;
        private readonly IRepository<Penalty> _penaltiesRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly ISalaryCalculator _salaryCalculator;
        private readonly IEfficiencyCalculator _efficiencyCalculator;
        private readonly RankCalculator _rankCalculator;
        private readonly IWebHostEnvironment _environment;

        public ScoreSheetGenerator(
            ILogger<ScoreSheetsController> logger,
            IRepository<Rank> ranksRepository,
            IRepository<Shift> shiftsRepository,
            IEmployeesRepository employeesRepository,
            IRepository<ScoreSheet> scoreSheetsRepository,
            ISalaryCalculator salaryCalculator,
            IEfficiencyCalculator efficiencyCalculator,
            IRepository<EmployeePenalty> employeePenaltiesRepository,
            IRepository<EmployeeTask> employeeTasksRepository,
            IRepository<Penalty> penaltiesRepository,
            IRepository<Qualification> qualificationRepository,
            IWebHostEnvironment environment
        )
        {
            _logger = logger;
            _ranksRepository = ranksRepository;
            _shiftsRepository = shiftsRepository;
            _employeesRepository = employeesRepository;
            _scoreSheetsRepository = scoreSheetsRepository;
            _salaryCalculator = salaryCalculator;
            _efficiencyCalculator = efficiencyCalculator;
            _penaltiesRepository = penaltiesRepository;
            _qualificationRepository = qualificationRepository;
            _employeePenaltiesRepository = employeePenaltiesRepository;
            _employeeTasksRepository = employeeTasksRepository;
            _rankCalculator = new RankCalculator();
            _environment = environment;
        }

        public async Task CreateForEmployee(Employee employee, int year, int month)
        {
            try
            {
                var scoreSheet = await CreateScoreSheetForEmployee(employee, year, month);
                await _scoreSheetsRepository.CreateAsync(scoreSheet);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying create scoresheet:\n {e}";
                _logger.LogError(exceptionString);
            }
        }

        public async Task<ScoreSheet> CreateScoreSheetForEmployee(
            Employee employee,
            int year,
            int month
        )
        {
            var startPeriod = new DateTime(year, month, 1);
            var endPeriod = new DateTime(
                year,
                month,
                DateTime.DaysInMonth(year, month),
                23,
                59,
                59
            );

            var salary = await _salaryCalculator.CalculateSalaryForMonth(employee, year, month);
            var efficiency = await _efficiencyCalculator.CalculateForEmployee(
                employee.Id,
                year,
                month
            );
            var penalties = _employeePenaltiesRepository.Entries.Where(p =>
                p.EmployeeId == employee.Id && p.Created.Year == year && p.Created.Month == month
            );
            var penaltiesCount = await penalties.CountAsync();
            var penaltyPoints = await penalties.SumAsync(p => p.Penalty!.Points);

            var qualificationPoints = employee.Qualification?.Points ?? 0;
            var rankNumber = employee.Rank?.Number ?? 0;

            var score = _rankCalculator.CalculateScore(penaltyPoints, qualificationPoints);
            var calculatedRankNumber = _rankCalculator.CalculateRankNumber(score);
            var newRankNumber = _rankCalculator.CalculateNewRankWithLimits(
                rankNumber,
                calculatedRankNumber
            );

            var ranks = _ranksRepository.Entries.Where(r => r.PositionId == employee.PositionId);
            var calculatedRank = await ranks.FirstOrDefaultAsync(r =>
                r.Number == calculatedRankNumber
            );
            if (calculatedRank is null)
            {
                throw new Exception("Calculated rank is null");
            }
            var newRank = await ranks.FirstOrDefaultAsync(r => r.Number == newRankNumber);
            var shifts = await _shiftsRepository
                .Entries.Where(s => s.EmployeeId == employee.Id)
                .Where(s => s.Started.Year == year && s.Started.Month == month)
                .ToArrayAsync();

            // Group by date so if there are many shifts in one day we can dedup them into one.
            // The scoresheet will not be generated if there is -infinity in Ended.
            shifts = shifts
                .GroupBy(s => s.Started.Date)
                .Select(group =>
                {
                    var first = group.OrderBy(s => s.Id).First();
                    var last = group.OrderByDescending(s => s.Id).First();

                    return new Shift
                    {
                        Id = first.Id,
                        EmployeeId = first.EmployeeId,
                        Employee = first.Employee,
                        Started = first.Started,
                        Ended = last.Ended,
                        LastPauseStart = last.LastPauseStart,
                        PauseTime = last.PauseTime,
                        LegalStartTime = first.LegalStartTime,
                        LegalEndTime = first.LegalEndTime,
                    };
                })
                .Where(s => s.Ended != DateTime.MinValue) // Safety filter against workers who don't close the workday
                .ToArray();

            var workingTime = shifts.Sum(s =>
                Math.Min(
                    (int)Math.Floor((s.Ended - s.Started - s.PauseTime).TotalHours),
                    (int)(s.LegalEndTime - s.LegalStartTime).TotalHours
                )
            );
            var shiftsCount = shifts.Count();

            var scoreSheet = new ScoreSheet
            {
                EmployeeId = employee.Id,
                Employee = employee,
                QualificationId = employee.Qualification.Id,
                Qualification = employee.Qualification,
                Salary = salary,
                PenaltyPoints = penaltyPoints,
                StartPeriod = startPeriod.ToUniversalTime(),
                EndPeriod = endPeriod.ToUniversalTime(),
                WorkingTime = workingTime,
                ShiftsCount = shiftsCount,
                RankId = employee.Rank.Id,
                Rank = employee.Rank,
                CalculatedRankId = calculatedRank.Id,
                CalculatedRank = calculatedRank,
                NewRankId = newRank.Id,
                NewRank = newRank,
                Efficiency = efficiency,
                Score = score,
            };
            return scoreSheet;
        }

        public async Task CreateSummarySheet(
            FileStream fileStream,
            IEnumerable<ScoreSheet> scoreSheets
        )
        {
            await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(fileStream);
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Сводный лист");
                var row = 1;
                var range = worksheet.Cells[row, 1, row, 13];
                range.Merge = true;
                range.Value =
                    $"Оценка сотрудников за период с {scoreSheets.First().StartPeriod.ToString("d")} по {scoreSheets.First().EndPeriod.ToString("d")}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                range.Style.Font.Bold = true;
                row = 2;

                worksheet.Cells[row, 1].Value = "№ п/п";
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1].Style.Font.Bold = true;

                worksheet.Cells[row, 2].Value = "Сотрудник";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 2].Style.Font.Bold = true;

                worksheet.Cells[row, 3].Value = "Эффективность";
                worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 3].Style.Font.Bold = true;

                worksheet.Cells[row, 4].Value = "Итоговая оценка";
                worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 4].Style.Font.Bold = true;
                worksheet.Cells[row, 4].Style.WrapText = true;

                worksheet.Cells[row, 5].Value = "Базовая оценка";
                worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 5].Style.WrapText = true;

                worksheet.Cells[row, 6].Value = "Оценка нарушений";
                worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 6].Style.Font.Bold = true;
                worksheet.Cells[row, 6].Style.WrapText = true;

                worksheet.Cells[row, 7].Value = "Квалификация";
                worksheet.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 7].Style.Font.Bold = true;

                worksheet.Cells[row, 8].Value = "Очки квалификации";
                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 8].Style.Font.Bold = true;
                worksheet.Cells[row, 8].Style.WrapText = true;

                worksheet.Cells[row, 9].Value = "Кол-во смен";
                worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 9].Style.Font.Bold = true;
                worksheet.Cells[row, 9].Style.WrapText = true;

                worksheet.Cells[row, 10].Value = "Отработанное время";
                worksheet.Cells[row, 10].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 10].Style.Font.Bold = true;
                worksheet.Cells[row, 10].Style.WrapText = true;

                worksheet.Cells[row, 11].Value = "Новый ранг";
                worksheet.Cells[row, 11].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 11].Style.Font.Bold = true;

                worksheet.Cells[row, 12].Value = "Предыдущий ранг";
                worksheet.Cells[row, 12].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 12].Style.Font.Bold = true;

                worksheet.Cells[row, 13].Value = "Расчётный ранг";
                worksheet.Cells[row, 13].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 13].Style.Font.Bold = true;

                var num = 1;
                foreach (var scoreSheet in scoreSheets)
                {
                    row++;
                    worksheet.Cells[row, 1].Value = num;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 2].Value = scoreSheet.Employee?.GetFullName();
                    worksheet.Cells[row, 2].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Left;
                    worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 3].Value = $"{scoreSheet.Efficiency}%";
                    worksheet.Cells[row, 3].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 4].Value = scoreSheet.Score;
                    worksheet.Cells[row, 4].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 5].Value = RankCalculator.BasePoints;
                    worksheet.Cells[row, 5].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 6].Value = scoreSheet.PenaltyPoints;
                    worksheet.Cells[row, 6].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 7].Value = scoreSheet.Qualification?.Name;
                    worksheet.Cells[row, 7].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 8].Value = scoreSheet.Qualification?.Points ?? 10;
                    worksheet.Cells[row, 8].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 9].Value = scoreSheet.ShiftsCount;
                    worksheet.Cells[row, 9].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 10].Value = scoreSheet.WorkingTime;
                    worksheet.Cells[row, 10].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 11].Value = scoreSheet.NewRank?.Number;
                    worksheet.Cells[row, 11].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 12].Value = scoreSheet.Rank?.Number;
                    worksheet.Cells[row, 12].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 13].Value = scoreSheet.CalculatedRank?.Number;
                    worksheet.Cells[row, 13].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    num++;
                }
                worksheet.Cells.AutoFitColumns();

                package.SaveAs(fileStream);
            });
        }

        public async Task GenerateExcelForEmployee(
            FileStream fileStream,
            ScoreSheet scoreSheet,
            IEnumerable<EmployeePenalty> penalties
        )
        {
            await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(fileStream);
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Оценочный лист");
                var row = 1;
                var range = worksheet.Cells[row, 1, row, 13];
                range.Merge = true;
                range.Value =
                    $"ОЦЕНОЧНЫЙ ЛИСТ ЗА ПЕРИОД С {scoreSheet.StartPeriod.ToString("d")} ПО {scoreSheet.EndPeriod.ToString("d")}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;
                row = 2;

                range = worksheet.Cells[row, 1, row, 13];
                range.Merge = true;
                range.Value = $"РАБОТНИК: {scoreSheet.Employee?.GetFullName()}";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;

                row += 4;
                range = worksheet.Cells[row, 1, row, 3];
                range.Merge = true;
                range.Value = "Результаты оценки утверждены";

                row = 5;
                range = worksheet.Cells[row, 1, row, 3];
                range.Merge = true;
                range.Value = "Должность";
                range = worksheet.Cells[row, 4, row, 5];
                range.Merge = true;
                range.Value = "Ф.И.О.";
                range = worksheet.Cells[row, 6, row, 7];
                range.Merge = true;
                range.Value = "подпись";

                row = 8;

                range = worksheet.Cells[row, 1, row, 3];
                range.Merge = true;
                range.Value = "ЖУРНАЛ ЗАМЕЧАНИЙ";
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Font.Bold = true;

                row = 9;
                worksheet.Cells[row, 1].Value = "№ п/п";
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1].Style.Font.Bold = true;

                worksheet.Cells[row, 2].Value = "Дата";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 2].Style.Font.Bold = true;

                range = worksheet.Cells[row, 3, row, 8];
                range.Merge = true;
                range.Value = "Замечание";
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                range.Style.Font.Bold = true;

                worksheet.Cells[row, 9].Value = "Оценка нарушения";
                worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 9].Style.Font.Bold = true;

                worksheet.Cells[row, 10].Value = "Автор";
                worksheet.Cells[row, 10].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 10].Style.Font.Bold = true;

                row = 10;
                foreach (var penalty in penalties)
                {
                    worksheet.Cells[row, 1].Value = row - 8;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 2].Value = penalty.Created.ToString("d");
                    worksheet.Cells[row, 2].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    range = worksheet.Cells[row, 3, row, 8];
                    range.Merge = true;
                    range.Value = penalty.Penalty?.Description;
                    range.Style.WrapText = true;
                    range.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 9].Value = penalty.Penalty?.Points;
                    worksheet.Cells[row, 9].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                    worksheet.Cells[row, 10].Value = penalty.Author?.GetShortName();
                    worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    worksheet.Cells[row, 10].Style.HorizontalAlignment =
                        ExcelHorizontalAlignment.Center;

                    worksheet.Row(row).CustomHeight = true;
                    worksheet.Row(row).Height =
                        ((double)((penalty.Penalty?.Description.Count() ?? 0) / 100) + 1) * 25;
                    row++;
                }

                worksheet.Cells[row, 8].Value = "Итого";
                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 8].Style.Font.Bold = true;
                worksheet.Cells[row, 9].Value = scoreSheet.PenaltyPoints;
                worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 9].Style.Font.Bold = true;
                row += 2;

                range = worksheet.Cells[row, 1, row, 8];
                range.Merge = true;
                range.Value = "ОЦЕНКА";
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                range = worksheet.Cells[row, 9, row, 13];
                range.Merge = true;
                range.Value = "РАНГ";
                range.Style.Font.Bold = true;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);

                row++;
                worksheet.Cells[row, 1].Value = "Эффективность";
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 1].Style.Font.Bold = true;

                worksheet.Cells[row, 2].Value = "Итоговая оценка";
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 2].Style.Font.Bold = true;
                worksheet.Cells[row, 2].Style.WrapText = true;

                worksheet.Cells[row, 3].Value = "Базовая оценка";
                worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Style.WrapText = true;

                worksheet.Cells[row, 4].Value = "Оценка нарушений";
                worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 4].Style.Font.Bold = true;
                worksheet.Cells[row, 4].Style.WrapText = true;

                worksheet.Cells[row, 5].Value = "Квалификация";
                worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 5].Style.Font.Bold = true;

                worksheet.Cells[row, 6].Value = "Очки квалификации";
                worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 6].Style.Font.Bold = true;
                worksheet.Cells[row, 6].Style.WrapText = true;

                worksheet.Cells[row, 7].Value = "Кол-во смен";
                worksheet.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 7].Style.Font.Bold = true;
                worksheet.Cells[row, 7].Style.WrapText = true;

                worksheet.Cells[row, 8].Value = "Отработанное время";
                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 8].Style.Font.Bold = true;
                worksheet.Cells[row, 8].Style.WrapText = true;

                worksheet.Cells[row, 9].Value = "Новый";
                worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 9].Style.Font.Bold = true;

                worksheet.Cells[row, 10].Value = "Предыдущий";
                worksheet.Cells[row, 10].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 10].Style.Font.Bold = true;

                worksheet.Cells[row, 11].Value = "Расчётный";
                worksheet.Cells[row, 11].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 11].Style.Font.Bold = true;

                worksheet.Cells[row, 12].Value = "Подпись сотрудника";
                worksheet.Cells[row, 12].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 12].Style.Font.Bold = true;
                worksheet.Cells[row, 12].Style.WrapText = true;

                worksheet.Cells[row, 13].Value = "Подпись руководителя";
                worksheet.Cells[row, 13].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 13].Style.Font.Bold = true;
                worksheet.Cells[row, 13].Style.WrapText = true;

                row++;
                worksheet.Cells[row, 1].Value = $"{scoreSheet.Efficiency}%";
                worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 2].Value = scoreSheet.Score;
                worksheet.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 2].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 3].Value = RankCalculator.BasePoints;
                worksheet.Cells[row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 3].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 4].Value = scoreSheet.PenaltyPoints;
                worksheet.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 4].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 5].Value = scoreSheet.Qualification?.Name;
                worksheet.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 6].Value = scoreSheet.Qualification?.Points ?? 10;
                worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 7].Value = scoreSheet.ShiftsCount;
                worksheet.Cells[row, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 7].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 8].Value = scoreSheet.WorkingTime;
                worksheet.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 9].Value = scoreSheet.NewRank?.Number;
                worksheet.Cells[row, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 10].Value = scoreSheet.Rank?.Number;
                worksheet.Cells[row, 10].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 10].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 11].Value = scoreSheet.CalculatedRank?.Number;
                worksheet.Cells[row, 11].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 11].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 12].Value = "";
                worksheet.Cells[row, 12].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 12].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                worksheet.Cells[row, 13].Value = "";
                worksheet.Cells[row, 13].Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 13].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                row += 2;
                range = worksheet.Cells[row, 3, row, 8];
                range.Merge = true;
                range.Value = "С результатами оценки ознакомлен";
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                worksheet.Cells[row, 10].Value = "подпись";

                worksheet.Column(3).Width = 100;
                worksheet.Cells.AutoFitColumns();

                package.SaveAs(fileStream);
            });
        }

        public async Task GeneratePdfForEmployee(
            FileStream fileStream,
            ScoreSheet scoreSheet,
            IEnumerable<EmployeePenalty> penalties
        )
        {
            await Task.Run(() =>
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var fontPath = System.IO.Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot",
                    "arialuni.ttf"
                );
                var writer = new PdfWriter(fileStream);
                var pdf = new PdfDocument(writer);
                var font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H, pdf);
                ;
                var document = new Document(pdf, PageSize.A4.Rotate());
                document.SetFont(font);
                document.SetFontSize(8);

                // Set margins
                document.SetMargins(20, 20, 20, 20);

                // Add title
                document.Add(
                    new Paragraph(
                        $"ОЦЕНОЧНЫЙ ЛИСТ ЗА ПЕРИОД С {scoreSheet.StartPeriod.ToString("d")} ПО {scoreSheet.EndPeriod.ToString("d")}"
                    )
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SimulateBold()
                        .SetFontSize(12)
                );

                // Add employee information
                document.Add(
                    new Paragraph($"РАБОТНИК: {scoreSheet.Employee?.GetFullName()}")
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SimulateBold()
                        .SetMarginTop(5)
                );

                // Add empty space
                document.Add(new Paragraph(" ").SetMarginBottom(20));

                // Create position/signature table
                Table infoTable = new Table(6).UseAllAvailableWidth();
                infoTable.AddCell(CreateCell("Должность"));
                infoTable.AddCell(CreateCell(""));
                infoTable.AddCell(CreateCell("Ф.И.О."));
                infoTable.AddCell(CreateCell(""));
                infoTable.AddCell(CreateCell("подпись"));
                infoTable.AddCell(CreateCell(""));
                document.Add(infoTable);

                // Add approval text
                document.Add(
                    new Paragraph("Результаты оценки утверждены").SetFontSize(10).SetMarginTop(5)
                );

                // Add remarks section
                document.Add(new Paragraph("ЖУРНАЛ ЗАМЕЧАНИЙ").SimulateBold().SetMarginTop(20));

                // Create remarks table
                Table remarksTable = new Table(6).UseAllAvailableWidth();

                // Add headers
                remarksTable.AddHeaderCell(
                    CreateCell("№ п/п", true).SetTextAlignment(TextAlignment.CENTER)
                );
                remarksTable.AddHeaderCell(
                    CreateCell("Дата", true).SetTextAlignment(TextAlignment.CENTER)
                );
                remarksTable.AddHeaderCell(
                    CreateCell("Замечание", true).SetTextAlignment(TextAlignment.CENTER)
                );
                remarksTable.AddHeaderCell(
                    CreateCell("Оценка нарушения", true).SetTextAlignment(TextAlignment.CENTER)
                );
                remarksTable.AddHeaderCell(
                    CreateCell("Автор", true).SetTextAlignment(TextAlignment.CENTER)
                );

                // Add data row
                var num = 1;
                foreach (var penalty in penalties)
                {
                    remarksTable.AddCell(
                        CreateCell(num.ToString(), true).SetTextAlignment(TextAlignment.CENTER)
                    );
                    remarksTable.AddCell(
                        CreateCell(penalty.Created.ToString("d"), true)
                            .SetTextAlignment(TextAlignment.CENTER)
                    );
                    remarksTable.AddCell(CreateCell(penalty.Penalty?.Description ?? "", true));
                    remarksTable.AddCell(
                        CreateCell(penalty.Penalty?.Points.ToString() ?? "0", true)
                            .SetTextAlignment(TextAlignment.CENTER)
                    );
                    remarksTable.AddCell(
                        CreateCell(penalty.Author?.GetShortName() ?? "", true)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMinWidth(100)
                    );
                    remarksTable.AddCell(CreateCell(""));
                    num++;
                }

                // Add total row
                remarksTable.AddCell(CreateCell(""));
                remarksTable.AddCell(CreateCell(""));
                remarksTable.AddCell(CreateCell("Итого").SetTextAlignment(TextAlignment.RIGHT));
                remarksTable.AddCell(CreateCell("0", true));

                document.Add(remarksTable.SetMarginTop(10));

                var NO_BORDER = iText.Layout.Borders.Border.NO_BORDER;
                // Create evaluation table
                Table evaluationTable = new Table(13).UseAllAvailableWidth();
                evaluationTable.AddHeaderCell(
                    CreateCell("Оценка", true).SimulateBold().SetBorderRight(NO_BORDER)
                ); // 1
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 2
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 3
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 4
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 5
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 6
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 7
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 8
                evaluationTable.AddHeaderCell(
                    CreateCell("Ранг", true).SimulateBold().SetBorderRight(NO_BORDER)
                ); // 9
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 10
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 11
                evaluationTable.AddHeaderCell(
                    CreateCell("", true).SetBorderLeft(NO_BORDER).SetBorderRight(NO_BORDER)
                ); // 12
                evaluationTable.AddHeaderCell(CreateCell("", true).SetBorderLeft(NO_BORDER)); // 13

                // Add headers
                evaluationTable.AddCell(
                    CreateCell("Эффективность", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 1
                evaluationTable.AddCell(
                    CreateCell("Итоговая оценка", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 2
                evaluationTable.AddCell(
                    CreateCell("Базовая оценка", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 3
                evaluationTable.AddCell(
                    CreateCell("Оценка нарушений", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 4
                evaluationTable.AddCell(
                    CreateCell("Квалификация", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 5
                evaluationTable.AddCell(
                    CreateCell("Очки квалификации", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 6
                evaluationTable.AddCell(
                    CreateCell("Кол-во смен", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 7
                evaluationTable.AddCell(
                    CreateCell("Отработанное время", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 8
                evaluationTable.AddCell(
                    CreateCell("Новый", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 9
                evaluationTable.AddCell(
                    CreateCell("Предыдущий", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 10
                evaluationTable.AddCell(
                    CreateCell("Расчётный", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 11
                evaluationTable.AddCell(
                    CreateCell("Подпись сотрудника", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 12
                evaluationTable.AddCell(
                    CreateCell("Подпись руководителя", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 13

                // Add data row
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.Efficiency}%", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 1
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.Score}", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 2
                evaluationTable.AddCell(
                    CreateCell($"{RankCalculator.BasePoints}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 3
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.PenaltyPoints}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 4
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.Qualification?.Name}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 5
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.Qualification?.Points}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 6
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.ShiftsCount}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 7
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.WorkingTime}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 8
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.NewRank?.Number}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 9
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.Rank?.Number}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 10
                evaluationTable.AddCell(
                    CreateCell($"{scoreSheet.CalculatedRank?.Number}", true)
                        .SetTextAlignment(TextAlignment.CENTER)
                ); // 11
                evaluationTable.AddCell(
                    CreateCell("", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 12
                evaluationTable.AddCell(
                    CreateCell("", true).SetTextAlignment(TextAlignment.CENTER)
                ); // 13

                document.Add(evaluationTable.SetMarginTop(20));

                // Add acknowledgement section
                Table acknowledgementTable = new Table(4).UseAllAvailableWidth();
                acknowledgementTable.AddCell(CreateCell(""));
                acknowledgementTable.AddCell(CreateCell(""));
                acknowledgementTable.AddCell(
                    CreateCell("С результатами оценки ознакомлен")
                        .SetTextAlignment(TextAlignment.CENTER)
                );
                acknowledgementTable.AddCell(CreateCell("подпись"));

                document.Add(acknowledgementTable.SetMarginTop(20));

                // Close document
                document.Close();
            });
        }

        private Cell CreateCell(string text, bool hasBorder = false)
        {
            var cell = new Cell().Add(
                new Paragraph(text).SetPadding(5).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            );

            if (!hasBorder)
            {
                cell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            }

            return cell;
        }
    }
}
