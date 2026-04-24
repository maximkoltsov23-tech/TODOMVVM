using System.Windows;
using Паттерн_MVVM.Services;
using Паттерн_MVVM.ViewModels;

namespace Паттерн_MVVM
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new FileTaskRepository(), new DialogConfirmationService());
        }
    }
}
