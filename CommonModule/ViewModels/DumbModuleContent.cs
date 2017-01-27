using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Interfaces;
using System.ComponentModel.Composition;

namespace CommonModule.ViewModels
{
    public class DumbModuleContent : BasicModuleContent
    {
        //[Import(AllowDefault = true)]
        //private IOtgruzModule otgruzModule;

        public DumbModuleContent(IModule _parent)
            :base(_parent)
        {
            Title = "Тестовое содержимое модуля";
        }
    }
}
