using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Helpers;
using CommonModule.Commands;
using System.Xml.Linq;
using System.Windows.Input;
using System.IO;

namespace ServiceModule.ViewModels
{
    public enum UserSelectMode { None, Current, All, Filtered };
    public enum OnlineMode { All, Online, Offline };
    public enum EnabledMode { All, Enabled, Disabled };
    public enum CiElements { None, Execute, Message };

    public class SecurityContextInfo
    {
        public int? Id { get; set; }
        public string FullName { get; set; }
    }

    public abstract class ElAttribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class BoolAttribute : ElAttribute
    {
        public bool ElValue { get; set; }
        public override string ToString()
        {
            return ElValue.ToString();
        }
    }

    public class StringAttribute : ElAttribute
    {
        public string ElValue { get; set; }
        public override string ToString()
        {
            return ElValue;
        }
    }

    public class CiElementConstructor : BasicNotifier
    {
        public CiElementConstructor(CiElements _eltype)
        {
            eltype = _eltype;
            Initialise();
            if (_eltype == CiElements.Execute)
            {
                LoadSaved();
                if (savedData != null)
                    ParseSaved(_eltype, savedData.Where(s => s.Name == _eltype.ToString()).ToArray());
            }
            parseSavedElementCommand = new DelegateCommand<XElement>(ExecParseSavedElement);
        }

        public XElement CreateCiElement()
        {
            XElement res = null;
            switch (eltype)
            {
                case CiElements.Message:
                    res = new XElement(WorkFlowHelper.CI_MESSAGE, 
                        ElementAttributes.Values.Where(v => !(v is StringAttribute) || !String.IsNullOrWhiteSpace((v as StringAttribute).ElValue))
                                                .Select(v => new XAttribute(v.Name, v.ToString()))
                                                .ToArray());
                    if (!String.IsNullOrWhiteSpace(Content))
                        res.Value = Content;
                    break;
                case CiElements.Execute:
                    bool issys = false;
                    var issysAtt = ElementAttributes.Values.OfType<BoolAttribute>().FirstOrDefault(v => v.Name == "IsSystem");
                    if (issysAtt != null)
                        issys = issysAtt.ElValue;
                    res = new XElement(WorkFlowHelper.CI_EXECUTE, 
                        ElementAttributes.Values.Where(v => (!issys || v.Name != "DelAfter")
                                                         && (!(v is StringAttribute) || !String.IsNullOrWhiteSpace((v as StringAttribute).ElValue)))
                                                         .Select(v => new XAttribute(v.Name, v.ToString()))
                                                         .ToArray());                                                        
                    if (!String.IsNullOrWhiteSpace(Content))
                        res.Value = Content;
                    break;
                default:
                    break;
            }

            return res;
        }

        //private string[] savedData = new string[] 
        //{ 
        //    "<Execute RunOnce=\"true\" DelAfter=\"true\" Restart=\"true\" Url=\"http://ivc-project/sharedinstallations/naftan/Real/Last/Maintaince/workconfig.exe\">workconfig.exe</Execute>",
        //    "<Execute RunOnce=\"true\" DelAfter=\"true\" Restart=\"true\" Url=\"http://ivc-project/sharedinstallations/naftan/Real/Last/Maintaince/usoconfig.exe\">usoconfig.exe</Execute>",
        //    "<Execute RunOnce=\"true\" DelAfter=\"false\" Restart=\"false\" IsSystem=\"false\" Url=\"http://ivc-project/sharedinstallations/naftan/Real/Last/RwModule/RwModule.dll\"></Execute>",
        //    "<Execute RunOnce=\"true\" DelAfter=\"false\" Restart=\"false\" IsSystem=\"false\" Url=\"http://ivc-project/sharedinstallations/naftan/Real/Last/ServiceModule/ServiceModule.dll\"></Execute>",
        //    "<Execute RunOnce=\"true\" DelAfter=\"true\" Restart=\"false\" NoWindow=\"true\" Url=\"http://ivc-project/sharedinstallations/naftan/Real/Last/Maintaince/getlog.cmd\">getlog.cmd</Execute>",
        //    "<Execute RunOnce=\"true\" IsSystem=\"true\">notepad.exe</Execute>"
        //};

        private XElement[] savedData;

        private Dictionary<XElement, string> savedElements;
        public Dictionary<XElement, string> SavedElements
        {
            get { return savedElements; }
            set { SetAndNotifyProperty(() => SavedElements, ref savedElements, value); }
        }

        private const string EXECUTES_ROOT_ELEMENT_NAME = "Executes";
        private const string EXECUTE_ELEMENT_NAME = "Execute";

