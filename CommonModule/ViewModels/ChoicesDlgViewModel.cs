using System.Collections.Generic;
using System.Linq;
using CommonModule.Commands;
using CommonModule.Interfaces;
using DataObjects;

namespace CommonModule.ViewModels
{
    public class ChoicesDlgViewModel : BaseDlgViewModel
    {
        public ChoicesDlgViewModel(params Choice[] _ch)
        {
            Groups = _ch.GroupBy(c => c.GroupName ?? string.Empty)
                .ToDictionary(g => g.Key, 
                              g => g.Select(c=>new ChoiceViewModel(c)).ToArray());
        }

        private bool isVertical;
        public bool IsVertical
        {
            get { return isVertical; }
            set { isVertical = value; }
        }

        private System.Action<ChoicesDlgViewModel, ChoiceViewModel> onChangeSelection;
        public System.Action<ChoicesDlgViewModel, ChoiceViewModel> OnChangeSelection
        {
            get { return onChangeSelection; }
            set
            {
                onChangeSelection = value;
                SetOnChange();                       
            }
        }

        private void SetOnChange()
        {
            var allch = Groups.SelectMany(g => g.Value).ToArray();
            for (int i = 0; i < allch.Length; i++)
            {
                var ch = allch[i];
                if (onChangeSelection != null)
                    ch.PropertyChanged += ch_PropertyChanged;
                else
                    ch.PropertyChanged -= ch_PropertyChanged;
            }
        }
        
        void ch_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" && onChangeSelection != null)
                onChangeSelection(this, sender as ChoiceViewModel);
        }

        public override void Dispose()
        {
            OnChangeSelection = null;
            base.Dispose();            
        }

        public override bool IsValid()
        {
            return base.IsValid() 
                && ChoicesCorrect();
        }

        protected bool ChoicesCorrect()
        {
            if (Groups == null) return false;
            bool res = true;
            foreach (var g in Groups)
            {
                var singles = g.Value.Where(c => c.IsSingleInGroup);
                if (singles.Any() && singles.Count(c => (c.IsChecked ?? false)) != 1)
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        public Dictionary<string,ChoiceViewModel[]> Groups { get; set; }

        public ChoiceViewModel GetChoiceByName(string _name)
        {
            ChoiceViewModel res = null;
            if (Groups != null && Groups.Any())
                res = Groups.SelectMany(g => g.Value).Where(v => v.Name == _name).FirstOrDefault();
            return res;
        }

        public T GetItemByName<T>(string _name)
        {
            ChoiceViewModel res = null;
            if (Groups != null && Groups.Any())
                res = Groups.SelectMany(g => g.Value).Where(v => v.Name == _name).FirstOrDefault();
            return res != null ? res.GetItem<T>() : default(T);
        }
    }
}