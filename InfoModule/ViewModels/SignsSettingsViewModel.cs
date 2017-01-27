using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.ViewModels;
using DataObjects;
using DataObjects.Interfaces;

namespace InfoModule.ViewModels
{
    public class SignsSettingsViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public SignsSettingsViewModel(IDbService _rep)
        {
            repository = _rep;
            myPoupsSignModes = CommonModule.CommonSettings.MyPoupsSignModes.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        /// <summary>
        /// Загрузка данных для редактирования
        /// </summary>
        private void LoadData()
        {
            if (SelPoup != null)
            {
                signatures = repository.GetSigners(SelPoup.Kod);
                var csigns = repository.GetSigns(SelPoup.Kod);
                if (csigns != null)
                {
                    curBossId = csigns.BossId;
                    curGlBuhId = csigns.GlBuhId;
                }
                else
                    curBossId = curGlBuhId = 0;

                SelBoss = Bosses.FirstOrDefault(b => b.Id == curBossId) ?? emptySignanure;
                SelGlBuh = GlBuhs.FirstOrDefault(b => b.Id == curGlBuhId) ?? emptySignanure;
            }
        }

        /// <summary>
        /// Сохранение изменённых данных
        /// </summary>
        public void SavePoupsSignModes()
        {
            CommonModule.CommonSettings.MyPoupsSignModes = myPoupsSignModes;
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelPoup != null
                && (IsSignsChanged || IsNeedSignsModesChanged());
        }

        public bool IsSignsChanged
        {
            get { return SelBoss.Id != curBossId || SelGlBuh.Id != curGlBuhId; }
        }

        private int curGlBuhId;
        private int curBossId;

        /// <summary>
        /// Список видов реализации
        /// </summary>
        private PoupModel[] poupList;
        public PoupModel[] PoupList
        {
            get
            {
                if (poupList == null)
                    poupList = repository.Poups.Values.Where(p => p.IsActive).ToArray();
                return poupList;
            }
        }

        /// <summary>
        /// Выбраное направление
        /// </summary>
        private PoupModel selPoup;
        public PoupModel SelPoup 
        {
            get { return selPoup; }
            set 
            {
                if (value != selPoup)
                {
                    selPoup = value;
                    LoadData();
                    NotifyPropertyChanged("SelPoup");
                    NotifyPropertyChanged("GlBuhs");
                    NotifyPropertyChanged("SelGlBuh");
                    NotifyPropertyChanged("Bosses");
                    NotifyPropertyChanged("SelBoss");
                    NotifyPropertyChanged("NeedSignsForPoupMode");
                }
            }
        }

        private SignatureInfo[] signatures;

        /// <summary>
        /// Бухгалтера по выбранному направлению
        /// </summary>
        public IEnumerable<SignatureInfo> GlBuhs
        {
            get { return Enumerable.Repeat(emptySignanure, 1).Union(signatures == null ? Enumerable.Empty<SignatureInfo>() : signatures.Where(s => s.SignTypeId == 2)); }
        }

        /// <summary>
        /// Выбранный главный бухгалтер
        /// </summary>
        private SignatureInfo selGlBuh;
        public SignatureInfo SelGlBuh
        {
            get { return selGlBuh; }
            set
            {
                if (value != selGlBuh)
                {
                    selGlBuh = value;
                    NotifyPropertyChanged("SelGlBuh");
                }
            }
        }

        private SignatureInfo emptySignanure = new SignatureInfo()
        {
            Id = 0,
            Fio = "Без подписи",
            Position = "",
            Short = "",
            SignTypeId = 0
        };

        /// <summary>
        /// Руководители по выбранному направлению
        /// </summary>
        public IEnumerable<SignatureInfo> Bosses
        {
            get { return Enumerable.Repeat(emptySignanure, 1).Union(signatures == null ? Enumerable.Empty<SignatureInfo>() : signatures.Where(s => s.SignTypeId == 1)); }
        }

        /// <summary>
        /// Выбранный руководитель
        /// </summary>
        private SignatureInfo selBoss;
        public SignatureInfo SelBoss
        {
            get { return selBoss; }
            set
            {
                if (value != selBoss)
                {
                    selBoss = value;
                    NotifyPropertyChanged("SelBoss");
                }
            }
        }

        private Dictionary<int, ApplyFeature> myPoupsSignModes;

        public ApplyFeature NeedSignsForPoupMode
        {
            get { return GetNeedSignsForSelectedPoup(); }
            set { SetNeedSignsForSelectedPoup(value); }
        }

        private void SetNeedSignsForSelectedPoup(ApplyFeature _needSigns)
        {
            if (_needSigns == ApplyFeature.Yes)
            {
                if (myPoupsSignModes.ContainsKey(SelPoup.Kod))
                    myPoupsSignModes.Remove(SelPoup.Kod);
            }
            else
                if (SelPoup != null)
                    myPoupsSignModes[SelPoup.Kod] = _needSigns;
        }

        private ApplyFeature GetNeedSignsForSelectedPoup()
        {
            ApplyFeature needSigns = ApplyFeature.Yes;
            if (SelPoup != null && myPoupsSignModes.ContainsKey(SelPoup.Kod))
                needSigns = myPoupsSignModes[SelPoup.Kod];
            return needSigns;
        }

        public bool IsNeedSignsModesChanged()
        { 
            bool res = false;
            res = !myPoupsSignModes.SequenceEqual(CommonModule.CommonSettings.MyPoupsSignModes);
            return res;
        }

    }
}