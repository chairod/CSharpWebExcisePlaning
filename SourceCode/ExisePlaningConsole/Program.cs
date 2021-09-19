using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExisePlaningConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //doAdminUser();
            //doOtherUser();
        }

        static void doOtherUser()
        {
            FileInfo fileinfo = new FileInfo(@"D:\Temp\user.xlsx");
            using (ExcelPackage xls = new ExcelPackage(fileinfo))
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var ws = xls.Workbook.Worksheets[1];
                int rowIndex = 2;
                do
                {
                    object cardNumber = ws.Cells[string.Format("E{0}", rowIndex)].Value;
                    if (null == cardNumber)
                        break;

                    string emailAddr = ws.Cells[string.Format("F{0}", rowIndex)].Value.ToString();
                    string prefixName = ws.Cells[string.Format("A{0}", rowIndex)].Value.ToString();
                    string depName = ws.Cells[string.Format("H{0}", rowIndex)].Value.ToString();
                    int depId = Convert.ToInt32(ws.Cells[string.Format("J{0}", rowIndex)].Value.ToString());
                    string accLevel = ws.Cells[string.Format("G{0}", rowIndex)].Value.ToString();

                    db.T_PERSONNEL_SSO_PREPAREs.InsertOnSubmit(new T_PERSONNEL_SSO_PREPARE()
                    {
                        CARD_NUMBER = cardNumber.ToString(),
                        DEFAULT_ROLE_ID = 5,
                        DEFAULT_DEP_ID = depId,
                        DEFAULT_PERSON_TYPE_ID = 1, //ข้าราชการ 
                        DEFAULT_LEVEL_ID = 1, // ระดับปฏิบัติการ
                        DEFAULT_POSITION_ID = 28, // เจ้าพนักงานพัสดุ
                        DEFAULT_SEX_TYPE = prefixName.Contains("นาย") ? "M" : "F",
                        DEFAULT_ACC_TYPE = Convert.ToInt16("ผู้ปฏิบัติ".Equals(accLevel) ? 0 : 2),
                        DEFAULT_EMAIL_ADDR = emailAddr
                    });
                    var depExpr = db.T_DEPARTMENTs.Where(e => e.DEP_NAME.Equals(depName)).FirstOrDefault();
                    if(null != depExpr)
                        ws.Cells[string.Format("J{0}", rowIndex)].Value = depExpr.DEP_ID;

                    Console.WriteLine("[{0}] Card Number: {1}", rowIndex, cardNumber);
                    rowIndex++;
                } while (true);

               // xls.Save();
                Console.WriteLine("Saving ...");
                db.SubmitChanges();
                Console.WriteLine("Done Please any key to close ...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }


        static void doAdminUser()
        {
            FileInfo fileinfo = new FileInfo(@"D:\Temp\admin.xlsx");
            using (ExcelPackage xls = new ExcelPackage(fileinfo))
            using (ExcisePlaningDbDataContext db = new ExcisePlaningDbDataContext())
            {
                var ws = xls.Workbook.Worksheets[1];
                int rowIndex = 2;
                do
                {
                    object cardNumber = ws.Cells[string.Format("E{0}", rowIndex)].Value;
                    if (null == cardNumber)
                        break;

                    string emailAddr = ws.Cells[string.Format("F{0}", rowIndex)].Value.ToString();
                    string prefixName = ws.Cells[string.Format("A{0}", rowIndex)].Value.ToString();

                    db.T_PERSONNEL_SSO_PREPAREs.InsertOnSubmit(new T_PERSONNEL_SSO_PREPARE()
                    {
                        CARD_NUMBER = cardNumber.ToString(),
                        DEFAULT_ROLE_ID = 1,
                        DEFAULT_DEP_ID = 64,
                        DEFAULT_PERSON_TYPE_ID = 1, //ข้าราชการ 
                        DEFAULT_LEVEL_ID = 1, // ระดับปฏิบัติการ
                        DEFAULT_POSITION_ID = 28, // เจ้าพนักงานพัสดุ
                        DEFAULT_SEX_TYPE = prefixName.Contains("นาย") ? "M" : "F",
                        DEFAULT_ACC_TYPE = 2,
                        DEFAULT_EMAIL_ADDR = emailAddr
                    });

                    Console.WriteLine("[{0}] Card Number: {1}", rowIndex, cardNumber);
                    rowIndex++;
                } while (true);

                Console.WriteLine("Saving ...");
                db.SubmitChanges();
                Console.WriteLine("Done Please any key to close ...");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }
    }
}
