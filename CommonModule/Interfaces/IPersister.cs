using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Interfaces
{
    public interface IPersister
    {
        T GetValue<T>(String key);
        Object GetValue(String key);
        void SetValue(String key, Object val);
        void SaveData(string path);
        void LoadData(string path);
    }
}
