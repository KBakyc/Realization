using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows.Controls;
using System.ComponentModel;

namespace ServiceModule.ViewModels
{
    public interface IDataGridItemComparer : IComparer
    {
        Dictionary<DataGridColumn, ItemPropertyInfo> ColumnProperties { get; set; }
    }
}
