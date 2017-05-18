using System;
using System.Linq;
using CommonModule.Interfaces;
using DataObjects;
using System.Collections.Generic;
using CommonModule.Helpers;

namespace CommonModule.ViewModels
{
    /// <summary>
    /// Модель режима модуля для отображения HTML страницы.
    /// </summary>
    public class HtmlPageViewModel : BasicModuleContent
    {
        private string path;

        public HtmlPageViewModel(IModule _parent, string _path)
            : base(_parent)
        {
            path = _path;
            if (Parent != null && !IsValid)
                Parent.Services.ShowMsg("Ошибка", errMsg, true);
        }

        /// <summary>
        /// Полный путь к странице
        /// </summary>
        public string HtmlPath
        {
            get { return path; }
        }

        private Uri htmlPathUri;
        public Uri HtmlPathUri
        {
            get { return htmlPathUri; }
        }

        private string errMsg;
        public bool IsValid
        {
            get
            {
                if (errMsg == null)
                    errMsg = Check();
                return String.Empty == errMsg;
            }
        }

        private string Check()
        {
            if (String.IsNullOrWhiteSpace(path)) return "Не указана страница для отображения";
            try
            {
                htmlPathUri = new Uri(path);
            }
            catch {}
            
            if (htmlPathUri == null)
            {
                try
                {
                    var fullpath = System.IO.Path.GetFullPath(path);
                    htmlPathUri = new Uri(fullpath);
                }
                catch (Exception e)
                {
                    return String.Format("Неверный путь: {0}\n{1}", path, e.Message);
                }
            }
            if (htmlPathUri.IsFile && !System.IO.File.Exists(htmlPathUri.LocalPath))
                    return String.Format("Страница {0} не найдена!", path);            
            
            return String.Empty;
        }
    }
}