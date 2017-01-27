using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonModule.Commands
{
    public class LabelCommand : DelegateCommand
    {
        #region Constructors

        /// <summary>
        ///     Constructor
        /// </summary>
        public LabelCommand(Action executeMethod)
            : this(executeMethod, null, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public LabelCommand(Action executeMethod, Func<bool> canExecuteMethod)
            : this(executeMethod, canExecuteMethod, false)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public LabelCommand(Action executeMethod, Func<bool> canExecuteMethod, bool isAutomaticRequeryDisabled)
            :base(executeMethod, canExecuteMethod, isAutomaticRequeryDisabled)
        {
        }

        #endregion

        public String Label { get; set; }
    }
}
