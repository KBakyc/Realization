using System;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Helpers;
using DataObjects.Interfaces;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace InfoModule.ViewModels
{
    /// <summary>
    /// Модель диалога управления подписантами документов.
    /// </summary>
    public class SignersDlgViewModel : BaseDlgViewModel
    {
        private List<PoupModel> poups;
        private IDbService repository;
        private SignatureInfo[] signatures;
        private Predicate<object> filter;

        public SignersDlgViewModel(IDbService _rep)
        {
            repository = _rep;

            LoadData();
            
            AddSignerCommand = new DelegateCommand(ExecuteAdd, CanAdd);
            DeleteSignerCommand = new DelegateCommand(ExecuteDelete, CanDelete);
            SaveChangesCommand = new DelegateCommand(ExecuteSaveChanges, CanSaveChanges);
        }

        private ObservableCollection<SignerViewModel> signers;
        public ObservableCollection<SignerViewModel> Signers 
        {
            get { return signers; }
            set { SetAndNotifyProperty("Signers", ref signers, value); } 
        }

        private PoupModel selectedPoup;
        public PoupModel SelectedPoup
        {
            get { return selectedPoup; }
            set
            {
                if (value != selectedPoup)
                {
                    selectedPoup = value;
                    if (selectedPoup != null && selectedPoup.Kod > 0)
                        filter = o => (o as SignerViewModel).Poups != null && (o as SignerViewModel).Poups.SingleOrDefault(spm => spm.Value.Kod == selectedPoup.Kod).IsSelected;
                    else
                        filter = null;
                    DoApplyFilter();
                }
            }
        }

        private void DoApplyFilter()
        {
            var view = CollectionViewSource.GetDefaultView(Signers);
            view.Filter = filter;
            view.Refresh();
            view.MoveCurrentToFirst();
        }


        private int selIndex;
        public int SelIndex
        {
            get { return selIndex; }
            set { selIndex = value; }
        }

        public Dictionary<byte, string> SignTypes { get; set; }

        private void LoadData()
        {
            poups = new List<PoupModel>();
            poups.Add(new PoupModel { Kod = 0, Name = "Все"});
            poups.AddRange(repository.Poups.Values.Where(p => p.IsActive));

            SignTypes = repository.GetSignTypes();

            LoadSigners();
        }

        private void LoadSigners()
        {
            signatures = repository.GetSigners(0);

            if (signatures != null)
            {
                if (signers == null)
                    Signers = new ObservableCollection<SignerViewModel>();
                else
                    signers.Clear();
                Array.ForEach(signatures, s => signers.Add(new SignerViewModel(repository, s, TrackingInfo.Unchanged, false)));
            }
        }

        private void RefreshData()
        {
            LoadSigners();
            DoApplyFilter();
        }

        public List<PoupModel> Poups
        {
            get { return poups; }
        }

        public DelegateCommand AddSignerCommand { get; set; }

        private void ExecuteAdd()
        {
            var newsigner = new SignerViewModel(repository, new SignatureInfo(), TrackingInfo.Created, true);
            if (selectedPoup != null && selectedPoup.Kod > 0)
                newsigner.Poups.SingleOrDefault(spm => spm.Value.Kod == selectedPoup.Kod).IsSelected = true;
            signers.Add(newsigner);
            var view = CollectionViewSource.GetDefaultView(Signers);
            view.MoveCurrentTo(newsigner);
        }

        private bool CanAdd()
        {
            return true;
        }

        public DelegateCommand DeleteSignerCommand { get; set; }

        private void ExecuteDelete()
        {
            var view = CollectionViewSource.GetDefaultView(Signers);
            var selsigner = view.CurrentItem as SignerViewModel;
            if (selsigner != null)
            {
                if (selsigner.TrackingState == TrackingInfo.Created)
                {
                    Signers.Remove(selsigner);
                    view.Refresh();
                }
                else
                {
                    var pstr = selsigner.PoupsString;
                    if (!String.IsNullOrEmpty(pstr))
                    {
                        Parent.Services.ShowMsg("Внимание", "Для удаления подписанта необходимо отменить его привязку к видам реализации.\n" + pstr, true);
                        return;
                    }
                    selsigner.TrackingState = TrackingInfo.Deleted;
                }
            }

        }

        private bool CanDelete()
        {
            return true;
        }

        public DelegateCommand SaveChangesCommand { get; set; }

        private void ExecuteSaveChanges()
        {
            // !!! 
            var changes = Signers.Where(s => s.TrackingState != TrackingInfo.Unchanged && s.IsValid);
            foreach(var signer in changes)
                switch (signer.TrackingState)
                {
                    case TrackingInfo.Deleted: repository.DeleteSigner(signer.SignerModel.Id); break;
                    case TrackingInfo.Created:
                    case TrackingInfo.Updated: 
                        var newid = repository.MergeSigner(signer.SignerModel);
                        signer.SignerModel.Id = newid;
                        if (newid > 0)
                            repository.SetSignerPoups(newid, signer.GetPoups());
                        break;
                }

            RefreshData();
        }

        private bool CanSaveChanges()
        {
            var ch = Signers.Where(s => s.TrackingState != TrackingInfo.Unchanged);
            return ch.Any() && ch.All(s => s.IsValid);
        }

    }   
}
