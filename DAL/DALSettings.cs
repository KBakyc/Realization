using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL
{
    /// <summary>
    /// Предоставляет другим модулям доступ к своим настройкам
    /// </summary>
    public static class DALSettings
    {
        public static bool IsReadOnly
        {
            get { return Properties.Settings.Default.IsReadOnly; }
        }
    }
}
