using System.Windows;

namespace Паттерн_MVVM.Services
{
    public class DialogConfirmationService : IConfirmationService
    {
        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
