using System;
using CommonModule.Commands;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель диалога вводы числовых данных.
    /// </summary>
    public class NumDlgViewModel : BaseDlgViewModel
    {
        public bool IsSelectAll { get; set; }

        /// <summary>
        /// Вводимый номер
        /// </summary>
        private decimal number;
        public decimal Number 
        {
            get { return number; }
            set 
            { 
                SetAndNotifyProperty("Number", ref number, value); 
            } 
        }

        public int IntValue
        {
            get
            {
                int res = 0;
                try
                {
                    res = Convert.ToInt32(Number);
                }
                catch 
                { }

                return res; ;
            }
            set
            {
                Number = Convert.ToDecimal(value);
            }
        }

        /// <summary>
        /// Подсказка номера
        /// </summary>
        public String Label { get; set; }
    }
}