using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace ExcisePlaning.Classes.Mappers
{
    public class ModelValidateErrorProperty
    {
        /// <summary>
        /// ตรวจสอบความถูกต้องของข้อมูล โดยใช้ Data Annotation ที่กำหนดไว้ใน Class 
        /// ได้ค่า null ถ้าไม่มีข้อผิดพลาดใดๆ
        /// </summary>
        /// <param name="cls"></param>
        /// <returns></returns>
        public static Dictionary<string, ModelValidateErrorProperty> TryOneValidate(object cls)
        {
            // ตรวจสอบ Data Annotation ของแต่ละ Property ใน Class
            var context = new ValidationContext(cls);
            var validateErrorFields = new List<ValidationResult>();
            Validator.TryValidateObject(cls, context, validateErrorFields, true);
            if (validateErrorFields.Count > 0)
            {
                // อ่านค่าทุก Field ที่ตรวจสอบค่าไม่ผ่าน
                Dictionary<string, ModelValidateErrorProperty> result = new Dictionary<string, ModelValidateErrorProperty>();
                validateErrorFields.ForEach(errorFieldProp => {
                    string fieldName = errorFieldProp.MemberNames.First();
                    result.Add(fieldName, new ModelValidateErrorProperty(fieldName, new List<string>() { errorFieldProp.ErrorMessage }));
                });
                return result;
            }

            return null;
        }

        /// <summary>
        /// ตรวจสอบความถูกต้องของข้อมูลโดยใช้ Data Annotation ผ่าน ModelState
        /// </summary>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public static Dictionary<string, ModelValidateErrorProperty> TryValidate(ModelStateDictionary modelState)
        {
            Dictionary<string, ModelValidateErrorProperty> result = new Dictionary<string, ModelValidateErrorProperty>();
            if (modelState.IsValid)
                return result;

            short index = 0;
            foreach (string fieldName in modelState.Keys)
            {
                ModelValidateErrorProperty errorItem = new ModelValidateErrorProperty
                {
                    FieldName = Regex.Replace(fieldName, @"^\w+\.", "", RegexOptions.IgnoreCase),
                    ErrorMessages = modelState.Skip(index++).Take(1).First().Value.Errors.Select(x => x.ErrorMessage).ToList()
                };

                if (errorItem.ErrorMessages.Count > 0)
                    result.Add(errorItem.FieldName, errorItem);
            }

            return result;
        }

        public ModelValidateErrorProperty() { }
        public ModelValidateErrorProperty(string fieldName, List<string> errorMessages)
        {
            FieldName = fieldName;
            ErrorMessages = errorMessages;
        }

        public string FieldName { get; set; }
        public List<string> ErrorMessages { get; set; }
    }
}