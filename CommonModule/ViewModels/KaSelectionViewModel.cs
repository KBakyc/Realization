using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    public class KaSelectionViewModel : BaseDlgViewModel
    {
        //private bool isKaPopulatedBySearch;
        private IDbService repository;

        public KaSelectionViewModel(IDbService _rep)
            :this(_rep, Enumerable.Empty<KontrAgent>())
        {
        }

        public KaSelectionViewModel(IDbService _rep, IEnumerable<KontrAgent> _kalist)
        {
            repository = _rep;
            //isKaPopulatedBySearch = false;
            kaList = new ObservableCollection<KontrAgent>(_kalist);
            Title = "Выбор контрагента";            
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && SelectedKA != null;
        }

        private ObservableCollection<KontrAgent> kaList;
        public ObservableCollection<KontrAgent> KaList
        {
            get { return kaList; }
        }

        public void PopulateKaList(IEnumerable<KontrAgent> _kalist)
        {
            //isKaPopulatedBySearch = false;
            KaList.Clear();
            foreach (var ka in _kalist)
                KaList.Add(ka);
        }

        private KontrAgent selectedKA;
        public KontrAgent SelectedKA
        {
            get
            {
                return selectedKA;
            }
            set
            {
                selectedKA = value;
                NotifyPropertyChanged("SelectedKA");
            }
        }

        /// <summary>
        /// Искомый код
        /// </summary>
        private int seekKod;
        public String SeekKod
        {
            get { return seekKod.ToString("#"); }
            set
            {
                int v;
                bool convres = int.TryParse(value, out v);
                if (!convres && value != String.Empty)
                    throw (new Exception("Неверный код для поиска"));
                if (v != seekKod)
                {
                    seekKod = v;
                    //SeekKaByKod(seekKod);
                    NotifyPropertyChanged("SeekKod");
                    SelectKaBySeekCode();
                }
                
            }
        }

        /// <summary>
        /// Искомое имя
        /// </summary>
        private string seekName;
        public string SeekName
        {
            get { return seekName; }
            set
            {
                if (value != seekName)
                {
                    seekName = value;
                    //SeekKaByKod(seekKod);
                    NotifyPropertyChanged("SeekName");
                }

            }
        }

        private string seekAny;
        public string SeekAny
        {
            get { return seekAny; }
            set
            {
                if (value != seekAny && !String.IsNullOrEmpty(value))
                {
                    seekAny = value.Trim();
                    if (seekAny.All(c => Char.IsDigit(c)))
                    {
                        SeekKod = seekAny;
                        seekName = null;
                    }
                    else
                    {
                        SeekName = seekAny;
                        seekKod = 0;
                    }
                    //SeekKaByKod(seekKod);
                    NotifyPropertyChanged("SeekAny");
                }

            }
        }


        /// <summary>
        /// Комманда запуска поиска контрагентов
        /// </summary>
        private ICommand seekCommand;
        public ICommand SeekCommand
        {
            get
            {
                if (seekCommand == null)
                    seekCommand = new DelegateCommand(ExecSeekCommand, CanExecSeekCommand);
                return seekCommand;
            }
        }
        private bool CanExecSeekCommand()
        {
            return isSearchEnabled && (seekKod > 0 || !String.IsNullOrEmpty(SeekName));
        }
        private void ExecSeekCommand()
        {
            if (SubmitCommand != null && (IsValid() && seekKod == SelectedKA.Kgr && (String.IsNullOrEmpty(seekName) || SelectedKA.Name.ToLower().Contains(seekName.ToLower()))))
                SubmitCommand.Execute(null);
            else
            {
                SeekKa();
                SelectKaBySeekCode();
            }
        }

        private void SelectKaBySeekCode()
        {
            SelectedKA = KaList != null && KaList.Count > 0 && seekKod > 0 
                                                       ? KaList.SingleOrDefault(k => k.Kgr == seekKod) 
                                                       : null;
        }

        /// <summary>
        /// Ищет контрагентов по части введённого кода
        /// </summary>
        /// <param name="_number"></param>
        public void SeekKa()
        {
            IEnumerable<KontrAgent> kalbynum = null, reskal = null;

            if (seekKod != 0)
                kalbynum = repository.GetKontrAgentsByCodePat(seekKod);


            if (!String.IsNullOrEmpty(SeekName))
            {
                if (kalbynum != null)
                    reskal = kalbynum.Where(k => k.Name.ToUpperInvariant().Contains(SeekName.ToUpperInvariant()));
                else
                    reskal = repository.GetKontrAgentsByNamePat(SeekName);
            }
            else
                reskal = kalbynum;

            KaList.Clear();
            foreach (var l_kal in reskal)
            {
                KaList.Add(l_kal);
            }

            //isKaPopulatedBySearch = true;
        }

        private bool isSearchEnabled = true;

        public bool IsSearchEnabled 
        {
            get { return isSearchEnabled; }
            set { SetAndNotifyProperty("IsSearchEnabled", ref isSearchEnabled, value); }
        }
    }
}
