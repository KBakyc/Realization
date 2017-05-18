using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Windows.Input;

namespace SfModule.ViewModels
{
    /// <summary>
    /// Модель диалога выбора отгрузки для формирования корректировочного счёта-фактуры на продукцию.
    /// </summary>
    public class CorrsfOtgrDocsViewModel : OtgrDocsDlgViewModel
    {
        public CorrsfOtgrDocsViewModel(IDbService _rep, IEnumerable<OtgrDocModel> _docs)
            :base(_rep, _docs)//, o => o.IdCorrsf == 0)
        {
            otgrDocsVM.SubscribeToSelection();
            PropertyChanged += new PropertyChangedEventHandler(CorrsfOtgrDocsViewModel_PropertyChanged);
            SplitOtgrCommand = new DelegateCommand(ExecSplitOtgr, CanSplitOtgr);  
        }

        void CorrsfOtgrDocsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == GetPropName(() => otgrDocsVM.SelectedOtgr))
            {
                PartOldCenProd = otgrDocsVM.SelectedOtgr.Value.Cenprod;
            }
        }

        private decimal newKolf;
        public decimal NewKolf
        {
            get { return newKolf; }
            set { SetAndNotifyProperty("NewKolf", ref newKolf, value); }
        }
        
        private decimal partOldCenProd;
        public decimal PartOldCenProd
        {
            get { return partOldCenProd; }
            set { SetAndNotifyProperty("PartOldCenProd", ref partOldCenProd, value); }
        }

        private decimal partNewCenProd;
        public decimal PartNewCenProd
        {
            get { return partNewCenProd; }
            set { SetAndNotifyProperty("PartNewCenProd", ref partNewCenProd, value); }
        }

        public ICommand SplitOtgrCommand { get; set; }

        private void ExecSplitOtgr()
        {
            var selotgr = otgrDocsVM.SelectedOtgr;
            if (newKolf > 0)
            {
                var oldOtgr = selotgr.Value.ModelRef;
                var newOtgr = DeepCopy.Make(oldOtgr);
                newOtgr.Kolf = newKolf;
                newOtgr.Cenprod = partOldCenProd;
                selotgr.Value.Kolf -= newKolf;
                newOtgr.Sumprod = partNewCenProd - newOtgr.Cenprod;
                var newOtgrVM = new Selectable<OtgrDocViewModel>(new OtgrDocViewModel(newOtgr, repository), false);

                var oldIndex = otgrDocsVM.OtgrDocs.IndexOf(selotgr);
                otgrDocsVM.OtgrDocs.Insert(oldIndex + 1, newOtgrVM);
            }
            else
            {
                selotgr.Value.Cenprod = partOldCenProd;
                selotgr.Value.Sumprod = partNewCenProd - partOldCenProd;
            }

            NewKolf = 0;
            PartNewCenProd = 0;
            otgrDocsVM.SubscribeToSelection();
        }

        private bool CanSplitOtgr()
        {
            return otgrDocsVM != null && otgrDocsVM.SelectedOtgr != null 
                && (newKolf == 0 || newKolf > 0 && newKolf < otgrDocsVM.SelectedOtgr.Value.ModelRef.Kolf)
                && partNewCenProd > 0;
        }

        /// <summary>
        /// Информация о первоначальном договоре
        /// </summary>
        public PDogInfoViewModel InPDogInfo { get; set; }

        /// <summary>
        /// Инвормация о новом договоре
        /// </summary>
        private PDogInfoViewModel outPDogInfo;
        public PDogInfoViewModel OutPDogInfo 
        {
            get { return outPDogInfo; }
            set
            {
                if (value != outPDogInfo)
                {
                    outPDogInfo = value;
                    NewCenaProd = outPDogInfo.ModelRef.Cenaprod;
                    NotifyPropertyChanged("OutPDogInfo");
                }
            }
        }

        /// <summary>
        /// Новая цена
        /// </summary>
        public decimal NewCenaProd { get; set; }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelectedOtgrDocs.All(d => d.Sumprod != 0 || d.Cenprod != NewCenaProd && NewCenaProd > 0);
        }

    }
}
