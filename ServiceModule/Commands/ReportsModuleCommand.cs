using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Commands;
using System.ComponentModel.Composition;
using CommonModule.ViewModels;
using DAL;
using CommonModule.Interfaces;
using CommonModule.Composition;
using CommonModule.Helpers;
using System.Data.OleDb;
using ServiceModule.ViewModels;
using DataObjects;
using System.IO;
using System.Xml.Linq;
using System.ServiceModel;

namespace ServiceModule.Commands
{
    [ExportModuleCommand("ServiceModule.ModuleCommand", DisplayOrder = 2f)]
    public class ReportsModuleCommand : ModuleCommand
    {
        public ReportsModuleCommand()
        {
            Label = "Отчёты";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            if (Parent == null) return;
            if (Parent.SelectContent<ReportsAdminViewModel>(null)) return;

            BasicModuleContent content = null;
            Action init = () => { content = new ReportsAdminViewModel(Parent, Parent.Repository); };
            Action open = () => { if (content != null) content.TryOpen(); };

            Parent.Services.DoWaitAction(init, "Подождите", "Загрузка содержимого...", open);
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter);
        }
    }
}
