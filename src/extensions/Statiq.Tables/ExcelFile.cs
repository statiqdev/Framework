﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace Statiq.Tables
{
    internal static class ExcelFile
    {
        public static IEnumerable<IEnumerable<string>> GetAllRecords(Stream stream, int sheetNumber = 0)
        {
            using (ExcelPackage excel = new ExcelPackage(stream))
            {
                excel.Compatibility.IsWorksheets1Based = false;
                if (sheetNumber > excel.Workbook.Worksheets.Count)
                {
                    return null;
                }

                ExcelWorksheet sheet = excel.Workbook.Worksheets[sheetNumber];

                return GetAllRecords(sheet);
            }
        }

        public static IEnumerable<IEnumerable<string>> GetAllRecords(ExcelWorksheet sheet)
        {
            ExcelAddressBase dimension = sheet.Dimension;

            if (dimension is null)
            {
                return null;
            }

            List<List<string>> rowList = new List<List<string>>();
            int rowCount = dimension.Rows;
            int columnCount = dimension.Columns;

            for (int r = 1; r <= rowCount; r++)
            {
                List<string> rowValues = new List<string>(columnCount);
                for (int c = 1; c <= columnCount; c++)
                {
                    ExcelRangeBase cell = sheet.Cells[r, c].FirstOrDefault();
                    rowValues.Add(cell?.Value?.ToString());
                }

                rowList.Add(rowValues);
            }

            return rowList;
        }
    }
}
