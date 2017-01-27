using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Reporting.WinForms;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    public class ReportParameterViewModel : BasicViewModel
    {
        private ReportParameterInfo paramInfo;
        private string[] coercedDefs;


        public ReportParameterViewModel(ReportParameterInfo _pi, string[] _newdefs)
        {
            Init(_pi, _newdefs);
        }

        private void Init(ReportParameterInfo _pi, string[] _newdefs)
        {
            if (_pi == null) throw new ArgumentNullException("paramInfo", "Конструктор не может принять NULL значение!");

            paramInfo = _pi;
            Title = _pi.Prompt;
            //if (_newdefs != null && _newdefs.Length > 0)
            CoerceDefaults(_newdefs);
            GetAvailableValues();
            if (isAvailableExist)
            {
                SelectAvailableDefaults();
                if (IsMultiValued)
                    SubscribeToChangeSelection();
                else
                    SelectFirstDefValueIfNeeded();
            }
            else
                GetParameterTypeAndDefaultValue();
        }

        public void SetNewParamData(ReportParameterInfo _pi)
        {
            UnSubscribeToChangeSelection();
            UnSubscribeSelectAll();
            Init(_pi, null);
            SubscribeSelectAll();
        }

        public void NotifyChanges()
        {
            var selVal = selectedValue; 
            NotifyPropertyChanged(() => AvailableValues);
            NotifyPropertyChanged(() => AvailableValuesWithOptions);
            if (isAvailableExist)
            {
                //NotifyPropertyChanged("AvailableValues");
                selectedValue = selVal;
            }
            if (IsMultiValued && !IsEditing)
                NotifyPropertyChanged("SelectedLabel");
            else 
            {
                switch (paramInfo.DataType)
                {
                    case ParameterDataType.Boolean:
                        NotifyPropertyChanged(() => BoolValue);
                        break;
                    case ParameterDataType.DateTime:
                        NotifyPropertyChanged(() => DateValue);
                        break;
                    default:
                        NotifyPropertyChanged("SelectedValue");
                        break;
                }
            }
            NotifyPropertyChanged("IsEnabled");
        }

        private void SelectFirstDefValueIfNeeded()
        {
            if (!isAvailableExist) return;
            if (availableValues.Any(v => v.Key.IsSelected))
                selectedValue = availableValues.Where(v => v.Key.IsSelected).Select(v => v.Value).First();
            else
                selectedValue = availableValues.Select(v => v.Value).First();
        }

        public string ParamName { get { return paramInfo.Name; } }
        public ParameterState ParamState { get { return paramInfo.State; } }
        public string GroupTitle { get; set; }
        public bool IsSingleInGroup { get; set; }

        public Type ParamType { get; private set; }

        public bool IsMultiValued { get { return paramInfo.MultiValue; } }
        public bool IsNullable { get { return paramInfo.Nullable; } }
        public bool IsAllowBlank { get { return paramInfo.AllowBlank; } }
        public bool IsValidValuesQueryBased { get { return paramInfo.AreValidValuesQueryBased; } }

        private bool isVisible = true;
        public bool IsVisible
        {
            get { return isVisible; }// { return String.IsNullOrEmpty(paramInfo.Prompt); }
            set { SetAndNotifyProperty("IsVisible", ref isVisible, value); }
        }
        public bool IsParamsValueValid { get { return IsValid(); } }
        public bool IsEnabled { get { return paramInfo.State != ParameterState.HasOutstandingDependencies; } }

        public bool IsQueryBased { get { return paramInfo.AreValidValuesQueryBased; } }

        private bool isAvailableExist = false;
        public bool IsAvailableExists
        {
            get { return isAvailableExist; }
        }

        public ReportParameterInfo[] Dependents
        {
            get { return paramInfo.Dependents.ToArray(); }
        }

        public bool HasCoersedDependents { get { return coersedDependents != null && coersedDependents.Count > 0; } }

        private List<ReportParameterViewModel> coersedDependents;
        public IEnumerable<ReportParameterViewModel> CoersedDependents
        {
            get { return coersedDependents.AsEnumerable(); }
        }

        public void AddDependentParameter(ReportParameterViewModel _dep)
        {
            if (_dep == null || Dependents.Any(d => d.Name == _dep.ParamName)) return;
            if (coersedDependents == null) coersedDependents = new List<ReportParameterViewModel>();
            else if (coersedDependents.Count > 0 && coersedDependents.Any(p => p.ParamName == _dep.ParamName)) return;
            coersedDependents.Add(_dep);
        }

        private bool IsValid()
        {
            bool res = false;
            if (IsMultiValued)
            {
                var selvals = GetSelectedValues();
                res = selvals != null && selvals.Length > 0;
            }
            else
                res = (!String.IsNullOrEmpty(SelectedValue) || IsAllowBlank || IsNullable && IsNull) && paramInfo.State != ParameterState.HasOutstandingDependencies;

            return res;
        }

        private bool isnull;
        public bool IsNull
        {
            get { return isnull; ; }
            set
            {
                if (IsNullable)
                {
                    SetAndNotifyProperty("IsNull", ref isnull, value);
                }
            }
        }

        private void SubscribeToChangeSelection()
        {
            if (!IsMultiValued || !IsAvailableExists) return;
            foreach(var a in availableValues)
                a.Key.PropertyChanged += AvailableItemChanged;
        }

        private void UnSubscribeToChangeSelection()
        {
            if (!IsMultiValued || !IsAvailableExists) return;
            foreach (var a in AvailableValues)
                a.Key.PropertyChanged -= AvailableItemChanged;
        }

        void AvailableItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected" && !IsEditing)
                NotifyPropertyChanged("SelectedLabel");
        }

        private void GetParameterTypeAndDefaultValue()
        {
            var datatype = paramInfo.DataType;
            switch (datatype)
            {
                case ParameterDataType.Boolean:
                    ParamType = typeof(System.Boolean);
                    BoolValue = GetDefaultBool();
                    break;
                case ParameterDataType.DateTime:
                    ParamType = typeof(System.DateTime);
                    DateValue = GetDefaultDate();
                    break;
                case ParameterDataType.Float:
                    ParamType = typeof(System.Double);
                    SelectedValue = GetDefaultDouble().ToString();
                    break;
                case ParameterDataType.Integer:
                    ParamType = typeof(System.Int32);
                    SelectedValue = GetDefaultInt().ToString();
                    break;
                default:
                    ParamType = typeof(System.String); 
                    SelectedValue = GetDefaultString();
                    break;
            }
        }

        private void GetAvailableValues()
        {
            if (paramInfo.ValidValues != null && paramInfo.ValidValues.Count > 0)
            {
                availableValues = paramInfo.ValidValues.ToDictionary(kv => new Selectable<string>(kv.Label), kv => kv.Value);
                isAvailableExist = true;
            }
            else
            {
                availableValues = null;
                isAvailableExist = false;
            }
        }

        private string selectAllName;
        /// <summary>
        /// Устанавливает название и доступность опции выбора всех возможных значений
        /// </summary>
        public string SelectAllName
        {
            get { return selectAllName; }
            set
            {
                if (selectAllName == value) return;
                selectAllName = value;
                
                if (selectAllOption != null)
                {
                    selectAllOption.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(selectAllOption_PropertyChanged);
                    selectAllOption = null;
                    UnSubscribeSelectAll();
                }

                if (!String.IsNullOrWhiteSpace(selectAllName))
                {
                    selectAllOption = new Selectable<string>(selectAllName, availableValues.Keys.All(k => k.IsSelected));
                    selectAllOption.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(selectAllOption_PropertyChanged);
                    SubscribeSelectAll();
                }
            }
        }

        private void SubscribeSelectAll()
        {
            if (IsSelectAllAvailable && availableValues != null && availableValues.Count > 0)
            {                
                foreach (var av in availableValues)
                    av.Key.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(AllKey_PropertyChanged);
            }
        }

        private void UnSubscribeSelectAll()
        {
            if (IsSelectAllAvailable && availableValues != null && availableValues.Count > 0)
            {                
                foreach (var av in availableValues)
                    av.Key.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(AllKey_PropertyChanged);
            }
        }

        void AllKey_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsSelected") return;
            var label = sender as Selectable<String>;
            var newSelectAllOption = availableValues.Keys.All(k => k.IsSelected);
            if (newSelectAllOption != selectAllOption.IsSelected)
            {
                selectAllOption.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(selectAllOption_PropertyChanged);
                selectAllOption.IsSelected = newSelectAllOption;
                selectAllOption.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(selectAllOption_PropertyChanged);
            }
        }

        void selectAllOption_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsSelected") return;
            var label = sender as Selectable<String>;
            foreach(var av in availableValues)
            {
                av.Key.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(AllKey_PropertyChanged);
                av.Key.IsSelected = label.IsSelected;
                av.Key.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(AllKey_PropertyChanged);
            }
        }

        public bool IsSelectAllAvailable
        {
            get { return IsMultiValued && availableValues != null && availableValues.Count > 1 && !String.IsNullOrWhiteSpace(selectAllName) && (singleVals == null || singleVals.Length == 0); }
        }

        private Selectable<string> selectAllOption;

        private String[] singleVals;
        /// <summary>
        /// Устанавливает значиния параметра с множественным выбором, при выборе которых остальные значения сбрасываются
        /// </summary>
        /// <param name="_singleVals"></param>
        public void SetSingleValues(params String[] _singleVals)
        {
            if (_singleVals == null || _singleVals.Length == 0 || !IsMultiValued || availableValues == null || availableValues.Count < 2 || IsSelectAllAvailable) return;
            singleVals = _singleVals;
            foreach(var av in availableValues)
                av.Key.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SingleKey_PropertyChanged);
        }

        private void SingleKey_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsSelected") return;
            var label = sender as Selectable<String>;
            var av = availableValues.FirstOrDefault(kv => kv.Key.Value == label.Value);
            if (av.Key == null || !av.Key.IsSelected) return;
            bool isSingle = Array.IndexOf(singleVals, av.Value) != -1;
            var svToChange = isSingle ? availableValues.Keys.Where(a => a != av.Key && a.IsSelected) // выбираем обычные значения
                                      : availableValues.Where(a => a.Key.IsSelected && Array.IndexOf(singleVals, a.Value) != -1).Select(a => a.Key); // выбираем "одиночные" значения
            foreach (var sv in svToChange)
            {
                sv.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(SingleKey_PropertyChanged);
                sv.IsSelected = false;
                sv.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(SingleKey_PropertyChanged);
            }
        }

        //public String ParameterTypeName { get { return type.FullName; } }


        private string selectedValue;
        public String SelectedValue
        { 
            get 
            { 
                return selectedValue; 
            }
            set { SetAndNotifyProperty("SelectedValue", ref selectedValue, value); }
        }

        public String SelectedLabel
        {
            get 
            {
                string res = null;
                if (IsMultiValued && !IsEditing)
                    res = GetSelectedLabelsString();
                return res;
            }
        }

        private DateTime dateValue;
        public DateTime DateValue 
        {
            get { return dateValue; }
            set
            {
                dateValue = value;
                SetAndNotifyProperty("SelectedValue", ref selectedValue, value.ToString("yyyy-MM-dd"));
            }
        }        

        private Boolean boolValue;
        public Boolean BoolValue 
        {
            get { return boolValue; }
            set
            {
                boolValue = value;
                SetAndNotifyProperty("SelectedValue", ref selectedValue, value ? "true" : "false");
            }
        }

        private Dictionary<Selectable<string>,string> availableValues;
        public Dictionary<Selectable<string>,string> AvailableValues
        {
            get { return availableValues; }
            private set 
            {
                if (SetAndNotifyProperty("AvailableValues", ref availableValues, value))
                    NotifyPropertyChanged(() => AvailableValuesWithOptions);
            }
        }

        public Dictionary<Selectable<string>,string> AvailableValuesWithOptions
        {
            get 
            {
                return IsSelectAllAvailable 
                    ? Enumerable.Repeat(new KeyValuePair<Selectable<string>,string>(selectAllOption, null), 1)
                                .Union(availableValues)
                                .ToDictionary(kv => kv.Key, kv => kv.Value)
                    : availableValues;
            }
        }

        private void SelectAvailableDefaults()
        {
            if (!IsAvailableExists || coercedDefs == null) return;
            foreach (var d in coercedDefs)
            {
                var aVal = availableValues.Where(a => a.Value == d).Select(a => a.Key).FirstOrDefault();
                if (aVal != null)
                    aVal.IsSelected = true;
            }
        }

        private void CoerceDefaults(string[] _newDefs)
        {
            coercedDefs = _newDefs != null ? _newDefs 
                                           : (paramInfo.Values != null) ? paramInfo.Values.ToArray() 
                                                                        : null;
        }            



        /// <summary>
        /// Возвращает все метки выбранных значений одной строкой через запятую (метка1,метка2...)
        /// </summary>
        /// <returns></returns>
        private string GetSelectedLabelsString()
        {
            if (!IsMultiValued || availableValues == null) return null;
            //if (IsSelectAllAvailable && availableValues.All(av => av.Key.IsSelected)) return selectAllName;

            var selvals = availableValues.Where(av => av.Key.IsSelected);
            var scnt = selvals.Count();

            string res = scnt > 1 && scnt < 6
                              ? String.Join("", selvals.Select(kv => "[" + kv.Value + "]").ToArray()) 
                              : selvals.Select(av => av.Key.Value).FirstOrDefault();            
            if (scnt > 5)
                res += "... +" + (scnt-1).ToString();

            return res;

            //var first = selvals.Select(av => av.Key.Value).FirstOrDefault();
            //if (first == null) return "";

            //var others = selvals.Skip(1).Select(kv => kv.Value).ToArray();          
            //if (others.Length == 0) return first;

            //return first + " + " + String.Join("", selvals.Skip(1).Select(kv => "[" + kv.Value + "]").ToArray());            
        }

        /// <summary>
        /// Возвращает выбранныe значения
        /// </summary>
        /// <returns></returns>
        private string[] GetSelectedValues()
        {
            if (!IsMultiValued || AvailableValues == null) return null;
            string[] selected = AvailableValues.Where(av => av.Key.IsSelected).Select(av => av.Value).ToArray();
            return selected;
        }

        private string GetDefaultString()
        {
            string defValueStr = null;
            if (coercedDefs != null && coercedDefs.Length > 0)
            {
                if (IsAvailableExists)
                    defValueStr = AvailableValues.Values.SingleOrDefault(k => k == coercedDefs[0]);
                else
                    defValueStr = coercedDefs[0];
            }
            
            if (defValueStr == null)
                IsNull = true;
            return defValueStr;
        }

        private double GetDefaultDouble()
        {
            double res = 0.0;
            string defValueStr = GetDefaultString();
            if (!String.IsNullOrEmpty(defValueStr))
                Double.TryParse(defValueStr, out res);
            return res;
        }

        private double GetDefaultInt()
        {
            int res = 0;
            string defValueStr = GetDefaultString();
            if (!String.IsNullOrEmpty(defValueStr))
                Int32.TryParse(defValueStr, out res);
            return res;
        }

        private DateTime GetDefaultDate()
        {
            DateTime res = DateTime.Now; ;
            string defValueStr = GetDefaultString();
            if (!String.IsNullOrEmpty(defValueStr))
                DateTime.TryParse(defValueStr, out res);
            return res;
        }

        private Boolean GetDefaultBool()
        {
            bool res = false;
            string defValueStr = GetDefaultString();
            if (!String.IsNullOrEmpty(defValueStr))
                Boolean.TryParse(defValueStr, out res);
            return res;
        }

        public string[] GetValues()
        {
            string[] res = null;
            if (!IsNull) 
            {
                if (IsMultiValued)
                    res = GetSelectedValues();
                else
                    res = new string[] { SelectedValue };
            }
            return res ?? new string[]{};
        }

        public Object GetParameterValue()
        {
            Object res = null;
            var datatype = paramInfo.DataType;
            switch (datatype)
            {
                case ParameterDataType.Boolean:
                    res = BoolValue;
                    break;
                case ParameterDataType.DateTime:
                    res = DateValue;
                    break;
                case ParameterDataType.Float:
                    double dres = 0.0;
                    Double.TryParse(SelectedValue, out dres);
                    res = dres;
                    break;
                case ParameterDataType.Integer:
                    int ires = 0;
                    int.TryParse(SelectedValue, out ires);
                    res = ires;
                    break;
                default:
                    res = SelectedValue;
                    break;
            }

            return res;
        }

        private bool isEditing;
        public bool IsEditing
        {
            get { return isEditing; }
            set 
            { 
                if (SetAndNotifyProperty("IsEditing", ref isEditing, value) && !value)
                    NotifyPropertyChanged(() => SelectedLabel); 
            }
        }

        public bool IsChanged 
        {
            get 
            {
                return !GetValues().SequenceEqual(paramInfo.Values);
            }
        }
    }
}
