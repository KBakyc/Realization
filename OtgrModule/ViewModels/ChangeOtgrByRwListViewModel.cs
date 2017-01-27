using System;
using System.Linq;
using System.Windows.Input;
using CommonModule.Commands;
using CommonModule.Helpers;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace OtgrModule.ViewModels
{
    public class RwPaysData
    {
        public decimal Sper { get; set; }
        public decimal NdsSper { get; set; }
        public decimal NdsStSper { get; set; }
        public decimal DopUsl { get; set; }
        public decimal NdsDop { get; set; }
        public decimal NdsStDop { get; set; }
    }

    public class ChangeOtgrByRwListViewModel : BaseDlgViewModel
    {
        IDbService repository;
        IEnumerable<OtgrLine> rwListData;

        public ChangeOtgrByRwListViewModel(IDbService _repository, IEnumerable<OtgrLine> _otgrData, IEnumerable<OtgrLine> _rwListData, bool _isAuto)
        {
            if (_otgrData == null) throw (new ArgumentNullException("_otgrData", "Нет данных для отображения"));
            if (_rwListData == null) throw (new ArgumentNullException("_rwListData", "Нет данных для отображения"));

            repository = _repository;
            rwListData = _rwListData;

            LoadOtgruz(_otgrData);
            ProcessChangesInOtgruzByRwList(_isAuto);
            CheckOtgr();
            SubscribeChanges();            
        }

        private void SubscribeChanges()
        {
            foreach (var ovm in otgrData)
                ovm.PropertyChanged += Otgruz_PropertyChanged;
        }

        private void LoadOtgruz(IEnumerable<OtgrLine> _otgrData)
        {
            foreach (var o in _otgrData.OrderBy(d => d.RwBillNumber).ThenBy(d => d.Idrnn))
            {
                var ovm = new OtgrLineViewModel(repository, o);
                otgrData.Add(ovm);
                oldOtgrsData[o.Idp623] = new RwPaysData { Sper = o.Sper, NdsSper = o.Ndssper, NdsStSper = o.Nds, DopUsl = o.Dopusl, NdsDop = o.Ndsdopusl, NdsStDop = o.Ndst_dop };
            }
        }

        void Otgruz_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var ovm = sender as OtgrLineViewModel;
            var verifiedProperties = new string[] {"Sper", "Ndssper", "Nds", "Dopusl", "Ndsdopusl", "Ndst_dop" };
            if (ovm != null && verifiedProperties.Contains(e.PropertyName))
                VerifyOtgruz(ovm);
        }

        private const int OTGRSTATUS_NEGATIVE = 95;
        private const int OTGRSTATUS_ERRORNAKL = 90;

        private void VerifyOtgruz(OtgrLineViewModel _ovm)
        {
            var rwBillNumber = _ovm.RwBillNumber;
            var idp623 = _ovm.Otgr.Idp623;
            short oldStatus = _ovm.StatusType;
            short newNaklStatus = 0;

            if (_ovm.Sper < 0 || _ovm.Ndssper < 0 || _ovm.Dopusl < 0 || _ovm.Ndsdopusl < 0 || _ovm.Ndst_dop < 0)
            {
                if (oldStatus < OTGRSTATUS_NEGATIVE)
                {
                    _ovm.StatusType = OTGRSTATUS_NEGATIVE;
                    _ovm.StatusMsgs = new string[] { "Отрицательные значения не допустимы" };
                }
            }
            else
                if (oldStatus == OTGRSTATUS_NEGATIVE)
                {
                    _ovm.StatusType = 0;
                    _ovm.StatusMsgs = null;
                }

            CurNakl = CollectCurNakldata(rwBillNumber);
            if (curNakl.Sper != newNakl.Sper || curNakl.NdsSper != newNakl.NdsSper || curNakl.DopUsl != newNakl.DopUsl || curNakl.NdsDop != newNakl.NdsDop || curNakl.NdsStDop != newNakl.NdsStDop)
                newNaklStatus = OTGRSTATUS_ERRORNAKL;
                
            foreach (var o in otgrData.Where(d => d.RwBillNumber == rwBillNumber))
            {
                o.StatusType = newNaklStatus > o.StatusType || o.StatusType == OTGRSTATUS_ERRORNAKL ? newNaklStatus : o.StatusType;
                if (o.StatusType == newNaklStatus)
                    o.StatusMsgs = (newNaklStatus == OTGRSTATUS_ERRORNAKL) ? new string[] {"Несоответствие введённых данных по накладной расчётным"} : null;
            }

            UpdateTrackingState(_ovm);
            NotifyCorrectness();
        }

        private void UpdateTrackingState(OtgrLineViewModel _ovm)
        {
            var idp623 = _ovm.Otgr.Idp623;
            var oldData = oldOtgrsData[idp623];
            if (oldData.Sper == _ovm.Sper
                && oldData.NdsSper == _ovm.Ndssper
                && oldData.NdsStSper == _ovm.Nds
                && oldData.DopUsl == _ovm.Dopusl
                && oldData.NdsDop == _ovm.Ndsdopusl
                && oldData.NdsStDop == _ovm.Ndst_dop)
                _ovm.TrackingState = TrackingInfo.Unchanged;
            else
                _ovm.TrackingState = TrackingInfo.Updated;
        }

        private void CheckOtgr()
        {
            if (otgrData == null) return;
            foreach (var otgr in otgrData.Where(o => o.OtgrAllSfs.Any(s => s.Status != LifetimeStatuses.Deleted)))
            {
                otgr.StatusType = 100;
                otgr.StatusMsgs = new string[] { "Сформирован счёт № " + otgr.OtgrAllSfs.First(s => s.Status != LifetimeStatuses.Deleted).NumSf.ToString() };
            }
        }

        /// <summary>
        /// Коллекция строк принимаемой отгрузки
        /// </summary>
        private List<OtgrLineViewModel> otgrData = new List<OtgrLineViewModel>();
        public List<OtgrLineViewModel> OtgrData
        {
            set
            {
                if (value != otgrData)
                {
                    otgrData = value;
                    NotifyPropertyChanged("OtgrData");
                }
            }
            get
            {
                return otgrData;
            }
        }

        public IEnumerable<OtgrLine> GetOtgrForUpdate()
        {
            bool isvalid = IsValid();
            return OtgrData.Where(d => isvalid && d.TrackingState == TrackingInfo.Updated && d.StatusType == 0).Select(d => d.Otgr);
        }

        private OtgrLineViewModel selectedOtgr;
        public OtgrLineViewModel SelectedOtgr
        {
            get { return selectedOtgr; }
            set
            {
                if (value != selectedOtgr)
                {
                    var oldRnn = selectedOtgr == null ? null : selectedOtgr.RwBillNumber;
                    var newRnn = value == null ? null : value.RwBillNumber;

                    selectedOtgr = value;
                    NotifyPropertyChanged("SelectedOtgr");

                    if (newRnn != null && oldRnn != newRnn)
                        CollectAndNotifySelectedNaklData();
                }
            }
        }

        private RwPaysData oldNakl;
        public RwPaysData OldNakl
        {
            get { return oldNakl; }
            set { SetAndNotifyProperty("OldNakl", ref oldNakl, value); }
        }

        private RwPaysData difNakl;
        public RwPaysData DifNakl
        {
            get { return difNakl; }
            set { SetAndNotifyProperty("DifNakl", ref difNakl, value); }
        }
        
        private RwPaysData newNakl;
        public RwPaysData NewNakl
        {
            get { return newNakl; }
            set { SetAndNotifyProperty("NewNakl", ref newNakl, value); }
        }

        private RwPaysData curNakl;
        public RwPaysData CurNakl
        {
            get { return curNakl; }
            set { SetAndNotifyProperty("CurNakl", ref curNakl, value); }
        }

        public bool IsCurSperCorrect
        {
            get
            {
                return curNakl == null || curNakl.Sper == newNakl.Sper;
            }
        }

        public bool IsCurNdsSperCorrect
        {
            get
            {
                return curNakl == null || curNakl.NdsSper == newNakl.NdsSper;
            }
        }

        public bool IsCurDopUslCorrect
        {
            get
            {
                return curNakl == null || curNakl.DopUsl == newNakl.DopUsl;
            }
        }

        public bool IsCurNdsDopCorrect
        {
            get
            {
                return curNakl == null || curNakl.NdsDop == newNakl.NdsDop;
            }
        }

        private void NotifyCorrectness()
        {
            NotifyPropertyChanged("IsCurSperCorrect");
            NotifyPropertyChanged("IsCurNdsSperCorrect");
            NotifyPropertyChanged("IsCurDopUslCorrect");
            NotifyPropertyChanged("IsCurNdsDopCorrect");
        }

        private void CollectAndNotifySelectedNaklData()
        {
            if (selectedOtgr == null) return;
            var rwBillNumber = selectedOtgr.RwBillNumber;
            CurNakl = CollectCurNakldata(rwBillNumber);
            OldNakl = oldNaklsData[rwBillNumber];
            DifNakl = difNaklsData[rwBillNumber];
            NewNakl = newNaklsData[rwBillNumber];
            NotifyCorrectness();
        }

        private RwPaysData CollectCurNakldata(string _rwBillNumber)
        {
            RwPaysData res = new RwPaysData();

            foreach (var o in otgrData.Where(d => d.RwBillNumber == _rwBillNumber))
            {
                res.Sper += o.Sper;
                res.NdsSper += o.Ndssper;
                res.DopUsl += o.Dopusl;
                res.NdsDop += o.Ndsdopusl;
                res.NdsStDop = o.Ndst_dop;
            }

            return res;
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && otgrData.Any(d => d.TrackingState == TrackingInfo.Updated)
                && !otgrData.Any(d => d.StatusType > 0);
        }

        private Dictionary<string, RwPaysData> oldNaklsData = new Dictionary<string,RwPaysData>();
        private Dictionary<string, RwPaysData> difNaklsData = new Dictionary<string, RwPaysData>();
        private Dictionary<string, RwPaysData> newNaklsData = new Dictionary<string, RwPaysData>();
        private Dictionary<long, RwPaysData> oldOtgrsData = new Dictionary<long, RwPaysData>();

        private void ProcessChangesInOtgruzByRwList(bool _isAuto)
        {
            foreach (var rwnakl in rwListData)
            {
                var rwBillNumber = rwnakl.RwBillNumber;

                var oldNaklData = new RwPaysData();
                var difNaklData = new RwPaysData();
                var newNaklData = new RwPaysData();

                var sperToNakl = rwnakl.Sper;
                var sperNdsToNakl = rwnakl.Ndssper;
                var ndsStToNakl = rwnakl.Nds;
                var dopToNakl = rwnakl.Dopusl;
                var ndsDopToNakl = rwnakl.Ndsdopusl;
                var ndsStDopToNakl = rwnakl.Ndst_dop;

                var naklOtgr = otgrData.Where(o => o.RwBillNumber == rwBillNumber).OrderBy(o => o.Otgr.Idrnn).ToArray();
                var naklKolf = naklOtgr.Sum(o => o.Kolf);
                var otgrLength = naklOtgr.Length;

                decimal nOldSper = 0;
                decimal nOldNdsSper = 0;
                decimal nOldNdsStSper = 0;
                decimal nOldDopUsl = 0;
                decimal nOldNdsDop = 0;
                decimal nOldNdsStDop = 0;

                OtgrLineViewModel curOtgr = naklOtgr[0];
                
                //decimal nNewSper = sperToNakl;
                //decimal nNewNdsSper = sperNdsToNakl ;
                //decimal nNewNdsStSper = ndsStToNakl;
                decimal nNewDopUsl = curOtgr.Dopusl;
                decimal nNewNdsDop = curOtgr.Ndsdopusl;
                decimal nNewNdsStDop = curOtgr.Ndst_dop;             

                // записываем доп. услуги
                nOldDopUsl = curOtgr.Dopusl;
                nOldNdsDop = curOtgr.Ndsdopusl;
                nOldNdsStDop = curOtgr.Ndst_dop;

                if (dopToNakl != 0 && dopToNakl != nOldDopUsl)
                {
                    nNewDopUsl = curOtgr.Dopusl + dopToNakl;
                    curOtgr.Dopusl = nNewDopUsl;
                }
                if (ndsDopToNakl != 0 && ndsDopToNakl != nOldNdsDop)
                {
                    nNewNdsDop = curOtgr.Ndsdopusl + ndsDopToNakl;
                    curOtgr.Ndsdopusl = nNewNdsDop;
                    curOtgr.Ndst_dop = nNewNdsStDop = ndsStDopToNakl;
                }

                for (int i = 0; i < otgrLength; i++)
                {
                    curOtgr = naklOtgr[i];

                    nOldSper += curOtgr.Sper;
                    nOldNdsSper += curOtgr.Ndssper;

                    decimal addedSper = 0;
                    decimal addedNdsSper = 0;

                    if (_isAuto)
                    {
                        if (i == otgrLength - 1) // последняя запись в накладной
                        {
                            if (sperToNakl != 0)
                                curOtgr.Sper += (sperToNakl - addedSper);
                            if (sperNdsToNakl != 0)
                            {
                                curOtgr.Ndssper += (sperNdsToNakl - addedNdsSper);
                                curOtgr.Nds = ndsStToNakl;
                            }
                        }
                        else
                        {
                            if (sperToNakl != 0 || sperNdsToNakl != 0)
                            {
                                decimal frac = curOtgr.Kolf / naklKolf;
                                decimal sumToAdd = 0;
                                if (sperToNakl != 0)
                                {
                                    sumToAdd = Math.Round(sperToNakl * frac, 0);
                                    addedSper += sumToAdd;
                                    curOtgr.Sper += sumToAdd;
                                }

                                if (sperNdsToNakl != 0)
                                {
                                    sumToAdd = Math.Round(sperNdsToNakl * frac, 0);
                                    addedNdsSper += sumToAdd;
                                    curOtgr.Ndssper += sumToAdd;
                                    curOtgr.Nds = ndsStToNakl;
                                }
                            }
                        }
                    }
                    UpdateTrackingState(curOtgr);
                }

                oldNaklData.Sper = nOldSper;
                oldNaklData.NdsSper = nOldNdsSper;
                oldNaklData.NdsStSper = nOldNdsStSper;
                oldNaklData.DopUsl = nOldDopUsl;
                oldNaklData.NdsDop = nOldNdsDop;
                oldNaklData.NdsStDop = nOldNdsStDop;
                oldNaklsData[rwBillNumber] = oldNaklData;

                newNaklData.Sper = sperToNakl + nOldSper;
                newNaklData.NdsSper = sperNdsToNakl + nOldNdsSper;
                newNaklData.NdsStSper = ndsStToNakl;
                newNaklData.DopUsl = nNewDopUsl;
                newNaklData.NdsDop = nNewNdsDop;
                newNaklData.NdsStDop = nNewNdsStDop;
                newNaklsData[rwBillNumber] = newNaklData;

                difNaklData.Sper = sperToNakl;
                difNaklData.NdsSper = sperNdsToNakl;
                difNaklData.NdsStSper = ndsStToNakl;
                difNaklData.DopUsl = dopToNakl != nOldDopUsl ? dopToNakl : 0;
                difNaklData.NdsDop = ndsDopToNakl != nOldNdsDop ? ndsDopToNakl : 0;
                difNaklData.NdsStDop = ndsDopToNakl != nOldNdsDop ? ndsStDopToNakl : 0;
                difNaklsData[rwBillNumber] = difNaklData;

            }
        }

        protected override void ExecuteSubmit()
        {
            if (!DoFinalCheck())
            {
                //ShowError();
                Parent.Services.ShowMsg("Ошибка","Операция не может быть завершена.\nПопробуйте повторить все этапы.", true);
                return;
            }
            base.ExecuteSubmit();
        }

        //private void ShowError()
        //{
        //    var dlg = new MsgDlgViewModel
        //    {
        //        Title = "Ошибка",
        //        Message = "Операция не может быть завершена.\nПопробуйте повторить все этапы."
        //    };
        //    Parent.OpenDialog(dlg);
        //}

        private bool DoFinalCheck()
        {
            var otgr = otgrData.Where(o => o.TrackingState != TrackingInfo.Unchanged);
            foreach (var o in otgr)
            {
                var sfs = repository.GetSfsByOtgruz(o.Otgr.Idp623);
                if (sfs != null && sfs.Length > 0 && sfs.Any(s => s.Status != LifetimeStatuses.Deleted))
                    return false;
            }
            return true;
        }


    }
}
