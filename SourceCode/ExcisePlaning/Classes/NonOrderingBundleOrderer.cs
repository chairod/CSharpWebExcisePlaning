using System.Collections.Generic;
using System.Web.Optimization;

namespace ExcisePlaning.Classes
{
    /// <summary>
    /// เนื่องจาก ค่าตั้งต้นของการจัดเรียง bundle script/style
    /// ใช้ตัวอักษร ในการจัดเรียง ดังนั้น หากไม่ต้องการใช้ค่าเริ่มต้นของ Bundle ให้เรียก Class นี้
    /// ScriptBundle bundle = new ScriptBundle()
    /// bundle.orderer = new NonOrderingBundleOrderer()
    /// ผลลัพธ์ที่ได้จะจัดเรียงตามลำดับการวางไฟล์ใน Bundle
    /// </summary>
    public class NonOrderingBundleOrderer : IBundleOrderer
    {
        public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
        {
            return files;
        }
    }
}