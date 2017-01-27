using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CommonModule.Helpers
{
    public static class Parser
    {
        /// <summary>
        /// Преобразует строку в указанный тип
        /// </summary>
        /// <param name="Input"></param>
        /// <param name="OutputType"></param>
        /// <returns></returns>
        public static object Parse(string Input, Type OutputType)
        {
            object res = null;
            object Item = null;
            object[] Values = null;
            
            try
            {
                if (string.IsNullOrEmpty(Input) || OutputType == null)
                    return null;
                Type[] MethodInputType = new Type[2];
                MethodInputType[0] = typeof(string);
                MethodInputType[1] = typeof(IFormatProvider);
                MethodInfo ParseMethod = OutputType.GetMethod("Parse", MethodInputType);                
                if (ParseMethod != null)
                {
                    Values = new object[2];
                    Values[0] = Input;
                    Values[1] = System.Globalization.CultureInfo.InvariantCulture;
                }
                else
                {
                    MethodInputType = new Type[1];
                    MethodInputType[0] = typeof(string);
                    ParseMethod = OutputType.GetMethod("Parse", MethodInputType);
                    if (ParseMethod != null)
                    {
                        Values = new object[1];
                        Values[0] = Input;
                    }
                }
                if (ParseMethod != null)
                {
                    Item = Activator.CreateInstance(OutputType);
                    res = ParseMethod.Invoke(Item, Values);
                }               
            }
            catch {}

            return res;
        }

        public static T Parse<T>(string Input)
        {
            if (string.IsNullOrEmpty(Input)) return default(T);
            Type OutputType = typeof(T);
            T res = default(T);

            try
            {
                Type[] MethodInputType = new Type[1];
                MethodInputType[0] = typeof(string);
                MethodInfo ParseMethod = OutputType.GetMethod("Parse", MethodInputType);
                if (ParseMethod != null)
                {
                    object Item = Activator.CreateInstance(OutputType);
                    object[] Values = new object[1];
                    Values[0] = Input;
                    res = (T)ParseMethod.Invoke(Item, Values);
                }
            }
            catch { throw; }

            return res;
        }

        public static T[] ParseMultiValued<T>(string Input)
        {
            T[] res = null;
            if (string.IsNullOrEmpty(Input)) return null;
            string[] svalues = Input.Split(',');
            res = svalues.Select(s => Parse<T>(s)).ToArray();

            return res;
        }

        public static int GetIntFromString(string _s)
        {
            if (String.IsNullOrWhiteSpace(_s)) return 0;
            string resultString = System.Text.RegularExpressions.Regex.Match(_s, @"\d+").Value;
            int res = 0;
            int.TryParse(resultString, out res);
            return res;
        }
    }
}
