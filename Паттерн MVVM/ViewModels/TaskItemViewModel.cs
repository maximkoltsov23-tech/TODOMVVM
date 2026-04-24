using System;
using Паттерн_MVVM.Models;

namespace Паттерн_MVVM.ViewModels
{
    public class TaskItemViewModel : ViewModelBase
    {
        private bool _isSelectedForBatch;

        public TaskItemViewModel(TodoTask model)
        {
            Model = model;
        }

        public TodoTask Model { get; private set; }

        public Guid Id
        {
            get { return Model.Id; }
        }

        public string Title
        {
            get { return Model.Title; }
        }

        public string Description
        {
            get { return Model.Description; }
        }

        public bool IsCompleted
        {
            get { return Model.IsCompleted; }
        }

        public string StatusText
        {
            get { return Model.IsCompleted ? "Выполнена" : "Активна"; }
        }

        public DateTime CreatedAt
        {
            get { return Model.CreatedAt; }
        }

        public bool IsSelectedForBatch
        {
            get { return _isSelectedForBatch; }
            set { SetProperty(ref _isSelectedForBatch, value); }
        }

        public void Refresh()
        {
            OnPropertyChanged("Title");
            OnPropertyChanged("Description");
            OnPropertyChanged("IsCompleted");
            OnPropertyChanged("StatusText");
            OnPropertyChanged("CreatedAt");
        }
    }
}
