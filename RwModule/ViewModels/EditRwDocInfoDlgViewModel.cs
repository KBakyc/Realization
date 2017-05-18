using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.ViewModels;
using System.Collections.ObjectModel;
using DataObjects;
using DataObjects.Interfaces;
using DotNetHelper;

namespace RwModule.ViewModels
{
    /// <summary>
    /// Модель диалога изменения данных по документу перечня.
    /// </summary>
    public class EditRwDocInfoDlgViewModel : BaseDlgViewModel
    {
        private IDbService repository;
        private RwDocViewModel[] rwdViewModels;

        public EditRwDocInfoDlgViewModel(RwDocViewModel[] _rvms)
        {
            rwdViewModels = _rvms;
            repository = CommonModule.CommonSettings.Repository;
            LoadData();
        }

        public bool IsMultipleEdit { get { return rwdViewModels != null && rwdViewModels.Length > 1; } }

        public bool IsChanged
        {
            get 
            {
                return
                    (isRepDateEdEnabled && rwdViewModels[0].Rep_date != RepDate && RepDate != null
                    || isExcludeEdEnabled && rwdViewModels[0].Exclude != IsExclude
                    || isExclInfoEdEnabled && rwdViewModels[0].Excl_info != Excl_info
                    || isCommentsEdEnabled && rwdViewModels[0].Comments != Comments
                    || isDatDocEdEnabled && rwdViewModels[0].Dat_doc != DatDoc
                    || isDatZKrtEdEnabled && rwdViewModels[0].Dzkrt != DatZKrt
                    || isNum_docEdEnabled && rwdViewModels[0].Num_doc != Num_doc
                    || isNkrtEdEnabled && rwdViewModels[0].Nkrt != Nkrt
                    || isSumDocEdEnabled && rwdViewModels[0].Sum_doc != Sum_doc
                    || isSumNdsEdEnabled && rwdViewModels[0].Sum_nds != Sum_nds
                    || !IsMultipleEdit && isSumExclEdEnabled && rwdViewModels[0].Sum_excl != Sum_excl);
            }
        }

        private void LoadData()
        {
            if (rwdViewModels == null || rwdViewModels.Length == 0) return;

            if (IsMultipleEdit)
            {
                var isSameExcl = Array.TrueForAll(rwdViewModels, sd => sd.Exclude == rwdViewModels[0].Exclude && sd.Excl_info == rwdViewModels[0].Excl_info);
                Title = "Изменение данных документов ({0})".Format(rwdViewModels.Length);
                IsDatDocEdEnabled = IsSumExclEdEnabled = IsNkrtEdEnabled = IsNum_docEdEnabled = false;
                if (IsExcludeEdEnabled = isSameExcl)
                {
                    if (IsExclude = rwdViewModels[0].Exclude)
                        Excl_info = rwdViewModels[0].Excl_info;
                }
                var isSameRDate = Array.TrueForAll(rwdViewModels, sd => sd.Rep_date == rwdViewModels[0].Rep_date);
                if (IsRepDateEdEnabled = isSameRDate)
                    repDate = rwdViewModels[0].Rep_date;
                var isSameDZKrt = Array.TrueForAll(rwdViewModels, sd => sd.Dzkrt == rwdViewModels[0].Dzkrt);
                if (IsDatZKrtEdEnabled = isSameDZKrt)
                    DatZKrt = rwdViewModels[0].Dzkrt;
                var isSameComments = Array.TrueForAll(rwdViewModels, sd => sd.Comments == rwdViewModels[0].Comments);
                if (IsCommentsEdEnabled = isSameComments)
                    comments = rwdViewModels[0].Comments;

                IsSumDocEdEnabled = IsSumNdsEdEnabled = false;
            }
            else 
            {
                datDoc = rwdViewModels[0].Dat_doc;
                datZKrt = rwdViewModels[0].Dzkrt;
                excl_info = rwdViewModels[0].Excl_info;
                sum_excl = rwdViewModels[0].Sum_excl;
                sum_doc = rwdViewModels[0].Sum_doc;
                sum_nds = rwdViewModels[0].Sum_nds;
                IsExclude = rwdViewModels[0].Exclude;
                num_doc = rwdViewModels[0].Num_doc;
                nkrt = rwdViewModels[0].Nkrt;
                repDate = rwdViewModels[0].Rep_date;
                comments = rwdViewModels[0].Comments;
            }
            
            
        }

        private bool isRepDateEdEnabled = true;
        public bool IsRepDateEdEnabled
        {
            get { return isRepDateEdEnabled; }
            set { SetAndNotifyProperty("IsRepDateEdEnabled", ref isRepDateEdEnabled, value); }
        }

        private DateTime? repDate;
        public DateTime? RepDate
        {
            get { return repDate; }
            set { repDate = value; }
        }

        private bool isDatDocEdEnabled = true;
        public bool IsDatDocEdEnabled
        {
            get { return isDatDocEdEnabled; }
            set { SetAndNotifyProperty("IsDatDocEdEnabled", ref isDatDocEdEnabled, value); }
        }

        private DateTime? datDoc;
        public DateTime? DatDoc
        {
            get { return datDoc; }
            set { datDoc = value; }
        }

        private bool isDatZKrtEdEnabled = true;
        public bool IsDatZKrtEdEnabled
        {
            get { return isDatZKrtEdEnabled; }
            set { SetAndNotifyProperty("IsDatZKrtEdEnabled", ref isDatZKrtEdEnabled, value); }
        }

        private DateTime? datZKrt;
        public DateTime? DatZKrt
        {
            get { return datZKrt; }
            set { datZKrt = value; }
        }

