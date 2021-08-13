using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace ExcisePlaning.Classes
{
    /// <summary>
    /// ตัวช่วยในการเขียนข้อมูลลงไฟล์ Excel ด้วย EPPLUS
    /// </summary>
    public class ExportUtils
    {
        /// <summary>
        /// Worksheet ในปัจจุบันที่กำลังเขียนอ่าน ข้อมูลอยู่
        /// </summary>
        public static ExcelWorksheet CurrWorkSheet { get; set; }

        /// <summary>
        /// Excel Range ในปัจจุบันที่กำลังทำงานอยู่
        /// </summary>
        public static ExcelRange SelectedExcelRange { get; set; }

        /// <summary>
        /// สีของหัวคอลัมล์ตาราง
        /// </summary>
        public static string CaptionHtmlColorCode { get { return "#F6F6F6"; } }

        /// <summary>
        /// สีของแถวที่เป็นแถวคี่
        /// </summary>
        public static string OddHtmlColorCode { get { return "#E9E9E9"; } }

        /// <summary>
        /// สีของแถวข้อมูลที่เป็นกลุ่มของข้อมูล
        /// </summary>
        public static string GroupHtmlColorCode { get { return "#CFCFCF"; } }

        /// <summary>
        /// รูปแบบตัวเลข
        /// </summary>
        public static string CurrencyNumberFormat { get { return "_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)"; } }

        public static string[] ColumnsName = new string[] {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "AA", "AB", "AC", "AD", "AE", "AF", "AG", "AH", "AI", "AJ", "AK", "AL", "AM", "AN", "AO", "AP", "AQ", "AR", "AS", "AT", "AU", "AV", "AW", "AX", "AY", "AZ",
            "BA", "BB", "BC", "BD", "BE", "BF", "BG", "BH", "BI", "BJ", "BK", "BL", "BM", "BN", "BO", "BP", "BQ", "BR", "BS", "BT", "BU", "BV", "BW", "BX", "BY", "BZ",
            "CA", "CB", "CC", "CD", "CE", "CF", "CG", "CH", "CI", "CJ", "CK", "CL", "CM", "CN", "CO", "CP", "CQ", "CR", "CS", "CT", "CU", "CV", "CW", "CX", "CY", "CZ",
            "DA", "DB", "DC", "DD", "DE", "DF", "DG", "DH", "DI", "DJ", "DK", "DL", "DM", "DN", "DO", "DP", "DQ", "DR", "DS", "DT", "DU", "DV", "DW", "DX", "DY", "DZ"
        };

        /// <summary>
        /// Ex. A1, A1:A2
        /// </summary>
        /// <param name="As"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static ExcelRange GetRange(string range)
        {
            CurrWorkSheet.Select(range);
            return CurrWorkSheet.SelectedRange;
        }

        /// <summary>
        /// ค้นหา Cell Object โดยใช้ RowIndex & ColumnIndex
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="columnIndex"></param>
        /// <returns></returns>
        public static ExcelRange GetCellByIndex(int rowIndex, int columnIndex)
        {
            SelectedExcelRange = CurrWorkSheet.Cells[rowIndex, columnIndex];
            return SelectedExcelRange;
        }

        public static void SetReportName(string range, string text, bool includeExportDate)
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            SelectedExcelRange.Style.Font.Size = 16;
            SelectedExcelRange.Style.Font.Bold = true;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;

            if (includeExportDate)
            {
                text = string.Format("{0}     {1}", text
                    , DateTime.Now.ToString("[ข้อมูล ณ วันที่ dd MMMM yyyy เวลา HH:mm:ss]", new CultureInfo("th-TH")));
            }
            SelectedExcelRange.Value = text;
        }

        public static void SetCaption(string range, string text, string htmlColorCode = "#F6F6F6", bool isFontBold = true)
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            if (isFontBold)
                SelectedExcelRange.Style.Font.Bold = true;
            else
                SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            SelectedExcelRange.Value = text;

            //AutofitRowHeight();

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static void SetCellTextVal(string range, string value, bool isBorder, string htmlColorCode = "",bool isFontBold = false)
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            if (isFontBold)
                SelectedExcelRange.Style.Font.Bold = true;
            else
                SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            if (isBorder)
                SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            SelectedExcelRange.Value = value;

            CurrWorkSheet.SelectedRange.Style.WrapText = true;
            AutofitRowHeight();

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static void SetBorder(string range, string htmlColorCode = "")
        {
            SelectedExcelRange = GetRange(range);

            SelectedExcelRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            SelectedExcelRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            SelectedExcelRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            SelectedExcelRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }
        }

        public static void SetCellFormulaVal(string range, string value, bool isBorder, string htmlColorCode = "")
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            if (isBorder)
                SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            SelectedExcelRange.Formula = value;

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static void SetCellFormulaVal(int rowIndex, int columnIndex, string value, bool isBorder, string htmlColorCode = "")
        {
            SelectedExcelRange = GetCellByIndex(rowIndex, columnIndex);
            SelectedExcelRange.Merge = true;
            SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            if (isBorder)
                SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            SelectedExcelRange.Formula = value;

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static void SetCellCurrencyVal(string range, decimal? value, bool isBorder, string numberFormat = "_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)", string htmlColorCode = "")
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            if (isBorder)
                SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            SelectedExcelRange.Value = value != null ? value.Value : 0;
            SelectedExcelRange.Style.Numberformat.Format = numberFormat;


            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static void SetCellIntVal(string range, int? value, bool isBorder, string htmlColorCode = "")
        {
            SelectedExcelRange = GetRange(range);
            SelectedExcelRange.Merge = true;
            SelectedExcelRange.Style.Font.Bold = false;
            SelectedExcelRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            SelectedExcelRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            if (isBorder)
                SelectedExcelRange.Style.Border.BorderAround(ExcelBorderStyle.Thin);

            SelectedExcelRange.Value = null != value ? value.Value : 0;
            SelectedExcelRange.Style.Numberformat.Format = "_(* #,##0_);_(* (#,##0);_(* \"-\"??_);_(@_)";

            if (!string.IsNullOrEmpty(htmlColorCode))
            {
                SelectedExcelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                SelectedExcelRange.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(htmlColorCode));
            }

            AutofitRowHeight();
        }

        public static double MeasureTextHeight(string text, ExcelFont font, Int32 width)
        {
            if (string.IsNullOrEmpty(text))
                return 0.0;

            var bitmap = new Bitmap(1, 1);
            var graphics = Graphics.FromImage(bitmap);

            var pixelWidth = Convert.ToInt32(width * 7);  //7 pixels per excel column width
            var fontSize = font.Size * 1.01f;
            var drawingFont = new Font(font.Name, fontSize);
            var size = graphics.MeasureString(text, drawingFont, pixelWidth, new StringFormat { FormatFlags = StringFormatFlags.MeasureTrailingSpaces });

            //72 DPI and 96 points per inch.  Excel height in points with max of 409 per Excel requirements.
            return Math.Min(Convert.ToDouble(size.Height) * 72 / 96, 409);
        }


        /// <summary>
        /// คำนวนขนาดความสูงของ Excel Row
        /// </summary>
        private static void AutofitRowHeight()
        {
            if (null == SelectedExcelRange || null == SelectedExcelRange.Value)
                return;

            string cellText = null == SelectedExcelRange.Value ? string.Empty : SelectedExcelRange.Value.ToString();
            if (cellText.Contains("System.Object"))
                return;

            int rowIndex = SelectedExcelRange.Start.Row; // แถวปัจจุบันที่ Cell นั้นอยู่

            // คำนวณความกว้างของคอลัมล์
            int startColumnIndex = SelectedExcelRange.Start.Column,
                endColumnIndex = SelectedExcelRange.End.Column;
            double sumColWidth = 0;
            do
            {
                sumColWidth += CurrWorkSheet.Column(startColumnIndex++).Width;
            } while (startColumnIndex <= endColumnIndex);

            // คำนวณความสูงของแถวจาก จำนวนอักษระ และ ความกว้างของคอลัมล์
            var newRowHeight = MeasureTextHeight(cellText, SelectedExcelRange.Style.Font, Convert.ToInt32(Math.Ceiling(sumColWidth)));
            newRowHeight = Math.Max(CurrWorkSheet.Row(rowIndex).Height, newRowHeight);
            CurrWorkSheet.Row(rowIndex).CustomHeight = true;
            CurrWorkSheet.Row(rowIndex).Height = newRowHeight;
        }

    }
}