using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace CommonModule.Helpers
{
    public static class Enumerations
    {
        //public static string GetEnumDescription(this Enum value)
        //{
        //    FieldInfo fi = value.GetType().GetField(value.ToString());

        //    DescriptionAttribute[] attributes =
        //        (DescriptionAttribute[])fi.GetCustomAttributes(
        //        typeof(DescriptionAttribute),
        //        false);

        //    if (attributes != null &&
        //        attributes.Length > 0)
        //        return attributes[0].Description;
        //    else
        //        return value.ToString();
        //}
        
        public static string GetEnumDescription<TEnum>(this TEnum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }

        public static Dictionary<TEnum, string> GetAllValuesAndDescriptions<TEnum>() where TEnum : struct, IComparable, IFormattable, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException("TEnum must be an Enumeration type");            
            
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToDictionary(e => e , e => e.GetEnumDescription());
        }       
    }
}
