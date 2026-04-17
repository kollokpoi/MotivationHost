using System.Text;
using Microsoft.EntityFrameworkCore;
using Motivation.Data;
using Motivation.Data.Repositories;
using Motivation.Models;
using Motivation.ViewModels;

namespace MotivationTest
{
    public class ShiftsTests
    {
        IRepository<Shift> _shiftsRepository;
        IEmployeesRepository _employeesRepository;
        
        public ShiftsTests()
        {
            var appConnectionString = $"Server=127.0.0.1;Port=5432;Username=postgres;Password=postgres;Database=testdatabase;";
            var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(appConnectionString).Options;
            var dbContext = new ApplicationDbContext(dbContextOptions);
         
            _shiftsRepository = new ShiftsRepository(dbContext);
            _employeesRepository = new EmployeesRepository(dbContext);
        }

        [Fact]
        public async void ShifTimeCalculationTest()
        {
            var shiftViewModels = await GetShiftsViewModel(2024, 7);
            Assert.NotNull(shiftViewModels);
        }

        private async Task<ShiftsViewModel> GetShiftsViewModel(int year, int month)
        {
            var periodStartDate = new DateTime(year, month, 1);
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var shiftViewModels = new List<ShiftViewModel>();
            foreach (var employee in await _employeesRepository.Entries.ToListAsync())
            {
                var shifts = await _shiftsRepository.Entries.Where(s => s.EmployeeId == employee.Id)
                    .Where(s => s.Started.Year == year && s.Started.Month == month).ToListAsync();

                var workingHours = new int[daysInMonth];
                var comments = new string[daysInMonth];

                foreach (var shift in shifts)
                {
                    if (shift.Ended == DateTime.MinValue) continue;

                    var dayIndex = shift.Started.Day - 1;
                    var workTime = (int)Math.Floor((shift.Ended - shift.Started - shift.PauseTime).TotalHours);
                    workingHours[dayIndex] = Math.Min(workTime, (shift.LegalEndTime - shift.LegalStartTime).Hours);

                    var employeeStartTime = shift.LegalStartTime.ToLocalTime();
                    var employeeEndTime = shift.LegalEndTime.ToLocalTime();
                    var shiftStart = shift.Started.ToLocalTime();
                    var shiftEnd = shift.Ended.ToLocalTime();
                    var shiftStartDay = new DateTime(shiftStart.Year, shiftStart.Month, shiftStart.Day);

                    var comment = new StringBuilder();
                    comment.Append($"Начало: {shiftStart:g}<br>Конец: {shiftEnd:g}");

                    void AddCommentPart(TimeSpan time, string name)
                    {
                        if (time <= TimeSpan.Zero) return;
                        comment.Append($"<br>{name}: ");
                        if (time.Hours > 0) comment.Append($"{time.Hours:0} ч ");
                        comment.Append($"{time.Minutes:0} м ");
                        comment.Append($"{time.Seconds:0} с");
                    }

                    var lateness = shiftStart - employeeStartTime - new TimeSpan(shiftStartDay.Ticks);
                    var earlyFinish = employeeEndTime - (shiftEnd - new TimeSpan(shiftStartDay.Ticks));

                    AddCommentPart(shift.PauseTime, "Перерыв");
                    AddCommentPart(lateness, "Опоздание");
                    AddCommentPart(earlyFinish, "Уход раньше");

                    comments[dayIndex] = comment.ToString();
                }

                var shiftViewModel = new ShiftViewModel
                {
                    Employee = employee,
                    WorkingHours = workingHours.ToList(),
                    Comments = comments.ToList()
                };

                shiftViewModels.Add(shiftViewModel);
            }

            var shiftsViewModel = new ShiftsViewModel
            {
                EmployeesShifts = shiftViewModels,
                StartDate = periodStartDate,
                EndDate = periodStartDate.AddDays(daysInMonth - 1)
            };

            return shiftsViewModel;
        }
    }
}
