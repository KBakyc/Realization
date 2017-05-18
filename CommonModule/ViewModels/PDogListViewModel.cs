using System;
using CommonModule.Commands;
using DataObjects;
using System.ComponentModel;
using System.Windows.Input;
using System.Linq;
using DAL;
using System.Collections.Generic;
using DataObjects.Interfaces;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога отображения списка продуктовых спецификаций договоров сбыта.
    /// </summary>
    public class PDogListViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public PDogListViewModel(IDbService _rep)
        {
            repository = _rep;            
        }

        public PDogListViewModel(IDbService _rep, IEnumerable<PDogInfoModel> _dogs)
            :this(_rep)
        {
            LoadData(_dogs);
        }

        /// <summary>
        /// Загрузка моделей договоров
        /// </summary>
        /// <param name="_dogs"></param>
        public void LoadData(IEnumerable<PDogInfoModel> _dogs)
        { 
            if (_dogs != null)
                PDogInfos = _dogs.Select(m => new PDogInfoViewModel(m, repository)).ToArray();
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && SelPDogInfo != null;
        }

        /// <summary>
        /// Список договоров для выбора
        /// </summary>
        private PDogInfoViewModel[] pDogInfos;
        public PDogInfoViewModel[] PDogInfos
        {
            get { return pDogInfos; }
            set
            {
                if (value != pDogInfos)
                {
                    pDogInfos = value;
                    NotifyPropertyChanged("PDogInfos");
                }
            }
        }

        /// <summary>
        /// Выбраный договор
        /// </summary>
        private PDogInfoViewModel selPDogInfo;
        public PDogInfoViewModel SelPDogInfo 
        {
            get { return selPDogInfo; }
            set
            {
                if (value != selPDogInfo)
                {
                    selPDogInfo = value;
                    NotifyPropertyChanged("SelPDogInfo");
                }
            }
        }
    }
}