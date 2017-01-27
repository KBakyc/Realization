using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Reporting.WinForms;
using System.Xml.Linq;
using CommonModule.Helpers;
using System.ComponentModel;

namespace ServiceModule.ViewModels
{
    public enum SelectIfCases 
    { 
        [Description("Нет")]
        None, 
        [Description("Единственный в списке")]
        Single 
    };
    
    public enum HideIfCases 
    {
        [Description("Нет")]
        None, 

        [Description("Единственное значение")]
        SelfSingleVal, 

        [Description("Значение другого параметра")]
        OtherParamVal
    }

    public class ParameterSettingsViewModel : BasicNotifier
    {
        private ReportParameterInfo parameter;

        public ParameterSettingsViewModel(ReportParameterInfo _parameter)
        {
            if (_parameter == null) throw new ArgumentNullException();
            parameter = _parameter;
        }

        public bool IsValid 
        {
            get { return EditorSettings != null || DefaultSettings != null; }
        }

        public bool IsChanged()
        {
            return (oldEditor == null && newEditor != null) || (oldEditor != null && newEditor == null) || (oldEditor != null && newEditor != null && oldEditor.ToString() != newEditor.ToString())
                || (oldDefault == null && newDefault != null) || (oldDefault != null && newDefault == null) || (oldDefault != null && newDefault != null && oldDefault.ToString() != newDefault.ToString());
        }

        private const string EDITOR_GROUP = "group";
        private const string EDITOR_GROUPSINGLE = "single";
        private const string EDITOR_SELECTALL = "selectallname";
        private const string EDITOR_SELECTIF = "select";
        private const string EDITOR_SELECTIFSINGLE = "single";
        private const string EDITOR_HIDEIF = "hideif";
        private const string EDITOR_HIDEIFSELFVAL = "single";
        private const string EDITOR_SINGLEVALS = "singlevals";

        private XElement oldEditor;
        private XElement oldDefault;

        public void Parse(XElement _editor, XElement _default)
        {
            oldEditor = newEditor = _editor;
            oldDefault = newDefault = _default;

            if (_default != null && _default.Name == parameter.Name && _default.HasAttributes)
            {
                var defValAtt = _default.Attribute("Value");
                if (defValAtt != null)
                    DefaultVarName = defValAtt.Value;
            }

            if (_editor != null && _editor.Name == parameter.Name && _editor.HasAttributes)
            {
                var groupAtt = _editor.Attribute(EDITOR_GROUP);
                if (groupAtt != null)
                {
                    GroupName = groupAtt.Value;
                    if (IsSingleInGroupEnabled)
                    {
                        var singleAtt = _editor.Attribute(EDITOR_GROUPSINGLE);
                        bool issingle = false;
                        if (singleAtt != null && !String.IsNullOrWhiteSpace(singleAtt.Value) && Boolean.TryParse(singleAtt.Value, out issingle))
                            IsSingleInGroup = issingle;
                    }
                }
                var selAllAtt = _editor.Attribute(EDITOR_SELECTALL);
                if (IsSelectAllOptionEnabled && selAllAtt != null)
                    SelectAllOptionName = selAllAtt.Value;
                var selIfAtt = _editor.Attribute(EDITOR_SELECTIF);
                if (IsSelectIfEnabled && selIfAtt != null)
                    switch (selIfAtt.Value)
                    {
                        case EDITOR_SELECTIFSINGLE:
                            SelectIf = SelectIfCases.Single;
                            break;
                        default:
                            SelectIf = SelectIfCases.None;
                            break;
                    }
                var hideifAtt = _editor.Attribute(EDITOR_HIDEIF);
                if (hideifAtt != null && !String.IsNullOrWhiteSpace(hideifAtt.Value))
                {
                    var hifopt = hideifAtt.Value.Split(':').ToArray();
                    if (hifopt.Length != 2) return;
                    if (hifopt[0] == EDITOR_HIDEIFSELFVAL)
                    {
                        HideIf = HideIfCases.SelfSingleVal;
                        HideIfSelValue = hifopt[1];
                    }
                    else
                    {
                        if (hifopt[0][0] == '!')
                        {
                            HideIf = HideIfCases.OtherParamVal;
                            HideIfParameterName = hifopt[0].TrimStart('!');
                            HideIfParameterValue = hifopt[1];
                        }
                    }
                }
                var svalsAtt = _editor.Attribute(EDITOR_SINGLEVALS);
                if (IsSingleValsEnabled && svalsAtt != null)
                    SingleVals = svalsAtt.Value;
            }
        }

        private XElement newEditor;

