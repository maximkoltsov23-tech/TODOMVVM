namespace Паттерн_MVVM.Services
{
    public interface IConfirmationService
    {
        bool Confirm(string message, string title);
    }
}
