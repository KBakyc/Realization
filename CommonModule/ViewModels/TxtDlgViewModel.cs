using System;
using CommonModule.Commands;
using System.Windows.Controls;

namespace CommonModule.ViewModels
{
    public class TxtDlgViewModel : BaseDlgViewModel
    {
        /// <summary>
        /// Вводимый текст
        /// </summary>
        public string Text { get; set; }
        public CharacterCasing TextCasing { get; set; }
    }
}