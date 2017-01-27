using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects.Events
{
    public class ErrorEventArgs : EventArgs
    {
        private string title;
        private string message;
        private bool issilent;
        private Action onSubmit;

        public ErrorEventArgs(string _t, string _m, Action _onSubmit)
        {
            title = _t;
            message = _m;
            onSubmit = _onSubmit;
        }

        public ErrorEventArgs(string _t, string _m, bool _s)
        {
            title = _t;
            message = _m;
            issilent = _s;
        }
        
        public ErrorEventArgs(string _t, string _m)
            :this(_t, _m, false)
        {}

        public string Title { get { return title; } }
        public string Message { get { return message; } }
        public bool IsSilent { get { return issilent; } }
        public Action OnSubmit { get { return onSubmit; } }
    }
}
