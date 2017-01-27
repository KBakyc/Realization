using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace CommonModule.Helpers
{
    public static class PanelsHelper
    {
        public static void SaveGridColumns(this Grid _g) 
        {
            if (_g == null || String.IsNullOrEmpty(_g.Name) || CommonModule.CommonSettings.Persister == null) return;

            string cols = String.Join(",", _g.ColumnDefinitions.Select(cd => cd.Width.ToString()).ToArray());
            var valueKey = _g.Name + "ColumnDefinitions";
            CommonModule.CommonSettings.Persister.SetValue(valueKey, cols);
        }

        public static void LoadGridColumns(this Grid _g)
        {
            if (_g == null || String.IsNullOrEmpty(_g.Name) || CommonModule.CommonSettings.Persister == null) return;

            try
            {
                var valueKey = _g.Name + "ColumnDefinitions";
                var colsstr = CommonModule.CommonSettings.Persister.GetValue<string>(valueKey);
                if (!String.IsNullOrEmpty(colsstr))
                {
                    var conv = new GridLengthConverter();
                    var cols = colsstr.Split(',').Select(cs => (GridLength)conv.ConvertFromInvariantString(cs)).ToArray();
                    for (int i = 0; i < _g.ColumnDefinitions.Count; i++)
                        _g.ColumnDefinitions[i].Width = cols[i];
                }
            }
            catch (Exception _e)
            {
                WorkFlowHelper.OnCrash(_e, "Ошибка загрузки сохранённых размеров колонок таблицы " + _g.Name);
            }
        }

        public static void SaveGridRows(this Grid _g)
        {
            if (_g == null || String.IsNullOrEmpty(_g.Name) || CommonModule.CommonSettings.Persister == null) return;

            string rows = String.Join(",", _g.RowDefinitions.Select(rd => rd.Height.ToString()).ToArray());
            var valueKey = _g.Name + "RowDefinitions";
            CommonModule.CommonSettings.Persister.SetValue(valueKey, rows);
        }

        public static void LoadGridRows(this Grid _g)
        {
            if (_g == null || String.IsNullOrEmpty(_g.Name) || CommonModule.CommonSettings.Persister == null) return;

            try
            {
                var valueKey = _g.Name + "RowDefinitions";
                var rowsstr = CommonModule.CommonSettings.Persister.GetValue<string>(valueKey);
                if (!String.IsNullOrEmpty(rowsstr))
                {
                    var conv = new GridLengthConverter();
                    var rows = rowsstr.Split(',').Select(rs => (GridLength)conv.ConvertFromString(rs)).ToArray();
                    for (int i = 0; i < _g.RowDefinitions.Count; i++)
                        _g.RowDefinitions[i].Height = rows[i];
                }
            }
            catch (Exception _e)
            {
                WorkFlowHelper.OnCrash(_e, "Ошибка загрузки сохранённых размеров строк таблицы " + _g.Name);
            }
        }
    }
}
