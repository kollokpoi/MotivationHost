using OfficeOpenXml.Style;
using OfficeOpenXml;

namespace Motivation.Helpers
{
    public static class ExcelHelper
    {
        public static void MakeBorderOfCells(ExcelRange cells, ExcelBorderStyle borderStyle = ExcelBorderStyle.Thin)
        {
            cells.Style.Border.Top.Style = borderStyle;
            cells.Style.Border.Right.Style = borderStyle;
            cells.Style.Border.Bottom.Style = borderStyle;
            cells.Style.Border.Left.Style = borderStyle;
        }
    }
}
