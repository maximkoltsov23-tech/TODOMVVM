namespace Паттерн_MVVM.ViewModels
{
    public enum TaskFilter
    {
        All,
        Active,
        Completed
    }

    public class FilterOption
    {
        public FilterOption(TaskFilter value, string title)
        {
            Value = value;
            Title = title;
        }

        public TaskFilter Value { get; private set; }

        public string Title { get; private set; }
    }
}
