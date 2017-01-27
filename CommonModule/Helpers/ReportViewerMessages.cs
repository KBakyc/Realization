using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Reporting.WinForms;

namespace CommonModule.Helpers
{
    public class ReportViewerMessages : IReportViewerMessages3
    {
        public string CancelLinkText
        {
            get { return null; }
        }

        public string ExportDialogCancelButton
        {
            get { return null; }
        }

        public string ExportDialogStatusText
        {
            get { return null; }
        }

        public string ExportDialogTitle
        {
            get { return null; }
        }

        public string FalseBooleanToolTip
        {
            get { return null; }
        }

        public string TotalPages(int pageCount, PageCountMode pageCountMode)
        {
            return pageCount.ToString();
        }

        public string TrueBooleanToolTip
        {
            get { return null; }
        }

        public string AllFilesFilter
        {
            get { return null; }
        }

        public string CredentialMissingUserNameError(string dataSourcePrompt)
        {
            return null;
        }

        public string DateToolTip
        {
            get { return "Дата"; }
        }

        public string ExportErrorTitle
        {
            get { return null; }
        }

        public string FloatToolTip
        {
            get { return null; }
        }

        public string GetLocalizedNameForRenderingExtension(string format)
        {
            return null;
        }

        public string HyperlinkErrorTitle
        {
            get { return null; }
        }

        public string IntToolTip
        {
            get { return null; }
        }

        public string MessageBoxTitle
        {
            get { return null; }
        }

        public string ParameterMissingSelectionError(string parameterPrompt)
        {
            return null;
        }

        public string ParameterMissingValueError(string parameterPrompt)
        {
            return null;
        }

        public string ProcessingStopped
        {
            get { return null; }
        }

        public string PromptAreaErrorTitle
        {
            get { return null; }
        }

        public string StringToolTip
        {
            get { return null; }
        }

        public string BackButtonToolTip
        {
            get { return "Назад"; }
        }

        public string BackMenuItemText
        {
            get { return "Назад"; }
        }

        public string ChangeCredentialsText
        {
            get { return null; }
        }

        public string CurrentPageTextBoxToolTip
        {
            get { return "Текущая страница"; }
        }

        public string DocumentMapButtonToolTip
        {
            get { return null; }
        }

        public string DocumentMapMenuItemText
        {
            get { return null; }
        }

        public string ExportButtonToolTip
        {
            get { return "Экспорт"; }
        }

        public string ExportMenuItemText
        {
            get { return null; }
        }

        public string FalseValueText
        {
            get { return null; }
        }

        public string FindButtonText
        {
            get { return "Найти"; }
        }

        public string FindButtonToolTip
        {
            get { return "Найти"; }
        }

        public string FindNextButtonText
        {
            get { return "Продолжить"; }
        }

        public string FindNextButtonToolTip
        {
            get { return "Продолжить поиск"; }
        }

        public string FirstPageButtonToolTip
        {
            get { return "В начало"; }
        }

        public string LastPageButtonToolTip
        {
            get { return "В конец"; }
        }

        public string NextPageButtonToolTip
        {
            get { return "Следующая страница"; }
        }

        public string NoMoreMatches
        {
            get { return "Больше не найдено"; }
        }

        public string NullCheckBoxText
        {
            get { return null; }
        }

        public string NullCheckBoxToolTip
        {
            get { return null; }
        }

        public string NullValueText
        {
            get { return null; }
        }

        public string PageOf
        {
            get { return "из"; }
        }

        public string PageSetupButtonToolTip
        {
            get { return "Настройка страницы"; }
        }

        public string PageSetupMenuItemText
        {
            get { return null; }
        }

        public string ParameterAreaButtonToolTip
        {
            get { return null; }
        }

        public string PasswordPrompt
        {
            get { return "Введите пароль"; }
        }

        public string PreviousPageButtonToolTip
        {
            get { return "Предыдущая страница"; }
        }

        public string PrintButtonToolTip
        {
            get { return "Печать"; }
        }

        public string PrintLayoutButtonToolTip
        {
            get { return "Вид при печати"; }
        }

        public string PrintLayoutMenuItemText
        {
            get { return null; }
        }

        public string PrintMenuItemText
        {
            get { return null; }
        }

        public string ProgressText
        {
            get { return "Загрузка"; }
        }

        public string RefreshButtonToolTip
        {
            get { return "Обновить"; }
        }

        public string RefreshMenuItemText
        {
            get { return null; }
        }

        public string SearchTextBoxToolTip
        {
            get { return "Что искать"; }
        }

        public string SelectAValue
        {
            get { return null; }
        }

        public string SelectAll
        {
            get { return "Выделить всё"; }
        }

        public string StopButtonToolTip
        {
            get { return "Остановить"; }
        }

        public string StopMenuItemText
        {
            get { return null; }
        }

        public string TextNotFound
        {
            get { return "Не найдено"; }
        }

        public string TotalPagesToolTip
        {
            get { return "Всего страниц"; }
        }

        public string TrueValueText
        {
            get { return null; }
        }

        public string UserNamePrompt
        {
            get { return "Имя пользователя"; }
        }

        public string ViewReportButtonText
        {
            get { return null; }
        }

        public string ViewReportButtonToolTip
        {
            get { return null; }
        }

        public string ZoomControlToolTip
        {
            get { return "Масштаб документа"; }
        }

        public string ZoomMenuItemText
        {
            get { return "Масштаб"; }
        }

        public string ZoomToPageWidth
        {
            get { return "По ширине"; }
        }

        public string ZoomToWholePage
        {
            get { return "Вся страница"; }
        }
    }
}