        private void LoadSaved()
        {
            var fileName = GetType().Assembly.GetName().Name + ".xml";
            string settingsString = null;
            if (File.Exists(fileName))
            {
                using (var sr = new StreamReader(fileName))
                {
                    settingsString = sr.ReadToEnd();
                }
                if (!String.IsNullOrWhiteSpace(settingsString))
                {
                    XElement el = null;
                    try
                    {
                        el = XElement.Parse(settingsString);
                        if (el != null && el.HasElements)
                        {
                            if (el.Name == EXECUTES_ROOT_ELEMENT_NAME)
                                savedData = el.Elements(EXECUTE_ELEMENT_NAME).ToArray();
                            else
                                savedData = el.Elements(EXECUTES_ROOT_ELEMENT_NAME).SelectMany(e => e.Elements(EXECUTE_ELEMENT_NAME)).ToArray();
                        }
                    }
                    catch { }                    
                }
            }            
        }

        private void ParseSaved(CiElements _type, params XElement[] _data)
        {
            var newdict = new Dictionary<XElement, string>();
            Array.ForEach(_data, el =>
            {                
                var tit = el.Value;
                if (String.IsNullOrWhiteSpace(tit))
                {
                    if (_type == CiElements.Execute)
                    {
                        var urlatt = el.Attribute("Url");
                        if (urlatt != null && !String.IsNullOrWhiteSpace(urlatt.Value))
                        {
                            tit = System.IO.Path.GetFileName(urlatt.Value);
                        }
                    }
                }
                if (String.IsNullOrWhiteSpace(tit)) tit = "Без заголовка";
                newdict.Add(el, tit);
            });
            SavedElements = newdict;
        }

        private ICommand parseSavedElementCommand;
        public ICommand ParseSavedElementCommand
        {
            get { return parseSavedElementCommand; }
        }

        private void ExecParseSavedElement(XElement _el)
        {
            Initialise();
            if (_el == null) return;
            var newattr = _el.Attributes().ToArray();
            // var newdict = new Dictionary<string, ElAttribute>();
            Array.ForEach(newattr, a =>
            {
                var eakv = elementAttributes.FirstOrDefault(ela => ela.Value.Name == a.Name);
                var ea = eakv.Value;
                if (ea == null) return;
                if (ea is StringAttribute)
                    ((StringAttribute)ea).ElValue = a.Value;
                else
                    if (ea is BoolAttribute)
                    {
                        bool val;
                        if (Boolean.TryParse(a.Value, out val))
                            ((BoolAttribute)ea).ElValue = val;
                    }
                //newdict.Add(eakv.Key, ea);
            });
            Content = _el.Value;
            //ElementAttributes = newdict;
        }


        private void Initialise()
        {
            var eAttributes = new Dictionary<string, ElAttribute>();
            switch (eltype)
            {
                case CiElements.Message:
                    //eAttributes.Add("Текст сообщения", new StringAttribute { Name = "Text", ElValue = "" });
                    ContentTitle = "Сообщение пользоватеям при загрузке";
                    Content = null;
                    break;
                case CiElements.Execute:
                    eAttributes.Add("URL для скачивания", new StringAttribute { Name = "Url", ElValue = "" });
                    eAttributes.Add("Однократное выполнение", new BoolAttribute { Name = "RunOnce", ElValue = true });
                    eAttributes.Add("Удалить после выполнения", new BoolAttribute { Name = "DelAfter", ElValue = false });
                    eAttributes.Add("Перезапуск АРМа после выполнения", new BoolAttribute { Name = "Restart", ElValue = false });
                    eAttributes.Add("Системное обновление", new BoolAttribute { Name = "IsSystem", ElValue = false, Description = "Запуск системной комманды или приложения. Путь к команде ищет ОС. Без удаления." });
                    eAttributes.Add("Без окна", new BoolAttribute { Name = "NoWindow", ElValue = true, Description = "Не открывать для процесса новое окно" });
                    ContentTitle = "Команда запуска";
                    Content = null;
                    break;
                default:
                    break;
            }
            ElementAttributes = eAttributes;
        }

        private CiElements eltype;

        private Dictionary<string, ElAttribute> elementAttributes;
        public Dictionary<string, ElAttribute> ElementAttributes
        {
            get { return elementAttributes; }
            set { SetAndNotifyProperty(() => ElementAttributes, ref elementAttributes, value); }
        }

        private string content;
        public string Content
        {
            get { return content; }
            set { SetAndNotifyProperty(() => Content, ref content, value); }
        }

        public string ContentTitle { get; set; }
    }
}
