using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataObjects.Events
{
    public static class EventsExtension
    {
        public static void Raise<TEventArgs>(this TEventArgs e, Object sender, ref EventHandler<TEventArgs> eventDelegate) where TEventArgs:EventArgs
        {    // Копирование ссылки на поле делегата во временное поле 
            // для безопасности в отношении потоков
            EventHandler<TEventArgs> temp = eventDelegate;
            // Если зарегистрированный метод заинтересован в событии, уведомите его
            if (temp != null) temp(sender, e);
        }
    }
}
