using System.Globalization;
using Motivation.Helpers;
using Motivation.ViewModels;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Motivation.Core.Services
{
    public class WorkingTimeTableGenerator
    {
        public void Generate(string path, ShiftsViewModel shifts)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var package = new ExcelPackage(fs))
            {
                var sheet = package.Workbook.Worksheets.Add("Табель");
                var cells = sheet.Cells;
                
                cells[1, 1].Value = "Сотрудники";
                cells[1, 2].Value = "Итого";
                var cellCounter = 3;
                for (var date = shifts.StartDate; date <= shifts.EndDate; date = date.AddDays(1))
                {
                    cells[1, cellCounter++].Value = date.ToString("d");
                }

                for (int i = 0; i < shifts.EmployeesShifts.Count; i++)
                {
                    var employee = shifts.EmployeesShifts[i].Employee;
                    var workingHours = shifts.EmployeesShifts[i].WorkingHours;
                    cells[i + 2, 1].Value = employee.GetShortName();
                    cells[i + 2, 2].Value = workingHours.Sum();
                    for (int j = 0; j < workingHours.Count; j++)
                    {
                        cells[i + 2, j + 3].Value = workingHours[j] == 0 ? "" : workingHours[j];
                    }
                }

                var endColumnLetter = ExcelCellAddress.GetColumnLetter(2 + shifts.EmployeesShifts[0].WorkingHours.Count);
                var usedRange = cells[$"A1:{endColumnLetter}{shifts.EmployeesShifts.Count + 1}"];
                ExcelHelper.MakeBorderOfCells(usedRange);
                usedRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cells.AutoFitColumns();
                package.SaveAs(fs);
            }
        }
    }
}
