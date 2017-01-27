using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommonModule.Interfaces;

namespace CommonModule.Helpers
{
    public class Remember : IPersister
    {
        private Dictionary<String,Object> Memory = new Dictionary<string, object>();
        
        public T GetValue<T>(String key)
        {
            Object res = null; //typeof(T).IsValueType ? Activator.CreateInstance(typeof(T)) : null;
            T val = default(T);

            if (!String.IsNullOrEmpty(key) && key != "" && Memory.ContainsKey(key))
            {
                res = Memory[key];
                //if (stres.GetType() == typeof(T))
                //    res = stres;
                try
                {
                    if (typeof(T) == typeof(String))
                        val = (T)(Object)res.ToString();
                    else
                        val = (T) res;
                }
                catch
                {
                    val = default(T);
                }
            }

            return val;
        }

        public Object GetValue(String key)
        {
            Object res = null; 
            if (!String.IsNullOrEmpty(key))
                Memory.TryGetValue(key, out res);

            return res;
        }

        public void SetValue(String key, Object val)
        {
            if (!String.IsNullOrEmpty(key) && key != "")
                if (Memory.ContainsKey(key) || val != null)
                    Memory[key] = val;
        }

        public void SaveData(string _tofile)
        {
            _tofile = _tofile.Trim();
            if (String.IsNullOrEmpty(_tofile))
                _tofile = "Memory.xml";

            if (!Path.IsPathRooted(_tofile))
                _tofile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _tofile);

            XDocument xdoc = new XDocument(new XElement(Path.GetFileNameWithoutExtension(_tofile), 
                Memory.Select(kv => new XElement(kv.Key, 
                                                    new XAttribute("Type",kv.Value.GetType()), 
                                                    kv.Value))));
            if (File.Exists(_tofile))
                File.Delete(_tofile);
            xdoc.Save(_tofile);
        }

        public void LoadData(string _fromfile)
        {
            _fromfile = _fromfile.Trim();
            if (String.IsNullOrEmpty(_fromfile))
                _fromfile = "Memory.xml";
            if (!File.Exists(_fromfile)) return;
            
            if (!Path.IsPathRooted(_fromfile))
                _fromfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _fromfile);

            XDocument xdoc = null;

            try
            {
                xdoc = XDocument.Load(_fromfile);
            }
            catch (Exception _e)
            {
                WorkFlowHelper.OnCrash(_e);
            }

            if (xdoc == null || xdoc.Root == null) return;
            
            Memory.Clear();

            foreach (var el in xdoc.Root.Elements())
            {
                XAttribute typeattr = el.Attribute("Type");
                if (typeattr != null)
                {
                    Type t = Type.GetType(typeattr.Value);
                    Object obj = null;
                    if (t == typeof(string))
                        obj = el.Value;
                    else
                        obj = Parser.Parse(el.Value, t);
                    SetValue(el.Name.LocalName, obj);
                }
            }
        }

    }
}