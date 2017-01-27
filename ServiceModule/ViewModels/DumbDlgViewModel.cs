using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.Interfaces;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using SfModule.ViewModels;
using DataObjects.Interfaces;
using CommonModule.DataViewModels;
using System.Globalization;

namespace ServiceModule.ViewModels
{
    public class DumbDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public DumbDlgViewModel(IDbService _rep)
        {
            repository = _rep;
            //System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("ru-RU");
            //ci.CompareInfo = ;
            //System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            //System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            Products = new ObservableCollection<ProductInfo>() { new ProductInfo{ Kpr = 1, Name = "520"}, new ProductInfo{ Kpr = 1, Name = "5-2"}, new ProductInfo{ Kpr = 3, Name = "5/2"}, new ProductInfo{ Kpr = 1, Name = "5-3"}, new ProductInfo{ Kpr = 5, Name = "5201"}};
            Cultures = new ObservableCollection<CultureInfo>(CultureInfo.GetCultures(CultureTypes.AllCultures));//.Select(c=>c.CompareInfo));
            //sorted = Strings.OrderBy(s => s, StringComparer.Ordinal).ToArray();    
            //foreach (var c in Cultures)
            //{
            //    cnt++;
            //    compareres = (new System.Collections.Comparer(c)).Compare("520", "5-3");
            //    if (compareres > 0)
            //        ci = c;
            //}
        }

        private int cnt;
        private CultureInfo ci;
        private int compareres;
        private String[] sorted;

        public ObservableCollection<ProductInfo> Products { get; set; }
        public ObservableCollection<CultureInfo> Cultures { get; set; }
    }
}