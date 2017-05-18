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
    /// Модель диалога отображения списка договоров сбыта.
    /// </summary>
    public class DogListViewModel : BaseDlgViewModel
    {
        private IDbService repository;

        public DogListViewModel(IDbService _rep)
        {
            repository = _rep;
        }

        public DogListViewModel(IDbService _rep, IEnumerable<DogInfo> _dogs)
            : this(_rep)
        {
            LoadData(_dogs);
        }

        /// <summary>
        /// Загрузка моделей договоров
        /// </summary>
        /// <param name="_dogs"></param>
        public void LoadData(IEnumerable<DogInfo> _dogs)
        {
            if (_dogs != null)
                DogInfos = _dogs.Select(m => new DogInfoViewModel(m, repository))
                                .ToArray();
        }

        public override bool IsValid()
        {
            return base.IsValid()
                && SelDogInfo != null;
        }

        /// <summary>
        /// Список договоров для выбора
        /// </summary>
        private DogInfoViewModel[] dogInfos;
        public DogInfoViewModel[] DogInfos
        {
            get { return dogInfos; }
            set
            {
                if (value != dogInfos)
                {
                    dogInfos = value;
                    NotifyPropertyChanged("DogInfos");
                }
            }
        }

        /// <summary>
        /// Выбраный договор
        /// </summary>
        private DogInfoViewModel selDogInfo;
        public DogInfoViewModel SelDogInfo
        {
            get { return selDogInfo; }
            set
            {
                if (value != selDogInfo)
                {
                    selDogInfo = value;
                    NotifyPropertyChanged("SelDogInfo");
                }
            }
        }
    }
}