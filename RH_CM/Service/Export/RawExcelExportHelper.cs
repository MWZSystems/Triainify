using System.Reflection;
using ClosedXML.Excel;

namespace RH_CM.Service.Export
{
    /// <summary>
    /// Dumps every scalar column of a list of entities into an Excel sheet via reflection,
    /// with no joins, translations or curation — a raw "SELECT * FROM table" export meant
    /// to let staff cross-check what's actually stored in the database against what the
    /// friendlier, curated screens/reports show, to spot data inconsistencies.
    /// </summary>
    public static class RawExcelExportHelper
    {
        public static byte[] ExportFullData<T>(IEnumerable<T> data, string sheetName, IEnumerable<string>? excludeProperties = null)
        {
            var excluded = new HashSet<string>(excludeProperties ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            var properties = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0
                    && IsExportableType(p.PropertyType)
                    && !excluded.Contains(p.Name))
                .ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName));

            for (int col = 0; col < properties.Count; col++)
            {
                worksheet.Cell(1, col + 1).Value = properties[col].Name;
            }

            var lastColumn = Math.Max(properties.Count, 1);
            var headerRange = worksheet.Range(1, 1, 1, lastColumn);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4a90a4");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var item in data)
            {
                for (int col = 0; col < properties.Count; col++)
                {
                    SetCellValue(worksheet.Cell(row, col + 1), properties[col].GetValue(item));
                }
                row++;
            }

            var lastRow = Math.Max(row - 1, 1);
            worksheet.SheetView.FreezeRows(1);
            if (properties.Count > 0 && lastRow > 1)
            {
                worksheet.Range(1, 1, lastRow, lastColumn).SetAutoFilter();
            }
            worksheet.Columns().AdjustToContents(1, 60);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static string BuildFileName(string baseName)
        {
            return $"{baseName}_FullData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static bool IsExportableType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsEnum
                || underlying == typeof(string)
                || underlying == typeof(bool)
                || underlying == typeof(byte)
                || underlying == typeof(short)
                || underlying == typeof(int)
                || underlying == typeof(long)
                || underlying == typeof(float)
                || underlying == typeof(double)
                || underlying == typeof(decimal)
                || underlying == typeof(DateTime)
                || underlying == typeof(DateTimeOffset)
                || underlying == typeof(TimeSpan)
                || underlying == typeof(Guid);
        }

        // Excel's hard cap is 32,767 chars/cell, but a much lower cap keeps column
        // auto-sizing fast even when a text/URL column occasionally holds a huge value
        // (e.g. an inline base64 blob) — full precision isn't needed to spot inconsistencies.
        private const int MaxCellTextLength = 4000;

        private static void SetCellValue(IXLCell cell, object? value)
        {
            switch (value)
            {
                case null:
                    break;
                case string s:
                    cell.Value = Truncate(s);
                    break;
                case bool b:
                    cell.Value = b;
                    break;
                case DateTime dt:
                    cell.Value = dt;
                    break;
                case DateTimeOffset dto:
                    cell.Value = dto.DateTime;
                    break;
                case TimeSpan ts:
                    cell.Value = ts;
                    break;
                case double d:
                    cell.Value = d;
                    break;
                case float f:
                    cell.Value = (double)f;
                    break;
                case decimal m:
                    cell.Value = (double)m;
                    break;
                case int i:
                    cell.Value = i;
                    break;
                case long l:
                    cell.Value = l;
                    break;
                case short sh:
                    cell.Value = sh;
                    break;
                case byte by:
                    cell.Value = by;
                    break;
                case Guid g:
                    cell.Value = g.ToString();
                    break;
                case Enum e:
                    cell.Value = e.ToString();
                    break;
                default:
                    cell.Value = Truncate(value.ToString());
                    break;
            }
        }

        private static string? Truncate(string? value)
        {
            if (value == null || value.Length <= MaxCellTextLength)
            {
                return value;
            }
            return value.Substring(0, MaxCellTextLength) + "…(truncated)";
        }

        private static string SanitizeSheetName(string name)
        {
            var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':' };
            var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return sanitized.Length > 31 ? sanitized.Substring(0, 31) : sanitized;
        }
    }
}