        public XElement EditorSettings
        {
            get
            {
                var res = new XElement(parameter.Name);
                if (!String.IsNullOrWhiteSpace(GroupName))
                    res.Add(new XAttribute(EDITOR_GROUP, GroupName.Trim()));
                if (IsSingleInGroupEnabled && IsSingleInGroup)
                    res.Add(new XAttribute(EDITOR_GROUPSINGLE, IsSingleInGroup));
                if (IsSelectAllOptionEnabled && !String.IsNullOrWhiteSpace(SelectAllOptionName))
                    res.Add(new XAttribute(EDITOR_SELECTALL, SelectAllOptionName.Trim()));
                if (IsSelectIfEnabled && SelectIf != SelectIfCases.None)
                    switch (SelectIf)
                    {
                        case SelectIfCases.Single:
                            res.Add(new XAttribute(EDITOR_SELECTIF, EDITOR_SELECTIFSINGLE));
                            break;
                    }
                if (HideIf != HideIfCases.None)
                {
                    switch (HideIf)
                    {
                        case HideIfCases.SelfSingleVal:
                            if (!String.IsNullOrWhiteSpace(
                                HideIfSelValue))
                                res.Add(new XAttribute(EDITOR_HIDEIF, EDITOR_HIDEIFSELFVAL + ":" + HideIfSelValue.Trim()));
                            break;
                        case HideIfCases.OtherParamVal:
                            if (!String.IsNullOrWhiteSpace(HideIfParameterName) && !String.IsNullOrWhiteSpace(HideIfParameterValue))
                                res.Add(new XAttribute(EDITOR_HIDEIF, "!" + HideIfParameterName.Trim() + ":" + HideIfParameterValue.Trim()));
                            break;
                    }
                }
                if (IsSingleValsEnabled && !String.IsNullOrWhiteSpace(SingleVals))
                    res.Add(new XAttribute(EDITOR_SINGLEVALS, SingleVals.Trim()));

                newEditor = res.HasAttributes ? res : null;
                return newEditor;
            }
        }

        private XElement newDefault;

        public XElement DefaultSettings
        {
            get
            {
                
                var res = new XElement(parameter.Name);
                if (!String.IsNullOrWhiteSpace(defaultVarName))
                    res.Add(new XAttribute("Value", defaultVarName.Trim()));
                newDefault = res.HasAttributes ? res : null;
                return newDefault;
            }
        }

        private string defaultVarName;
        public string DefaultVarName
        {
            get { return defaultVarName; }
            set 
            { 
                if (SetAndNotifyProperty(() => DefaultVarName, ref defaultVarName, value))
                    NotifyPropertyChanged(() => DefaultSettings); 
            }
        }

        private string groupName;
        public string GroupName
        {
            get { return groupName; }
            set 
            {
                if (SetAndNotifyProperty(() => GroupName, ref groupName, value))
                {
                    NotifyPropertyChanged(() => IsSingleInGroupEnabled);
                    NotifyPropertyChanged(() => EditorSettings);
                }
            }
        }

        public bool IsSingleInGroupEnabled
        {
            get { return !String.IsNullOrWhiteSpace(GroupName) && parameter.DataType == ParameterDataType.Boolean; }
        }

        private bool isSingleInGroup;
        public bool IsSingleInGroup
        {
            get { return isSingleInGroup; }
            set 
            { 
                if (SetAndNotifyProperty(() => IsSingleInGroup, ref isSingleInGroup, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }
        
        public bool IsSelectAllOptionEnabled
        {
            get { return parameter.MultiValue; }
        }

        private string selectAllOptionName;
        public string SelectAllOptionName
        {
            get { return selectAllOptionName; }
            set 
            { 
                if (SetAndNotifyProperty(() => SelectAllOptionName, ref selectAllOptionName, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }
        

        public bool IsSelectIfEnabled
        {
            get { return parameter.AreValidValuesQueryBased; }
        }

        public Dictionary<SelectIfCases, string> SelectIfDescriptions
        {
            get { return Enumerations.GetAllValuesAndDescriptions<SelectIfCases>(); }
        }

        private SelectIfCases selectIf;
        public SelectIfCases SelectIf
        {
            get { return selectIf; }
            set 
            { 
                if (SetAndNotifyProperty(() => SelectIf, ref selectIf, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

        public Dictionary<HideIfCases, string> HideIfDescriptions
        {
            get { return Enumerations.GetAllValuesAndDescriptions<HideIfCases>(); }
        }

        private HideIfCases hideIf;
        public HideIfCases HideIf
        {
            get { return hideIf; }
            set 
            { 
                if (SetAndNotifyProperty(() => HideIf, ref hideIf, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

        private string hideIfSelValue;
        public string HideIfSelValue
        {
            get { return hideIfSelValue; }
            set 
            { 
                if (SetAndNotifyProperty(() => HideIfSelValue, ref hideIfSelValue, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

        private string hideIfParameterName;
        public string HideIfParameterName
        {
            get { return hideIfParameterName; }
            set 
            { 
                if (SetAndNotifyProperty(() => HideIfParameterName, ref hideIfParameterName, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

        private string hideIfParameterValue;
        public string HideIfParameterValue
        {
            get { return hideIfParameterValue; }
            set 
            { 
                if (SetAndNotifyProperty(() => HideIfParameterValue, ref hideIfParameterValue, value))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

        public bool IsSingleValsEnabled
        {
            get { return parameter.MultiValue; }
        }

        private string singleVals;
        public string SingleVals 
        { 
            get { return singleVals; }
            set 
            {
                var cval = value == null ? null : String.Join(",", value.Split(',').Where(s => !String.IsNullOrWhiteSpace(s)).ToArray());
                if (SetAndNotifyProperty(() => SingleVals, ref singleVals, cval))
                    NotifyPropertyChanged(() => EditorSettings);
            }
        }

    }
}
