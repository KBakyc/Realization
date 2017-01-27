using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL
{
    public static class DALSettings
    {
        //public static String JzaPath
        //{
        //    get { return String.IsNullOrWhiteSpace(Properties.Settings.Default.Dbf_Path_Jza) ? Environment.CurrentDirectory : Properties.Settings.Default.Dbf_Path_Jza; }
        //}

        public static bool IsReadOnly
        {
            get { return Properties.Settings.Default.IsReadOnly; }
        }
    }
}
