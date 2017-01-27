using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonModule.Commands;
using System.ComponentModel.Composition;
using CommonModule.ViewModels;
using DAL;
using CommonModule.Interfaces;
using CommonModule.Composition;
using CommonModule.Helpers;
using System.Data.OleDb;
using ServiceModule.ViewModels;
using DataObjects;
using System.IO;
using System.Xml.Linq;
using System.ServiceModel;
using DotNetHelper;
//using System.Runtime.InteropServices;

namespace ServiceModule.Commands
{    
    //[ExportModuleCommand("ServiceModule.ModuleCommand", DisplayOrder = 99f)]
    public class DumbModuleCommand : ModuleCommand
    {
        public DumbModuleCommand()
        {
            Label = "Тестовая команда";
            GroupName = "DumbModuleCommand";
        }

        public override void Execute(object parameter)
        {
            base.Execute(parameter);
            Test();
        }

        public override bool CanExecute(object parameter)
        {
            return base.CanExecute(parameter);
        }

        private void Test()
        {
            //Parent.Services.ShowMsg("DumbModuleCommand", user.DisplayName, false);

            //var dlg = new ChoicesDlgViewModel(new Choice { Header = "Item1", IsSingleInGroup = true, GroupName = "List" }, new Choice { Header = "Item2", IsSingleInGroup = true, GroupName = "List" }, new Choice { Header = "Item3", IsSingleInGroup = true, GroupName = "List" }) 
            //{
            //    Title = "List Test",
            //    IsVertical = true,                
            //    IsCanClose = true,
            //    OnChangeSelection = (d, c) => { if (c.IsChecked ?? false) Parent.Services.ShowMsg("DumbModuleCommand", c.Header, false); }
            //};

            //Parent.OpenDialog(dlg);
            

            Parent.Services.DoWaitAction(() =>
            {
                var comb = DotNetExtensions.GetCombinations(Enumerable.Range(1, 1000).ToArray(), true, 1, 5);//.OrderBy(c => c.Sum()).ThenBy(c => c.Average()).ThenBy(c => c.Max() - c.Min()).ThenBy(c => c.Length);
                comb = comb.Where(c => c.All(ci => ci % 7 == 0) && c.Sum() % 14 == 0);
                var msg = String.Join(Environment.NewLine, comb.Take(10000).Select(dc => String.Join(", ", dc.Select(d => d.ToString()))));
                Parent.Services.ShowMsg("DumbModuleCommand", msg, false);
            });            
            //var comb = GetCombinations(new string[] { "мама", "мыла", "раму" }, false);

            

            
        }

                    

        private void ShowESFN(string _num)
        {

            // 1 метод. Через прокси-объект
 
            //var vDomain = AppDomain.CreateDomain("EsfnViewDomain");
            //var viewer = vDomain.CreateInstanceAndUnwrap(System.Reflection.Assembly.GetExecutingAssembly().FullName,
            //                                              "ServiceModule.Commands.DomainProxy") as DomainProxy;

            //viewer.ShowEsfn(_num);
            
            //AppDomain.Unload(vDomain);
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //GC.Collect();

            //return;

            // 2 метод. через базовый класс Form, который служит прокси-объектом
            var vDomain = AppDomain.CreateDomain("EsfnViewDomain");

            var viewver = vDomain.CreateInstanceFrom(@"VatInvoice\VatInvoiceView.dll", "VatInvoiceView.VatInvForm", true, System.Reflection.BindingFlags.Default, null, new object[] { _num }, null, null)
                .Unwrap()
                as System.Windows.Forms.Form;

            viewver.ShowDialog();
            viewver.Dispose();

            AppDomain.Unload(vDomain);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return;


            //var viAssembly = System.Reflection.Assembly.LoadFrom(@"VatInvoice\VatInvoiceView.dll");
            //var viViewverType = viAssembly.GetType("VatInvoiceView.VatInvForm");

            //var viViewverType = viewver.GetType();

            //if (viViewverType == null)
            //{
            //    Parent.Services.ShowMsg("Ошибка", "Не удалось закрузить тип VatInvoiceView.VatInvForm", true);
            //    return;
            //}

            //var viViewverConstructor = viViewverType.GetConstructor(new Type[] { typeof(string) });
            //var viViewverShowMethod = viViewverType.GetMethod("ShowDialog", new Type[] { });                     
            
            //var viViewverControlsProp = viViewverType.GetProperty("Controls");          

            //object viViewverInstance = null;
            //try
            //{
            //    viViewverInstance = viViewverConstructor.Invoke(new object[] { _num });
            //}
            //catch (Exception e)
            //{
            //    Parent.Services.ShowMsg("Ошибка создания объекта", e.Message, true);
            //    return;
            //}

            //var viViewverEnabled = viViewverType.GetProperty("Enabled");  
            
            //var viViewverControls = (System.Collections.IList)viViewverControlsProp.GetValue(viViewverInstance, new object[] { });
            //for (int i = 1; i < viViewverControls.Count; i++)
            //{
            //    var control = viViewverControls[i];
            //    var cType = control.GetType();
            //    //var cCheckProp = cType.GetProperty("CanSelect");
            //    //var cCheck = (bool)cCheckProp.GetValue(control, new object[] { });
            //    //if (cCheck)
            //    //{
            //    //    var cEnabledProp = cType.GetProperty("Enabled");
            //    //    cEnabledProp.SetValue(control, false, new object[] { });
            //    //}
            //    var cEnabledProp = cType.GetProperty("Enabled");
            //    cEnabledProp.SetValue(control, false, new object[] { });
            //}

            //viViewverEnabled.SetValue(viViewverInstance, false, new object[] { });
            //viViewverShowMethod.Invoke(viViewverInstance, new object[] { });
        }

        //var context = new System.DirectoryServices.AccountManagement.PrincipalContext(System.DirectoryServices.AccountManagement.ContextType.Domain, "LAN");
        //var users = System.DirectoryServices.AccountManagement.GroupPrincipal.FindByIdentity(context, "ttt").GetMembers().OfType<System.DirectoryServices.AccountManagement.UserPrincipal>();
        //var user = System.DirectoryServices.AccountManagement.UserPrincipal.FindByIdentity(context, "sudak");

        //[DllImport("user32.dll", CharSet = CharSet.Unicode)]
        //public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);        
    }

    //public class DomainProxy : MarshalByRefObject
    //{
    //    public void ShowEsfn(string _num)
    //    {
    //        var viAssembly = System.Reflection.Assembly.LoadFrom(@"VatInvoice\VatInvoiceView.dll");
    //        var viViewverType = viAssembly.GetType("VatInvoiceView.VatInvForm");

    //        if (viViewverType == null) return;

    //        var viViewverConstructor = viViewverType.GetConstructor(new Type[] { typeof(string) });
    //        var viViewverShowMethod = viViewverType.GetMethod("ShowDialog", new Type[] { });

    //        object viViewverInstance = null;
    //        try
    //        {
    //            viViewverInstance = viViewverConstructor.Invoke(new object[] { _num });
    //        }
    //        catch
    //        {
    //            return;
    //        }
    //        viViewverShowMethod.Invoke(viViewverInstance, new object[] { });
    //    }
    //}
}