        private bool isNkrtEdEnabled = true;
        public bool IsNkrtEdEnabled
        {
            get { return isNkrtEdEnabled; }
            set { SetAndNotifyProperty("IsNkrtEdEnabled", ref isNkrtEdEnabled, value); }
        }

        private string nkrt;
        public string Nkrt
        {
            get { return nkrt; }
            set { nkrt = value; }
        }

        private bool isNum_docEdEnabled = true;
        public bool IsNum_docEdEnabled
        {
            get { return isNum_docEdEnabled; }
            set { SetAndNotifyProperty("IsNum_docEdEnabled", ref isNum_docEdEnabled, value); }
        }

        private string num_doc;
        public string Num_doc
        {
            get { return num_doc; }
            set 
            { 
                SetAndNotifyProperty("Num_doc", ref num_doc, value.Trim());
            }
        }

        private bool isCommentsEdEnabled = true;
        public bool IsCommentsEdEnabled
        {
            get { return isCommentsEdEnabled; }
            set { SetAndNotifyProperty("IsCommentsEdEnabled", ref isCommentsEdEnabled, value); }
        }
        private string comments;
        public string Comments
        {
            get { return comments; }
            set { comments = value; }
        }

        private bool isExcludeEdEnabled = true;
        public bool IsExcludeEdEnabled
        {
            get { return isExcludeEdEnabled; }
            set { SetAndNotifyProperty("IsExcludeEdEnabled", ref isExcludeEdEnabled, value); }
        }

        private bool isExclude;
        public bool IsExclude
        {
            get { return isExclude; }
            set 
            { 
                SetAndNotifyProperty("IsExclude", ref isExclude, value);
                if (value)
                {
                    IsExclInfoEdEnabled = true;
                    Sum_excl = IsMultipleEdit ? rwdViewModels.Sum(sd => sd.Sum_itog) : rwdViewModels[0].Sum_itog;
                    IsSumExclEdEnabled = rwdViewModels.Length == 1;
                }
                else
                {
                    IsSumExclEdEnabled = IsExclInfoEdEnabled = false;
                    Sum_excl = 0;
                    Excl_info = null;
                }

            }
        }      

        private bool isSumDocEdEnabled = true;
        public bool IsSumDocEdEnabled
        {
            get { return isSumDocEdEnabled; }
            set { SetAndNotifyProperty("IsSumDocEdEnabled", ref isSumDocEdEnabled, value); }
        }

        private decimal sum_doc;
        public decimal Sum_doc
        {
            get { return sum_doc; }
            set { SetAndNotifyProperty("Sum_doc", ref sum_doc, value); }
        }

        private bool isSumNdsEdEnabled = true;
        public bool IsSumNdsEdEnabled
        {
            get { return isSumNdsEdEnabled; }
            set { SetAndNotifyProperty("IsSumNdsEdEnabled", ref isSumNdsEdEnabled, value); }
        }

        private decimal sum_nds;
        public decimal Sum_nds
        {
            get { return sum_nds; }
            set { SetAndNotifyProperty("Sum_nds", ref sum_nds, value); }
        }

        private bool isSumExclEdEnabled = true;
        public bool IsSumExclEdEnabled
        {
            get { return isSumExclEdEnabled; }
            set { SetAndNotifyProperty("IsSumExclEdEnabled", ref isSumExclEdEnabled, value); }
        }

        private decimal sum_excl;
        public decimal Sum_excl
        {
            get { return sum_excl; }
            set { SetAndNotifyProperty("Sum_excl", ref sum_excl, value); }
        }
        
        private bool isExclInfoEdEnabled = true;
        public bool IsExclInfoEdEnabled
        {
            get { return isExclInfoEdEnabled; }
            set { SetAndNotifyProperty("IsExclInfoEdEnabled", ref isExclInfoEdEnabled, value); }
        }

        private string excl_info;
        public string Excl_info
        {
            get { return excl_info; }
            set { SetAndNotifyProperty("Excl_info", ref excl_info, value); }
        }

        public override bool IsValid()
        {
            return base.IsValid() && Validate();
        }

        private bool Validate()
        {
            errors.Clear();
            
            bool res = !IsInputHasErrors;
            if (!res)
                errors.Add("Некорректный ввод");

            if (isExclude && sum_excl == 0)
            {
                res = false;
                errors.Add("Исключаемая сумма не должна быть равна 0");
            }

            bool tres = true;
            if (IsMultipleEdit)
                tres = Array.TrueForAll(rwdViewModels, d => d.Sum_itog > 0 && d.Sum_excl <= d.Sum_itog || d.Sum_itog < 0 && d.Sum_excl >= d.Sum_itog);
            else
                tres = rwdViewModels[0].Sum_itog > 0 && sum_excl <= rwdViewModels[0].Sum_itog || rwdViewModels[0].Sum_itog < 0 && sum_excl >= rwdViewModels[0].Sum_itog;
            if (!tres)
            {
                res = false;
                errors.Add("Исключаемая сумма не должна быть больше суммы документа");
            }

            NotifyPropertyChanged("IsHasErrors");
            return res;
        }

        public bool IsSumExclInputHasErrors { get; set; }
        public bool IsSumDocInputHasErrors { get; set; }
        public bool IsSumNdsInputHasErrors { get; set; }
        public bool IsInputHasErrors { get { return IsSumExclInputHasErrors || IsSumDocInputHasErrors || IsSumNdsInputHasErrors; } }

        public bool IsHasErrors { get { return errors.Count > 0 || IsSumExclInputHasErrors; } }

        private ObservableCollection<string> errors = new ObservableCollection<string>();
        public ObservableCollection<string> Errors
        {
            get { return errors; }
            set { errors = value; }
        }
    }
}
