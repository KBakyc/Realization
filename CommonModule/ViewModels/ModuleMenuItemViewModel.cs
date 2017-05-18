using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель отображения элемента меню модуля.
    /// </summary>
    public class ModuleMenuItemViewModel : BasicNotifier
    {
        public ModuleMenuItemViewModel(string _lbl, IEnumerable<ModuleCommand> _cmds)
        {
            command = _cmds.FirstOrDefault(c => c.CanExecute(null));

            if (_cmds.Count() > 1)
            {
                commands = new ObservableCollection<ModuleMenuItemViewModel>(_cmds.Select(c => new ModuleMenuItemViewModel(c.Label, Enumerable.Repeat(c, 1))));
                label = _lbl;
            }
            else 
                label = command != null && !String.IsNullOrWhiteSpace(command.Label) ? command.Label : _lbl;
            
            
            if (command != null)
                isEnabled = true;
            else
                command = _cmds.First();
            
            Parent = command == null ? commands[0].Command.Parent : command.Parent;
            
        }

        public IModule Parent { get; set; }

        private ModuleCommand command;
        public ModuleCommand Command 
        {
            get { return command; } 
        }

        private ObservableCollection<ModuleMenuItemViewModel> commands;
        public ObservableCollection<ModuleMenuItemViewModel> Commands
        {
            get { return commands; }
        }

        private bool isEnabled;
        public bool IsEnabled
        {
            get { return isEnabled; }
        }

        private string label;
        public string Label 
        {
            get { return label; }
        }
    }
}
