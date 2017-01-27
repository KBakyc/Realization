using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using RwModule.Models;
using CommonModule.Helpers;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using DAL;
//using System.Data.Entity;

namespace RwModule.ViewModels
{
    public class RwListViewModel : BasicNotifier
    {
        private IDbService repository;

        public RwListViewModel(IDbService _repository, RwList _model)
        {
            modelRef = _model;
            repository = _repository;
            if (modelRef != null)
                sum_opl = modelRef.Sum_opl;
        }

        private RwList modelRef;
        public RwList ModelRef
        {
            get { return modelRef; }
        }

        public int Id_rwlist 
        { 
            get { return modelRef.Id_rwlist; } 
        }

        public int Num_rwlist 
        { 
            get { return modelRef.Num_rwlist; } 
        }

        public DateTime? Bgn_date
        {
            get { return modelRef.Bgn_date; }
        }

        public DateTime? End_date 
        { 
            get { return modelRef.End_date; }
        }

        public int Num_inv 
        {
            get { return modelRef.Num_inv; }
        }

        public DateTime Dat_inv 
        {
            get { return modelRef.Dat_inv; }
        }

        public decimal Sum_inv 
        {
            get { return modelRef.Sum_inv; }
        }
        
        public decimal Sum_invnds 
        {
            get { return modelRef.Sum_invnds; }
        }

        public decimal Sum_itog 
        {
            get { return modelRef.Sum_inv + modelRef.Sum_invnds; }
        }

        public long Keykrt 
        {
            get { return modelRef.Keykrt; }
        }
        
        public RwUslType RwlType 
        {
            get { return modelRef.RwlType; }
        }

        public bool Transition
        {
            get { return modelRef.Transition; }
            set             
            {
                modelRef.Transition = value;
                NotifyPropertyChanged("Transition"); 
            }
        }

        public DateTime? Dat_accept
        {
            get { return modelRef.Dat_accept; }
            set
            {
                modelRef.Dat_accept = value;
                NotifyPropertyChanged("Dat_accept");
            }
        }

        public DateTime? Dat_oplto
        {
            get { return modelRef.Dat_oplto; }
            set
            {
                modelRef.Dat_oplto = value;
                NotifyPropertyChanged("Dat_oplto");
            }
        }

        public DateTime Dat_orc
        {
            get { return modelRef.Dat_orc; }
            set
            {
                modelRef.Dat_orc = value;
                NotifyPropertyChanged("Dat_orc");
            }
        }

        public PayStatuses PayStatus
        {
            get { return modelRef.Paystatus; }
        }

        public DateTime? PayDate
        {
            get { return modelRef.Paydate; }
        }

        private UserInfo userAccept;
        public UserInfo UserAccept
        {
            get 
            {
                if (userAccept == null && modelRef.User_accept > 0)
                    userAccept = GetUser(modelRef.User_accept);
                return userAccept; 
            }
            set
            {
                userAccept = value;
                modelRef.User_accept = value == null ? 0 : value.Id;
            }
        }

        private UserInfo GetUser(int _userid)
        {
            return repository != null ? repository.GetUserInfo(_userid) : null;
        }

        private DogInfo dogovor;
        public DogInfo Dogovor
        {
            get 
            {
                if (dogovor == null && modelRef.Iddog > 0)
                    dogovor = GetDogovor(modelRef.Iddog);
                return dogovor; 
            }
        }

        private DogInfo  GetDogovor(int _iddog)
        {
            return repository != null ? repository.GetDogInfo(_iddog, false) : null;
        }

        private List<RwDocViewModel> rwDocsCollection;
        public List<RwDocViewModel> RwDocsCollection
        { 
            get
            {
                if (rwDocsCollection == null)
                    LoadRwDocs(false);
                return rwDocsCollection;
            }
        }

        public void LoadRwDocs(bool _force)
        {
            if (_force || modelRef.RwDocs.Count == 0)
            {
                if (modelRef.RwDocs.Count > 0) modelRef.RwDocs.Clear();
                using (var db = new RealContext())
                    modelRef.RwDocs.AddRange(db.RwDocs.Include(d => d.Esfn).Where(rd => rd.Id_rwlist == modelRef.Id_rwlist));
            }
            rwDocsCollection = modelRef.RwDocs.Select(rd => new RwDocViewModel(rd)).ToList();
            if (modelRef.RwDocs.Count > 0)
                foreach (var doc in modelRef.RwDocs) 
                {
                    doc.RwList = modelRef;
                }
        }

        public bool IsNew { get; set; }

        public decimal Sum_excl
        {
            get { return modelRef.Sum_excl; }
            set 
            { 
                modelRef.Sum_excl = value;
                NotifyPropertyChanged("Sum_excl");
            }
        }

        private decimal sum_opl;
        public decimal Sum_opl
        {
            get { return sum_opl; }
            set
            {
                sum_opl = value;
                NotifyPropertyChanged("Sum_opl");
                NotifyPropertyChanged("Ostatok");
            }
        }

        //public decimal CalcSumOpl()
        //{
        //    if (modelRef.Paystatus == PayStatuses.Unpayed) sum_opl = 0M;
        //    else
        //        if (modelRef.Paystatus == PayStatuses.TotallyPayed) sum_opl = Sum_itog - Sum_excl;
        //        else
        //        {
        //            sum_opl = rwDocsCollection.Select(d => d.CalcSumOpl()).Sum(s => s);
        //        }
        //    return sum_opl;
        //}

        public decimal Ostatok
        {
            get { return Sum_itog - Sum_excl - Sum_opl; }
        }
    }
}
