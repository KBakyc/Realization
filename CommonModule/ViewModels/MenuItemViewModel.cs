using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using CommonModule.Commands;
using CommonModule.Interfaces;
using CommonModule.Helpers;
using System.Windows.Input;

namespace CommonModule.ViewModels
{
    public class MenuItemViewModel : BasicNotifier
    {
        public MenuItemViewModel(string _lbl, string _descr, params LabelCommand[] _cmds)
        {
            command = _cmds.FirstOrDefault(c => c.CanExecute());

            if (_cmds.Length > 1)
            {
                commands = new ObservableCollection<MenuItemViewModel>(_cmds.Select(c => new MenuItemViewModel(c.Label, null, c)));
                label = _lbl;                
            }
            else
                label = command != null && !String.IsNullOrWhiteSpace(command.Label) ? command.Label : _lbl;
            
            description = _descr;

            if (command != null)
                isEnabled = true;
            else
                command = _cmds[0];
        }

        private LabelCommand command;
        public LabelCommand Command
        {
            get { return command; }
        }

        private ObservableCollection<MenuItemViewModel> commands;
        public ObservableCollection<MenuItemViewModel> Commands
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
        
        private string description;
        public string Description
        {
            get { return description; }
        }
    }
}
