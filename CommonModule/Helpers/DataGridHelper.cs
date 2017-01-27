using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml.Linq;

namespace CommonModule.Helpers
{
    public static class DataGridHelper
    {
        public static void SaveColumns(this DataGrid _dg)
        {
            if (_dg == null || String.IsNullOrEmpty(_dg.Name) || CommonModule.CommonSettings.Persister == null) return;

            var keyName = _dg.Name + "Columns";
            string columnsStr = null;
            var columns = _dg.Columns.Where(c => c.Header != null && !String.IsNullOrWhiteSpace(c.Header.ToString()) && c.DisplayIndex >= 0 ).Select(c => 
                new XElement("Column", 
                    new XAttribute("Header", c.Header.ToString()), 
                    new XAttribute("DisplayIndex", c.DisplayIndex), 
                    new XAttribute("ActualWidth", c.ActualWidth)
                    )).ToArray();
            if (columns != null && columns.Length > 0)
                columnsStr = new XElement(keyName, columns).ToString(SaveOptions.DisableFormatting);

            CommonModule.CommonSettings.Persister.SetValue(keyName, columnsStr);
        }

        public static void LoadColumns(this DataGrid _dg)
        {
            if (_dg == null || String.IsNullOrEmpty(_dg.Name) || CommonModule.CommonSettings.Persister == null) return;
            var keyName = _dg.Name + "Columns";
            string columnsStr = CommonModule.CommonSettings.Persister.GetValue<string>(keyName);
            if (!String.IsNullOrEmpty(columnsStr))
            {
                XElement columnsXML = null;
                try
                {
                    columnsXML = XElement.Parse(columnsStr);
                }
                catch { }
                if (columnsXML != null && columnsXML.HasElements)
                    foreach (var xcol in columnsXML.Elements())
                    {
                        var headerAttr = xcol.Attribute("Header");
                        var indexAttr = xcol.Attribute("DisplayIndex");
                        var widthAttr = xcol.Attribute("ActualWidth");
                        if (headerAttr != null && !String.IsNullOrWhiteSpace(headerAttr.Value))
                        {
                            var dgCol = _dg.Columns.FirstOrDefault(c => c.Header != null && c.Header.ToString() == headerAttr.Value);
                            if (dgCol != null)
                            {
                                int index = 0;
                                if (indexAttr != null && !String.IsNullOrWhiteSpace(indexAttr.Value) && int.TryParse(indexAttr.Value, out index) && index > 0 && index < _dg.Columns.Count)
                                    dgCol.DisplayIndex = index;
                                double width = 0.0;
                                if (widthAttr != null && !String.IsNullOrWhiteSpace(widthAttr.Value) && double.TryParse(widthAttr.Value, out width))
                                    dgCol.Width = width;
                            }
                        }
                    }
            }
        }

        public static void LoadSortDescr(this DataGrid _dg)
        {
            if (_dg == null || String.IsNullOrEmpty(_dg.Name) || CommonModule.CommonSettings.Persister == null) return;

            var valueKey = _dg.Name + "SortOrder";
            var sortinfos = CommonModule.CommonSettings.Persister.GetValue<string>(valueKey);
            if (!String.IsNullOrEmpty(sortinfos))
            {
                var sortdescrStrs = sortinfos.Split(';');
                if (sortdescrStrs.Length > 0)
                {
                    var sortdescr = sortdescrStrs.Select(ds => ds.Split(',')).Where(dsa => dsa.Length == 2 && !String.IsNullOrEmpty(dsa[0]) && (dsa[1] == "A" || dsa[1] == "D"))
                        .Select(dsa => new SortDescription(dsa[0], (dsa[1]) == "A" ? ListSortDirection.Ascending : ListSortDirection.Descending)).ToArray();
                    if (sortdescr.Length > 0)
                        SetSortInfo(_dg, sortdescr);
                }
            }
        }

        public static void SaveSortDescr(this DataGrid _dg)
        {
            if (_dg == null || String.IsNullOrEmpty(_dg.Name) || CommonModule.CommonSettings.Persister == null) return;

            var valueKey = _dg.Name + "SortOrder";
            var sortdescr = GetSortInfo(_dg);
            var descrstr = String.Join(";", sortdescr.Select(d => d.PropertyName + "," + (d.Direction == ListSortDirection.Ascending ? "A" : "D")).ToArray());
            CommonModule.CommonSettings.Persister.SetValue(valueKey, descrstr);
        }

        //public static List<SortDescription> GetSortInfo(DataGrid dg)
        //{
        //    List<SortDescription> sortInfos = new List<SortDescription>();
        //    foreach (var sortDescription in dg.Items.SortDescriptions)
        //    {
        //        sortInfos.Add(sortDescription);
        //    }
        //    return sortInfos;
        //}
                

        public static List<SortDescription> GetSortInfo(DataGrid dg)
        {
            List<SortDescription> sortInfos = new List<SortDescription>();
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(dg.ItemsSource);
            if (view != null && view.SortDescriptions != null)
                sortInfos = view.SortDescriptions.ToList();            
            return sortInfos;
        }

        public static void SetSortInfo(this DataGrid dg, params SortDescription[] sortInfos)
        {
            dg.Items.SortDescriptions.Clear();
            foreach (var sortInfo in sortInfos)
            {
                dg.Items.SortDescriptions.Add(sortInfo);
                var col = dg.Columns.FirstOrDefault(c => c.SortMemberPath == sortInfo.PropertyName);
                if (col != null)
                    col.SortDirection = sortInfo.Direction;
            }
        }
        
    }
}
