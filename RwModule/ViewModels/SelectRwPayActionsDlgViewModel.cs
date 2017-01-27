using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;
using CommonModule.ViewModels;
using RwModule.Models;
using DAL;

namespace RwModule.ViewModels
{
    public class SelectRwPayActionsDlgViewModel : BaseDlgViewModel
    {
        private Selectable<RwPayActionViewModel>[] payActions;

        public SelectRwPayActionsDlgViewModel(RwPaysArc[] _pa)
        {
            LoadData(_pa);
        }

        private void LoadData(RwPaysArc[] _pa)
        {
            using (var db = new RealContext())
            {
                payActions = _pa.Select(a => new Selectable<RwPayActionViewModel>(RwPayActionViewModel.FromRwPaysArc(a, db), false))
                    .OrderBy(pa => pa.Value.DatDoc).ThenBy(pa => pa.Value.NumDoc).ThenBy(pa => pa.Value.DatPlat).ThenBy(pa => pa.Value.NumPlat)
                    .ToArray();
            }
        }

        private bool isAllSelected;
        public bool IsAllSelected
        {
            get { return isAllSelected; }
            set
            {
                Array.ForEach(PayActions, pa => pa.IsSelected = value);
                SetAndNotifyProperty("IsAllSelected", ref isAllSelected, value);
            }
        }

        public Selectable<RwPayActionViewModel>[] PayActions
        {
            get { return payActions; }
        }

        public RwPayActionViewModel[] SelectedPayActions
        {
            get
            {
                return payActions.Where(spa => spa.IsSelected)
                                 .Select(spa => spa.Value)                                 
                                 .ToArray();
            }
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && payActions.Any(pa => pa.IsSelected);
        }
    }
}
