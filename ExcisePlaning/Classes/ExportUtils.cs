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
        }

        public static double MeasureTextHeight(string text, ExcelFont font, Int32 width)
        {
            if (string.IsNullOrEmpty(text))
                return 0.0;

            //ขนาดความยาวของ คอลัมล์ จะมีผลต่อความสูงของ แถว ดังนั้น 
            //ให้ ลดความยาวของคอลัมล์ลงไป 10 ในกรณีที่ทำ WrapText = true
            //if (width > 10)
            //    width -= 10;

            Bitmap bitmap = new Bitmap(1, 1);
            Graphics g = Graphics.FromImage(bitmap);

            Int32 pixelWidth = Convert.ToInt32(width * 7.5); //7.5 pixels per excel column width
            Font drawingFont = new Font(font.Name, font.Size);
            SizeF Size = g.MeasureString(text, drawingFont, pixelWidth);

            //72 DPI And 96 points per inch.  Excel height in points with max of 409 per Excel requirements.
            return Math.Min(Convert.ToDouble(Size.Height) * 72 / 96, 409);
            //return Math.Min(Convert.ToDouble(Size.Height) * 72 / 76, 409);
        }


        private static int LastRowIndex { get; set; }
        private static double LastRowHeight { get; set; }

        /// <summary>
        /// คำนวนขนาดความสูงของ Excel Row
        /// </summary>
        private static void AutofitRowHeight()
        {
            if (null == SelectedExcelRange || null == SelectedExcelRange.Value)
                return;

            // คำนวณความสูงของ Row
            string cellText = CurrWorkSheet.SelectedRange.Value.ToString();
            int rowIndex = CurrWorkSheet.SelectedRange.Start.Row; // แถวปัจจุบันที่ Cell นั้นอยู่
            int columnIndex = CurrWorkSheet.SelectedRange.Start.Column; // คอลัมล์ปัจจุบันที่ Cell นั้นอยู่
            var rowHeight = MeasureTextHeight(cellText, CurrWorkSheet.SelectedRange.Style.Font, Convert.ToInt32(CurrWorkSheet.Column(columnIndex).Width));
            rowHeight += 5.5;

            // ความสูงที่คำนวณได้ น้อยกว่า ความสูงปัจจุบันของแถว ให้ใช้ความสูงปัจจุบัน
            var currRow = CurrWorkSheet.Row(rowIndex);
            if (rowHeight < currRow.Height)
                rowHeight = currRow.Height;


            if (LastRowIndex != rowIndex)
            {
                LastRowIndex = rowIndex;
                LastRowHeight = rowHeight;
                currRow.CustomHeight = true;
                currRow.Height = rowHeight;
            }
            else if (LastRowHeight < rowHeight)
            {
                LastRowHeight = rowHeight;
                currRow.CustomHeight = true;
                currRow.Height = rowHeight;
            }
        }

    }
}