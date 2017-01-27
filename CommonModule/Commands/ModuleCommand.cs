using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;

namespace CommonModule.Commands
{
    public abstract class ModuleCommand : Command
    {

        public IModule Parent { get; set; }
        public String Label { get; set; }
        public String GroupName { get; set; }

        protected bool isReadOnly;

        protected virtual int MinParentAccess { get { return 1; } }

        public ModuleCommand()
        {
        }

        public void SetAccess(int _al)
        {
            isReadOnly = _al < 2;
        }

        public override bool CanExecute(object parameter)
        {
            return Parent != null && Parent.AccessLevel >= MinParentAccess;
        }

        public override void Execute(object parameter)
        {
            Helpers.WorkFlowHelper.WriteToLog(null, String.Format("Модуль: [{0}] Команда: [{1}] [Exec]", Parent == null ? "Null" : Parent.Info.Name, Label));
        }

    }
}
