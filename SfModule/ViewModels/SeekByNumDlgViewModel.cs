using System;
using CommonModule.Commands;
using CommonModule.ViewModels;

namespace SfModule.ViewModels
{
    public class SeekByNumDlgViewModel : BaseDlgViewModel
    {
        public SeekByNumDlgViewModel()
        {
            IsCurrentYear = true;
            Title = "Поиск счёта-фактуры по номеру";
        }

        public bool IsCurrentYear { get; set; }

        /// <summary>
        /// Вводимый номер
        /// </summary>
        public int Number { get; set; }
    }
}