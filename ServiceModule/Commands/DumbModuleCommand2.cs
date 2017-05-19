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
//using System.Runtime.InteropServices;

namespace ServiceModule.Commands
{
    /// <summary>
    /// Тестовая команда модуля
    /// </summary>
    //[ExportModuleCommand("ServiceModule.ModuleCommand", DisplayOrder = 99f)]
    public class DumbModuleCommand2 : ModuleCommand
    {
        public DumbModuleCommand2()
        {
            Label = "Тестовая команда 2";
            GroupName = "DumbModuleCommand";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            Test();
        }

        protected override int MinParentAccess
        {
            get
            {
                return 0;
            }
        }

        private void Test()
        {
            Parent.OpenDialog(new DumbDlgViewModel(Parent.Repository) { Title = "Тест"});
        }

        //[DllImport("user32.dll", CharSet = CharSet.Unicode)]
        //public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);        

    }
}
