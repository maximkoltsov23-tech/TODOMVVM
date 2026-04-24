using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Паттерн_MVVM.Helpers;
using Паттерн_MVVM.Models;
using Паттерн_MVVM.Services;

namespace Паттерн_MVVM.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITaskRepository _repository;
        private readonly IConfirmationService _confirmationService;
        private readonly Func<Task> _operationDelay;
        private readonly ObservableCollection<TaskItemViewModel> _allTasks;
        private FilterOption _selectedFilter;
        private TaskItemViewModel _selectedTask;
        private string _titleInput;
        private string _descriptionInput;
        private bool _isCompletedInput;
        private bool _isBusy;
        private string _statusMessage;
        private string _validationMessage;

        public MainViewModel(ITaskRepository repository, IConfirmationService confirmationService, bool autoLoad = true, Func<Task> operationDelay = null)
        {
            _repository = repository;
            _confirmationService = confirmationService;
            _operationDelay = operationDelay ?? (() => Task.Delay(650));
            _allTasks = new ObservableCollection<TaskItemViewModel>();

            VisibleTasks = new ObservableCollection<TaskItemViewModel>();
            FilterOptions = new ObservableCollection<FilterOption>
            {
                new FilterOption(TaskFilter.All, "Все"),
                new FilterOption(TaskFilter.Active, "Активные"),
                new FilterOption(TaskFilter.Completed, "Выполненные")
            };
            _selectedFilter = FilterOptions[0];

            _titleInput = string.Empty;
            _descriptionInput = string.Empty;
            _statusMessage = "Готово";
            _validationMessage = string.Empty;

            LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            AddTaskCommand = new AsyncRelayCommand(AddTaskAsync, CanSubmit);
            SaveTaskCommand = new AsyncRelayCommand(SaveSelectedTaskAsync, () => SelectedTask != null && CanSubmit());
            DeleteSelectedTaskCommand = new AsyncRelayCommand(DeleteSelectedTaskAsync, () => SelectedTask != null && !IsBusy);
            DeleteCheckedTasksCommand = new AsyncRelayCommand(DeleteCheckedTasksAsync, () => !IsBusy);
            ClearFormCommand = new AsyncRelayCommand(ClearFormAsync, () => !IsBusy);

            if (autoLoad)
            {
                var ignored = LoadAsync();
            }
        }

        public ObservableCollection<TaskItemViewModel> VisibleTasks { get; private set; }

        public ObservableCollection<FilterOption> FilterOptions { get; private set; }

        public FilterOption SelectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    ApplyFilter();
                }
            }
        }

        public TaskItemViewModel SelectedTask
        {
            get { return _selectedTask; }
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    FillFormFromSelection();
                    RaiseCommandStates();
                }
            }
        }

        public string TitleInput
        {
            get { return _titleInput; }
            set
            {
                if (SetProperty(ref _titleInput, value ?? string.Empty))
                {
                    ValidateInput();
                    RaiseCommandStates();
                }
            }
        }

        public string DescriptionInput
        {
            get { return _descriptionInput; }
            set
            {
                if (SetProperty(ref _descriptionInput, value ?? string.Empty))
                {
                    ValidateInput();
                    RaiseCommandStates();
                }
            }
        }

        public bool IsCompletedInput
        {
            get { return _isCompletedInput; }
            set { SetProperty(ref _isCompletedInput, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { SetProperty(ref _statusMessage, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            private set { SetProperty(ref _validationMessage, value); }
        }

        public int TotalCount
        {
            get { return _allTasks.Count; }
        }

        public int ActiveCount
        {
            get { return _allTasks.Count(t => !t.IsCompleted); }
        }

        public int CompletedCount
        {
            get { return _allTasks.Count(t => t.IsCompleted); }
        }

        public ICommand LoadCommand { get; private set; }

        public ICommand AddTaskCommand { get; private set; }

        public ICommand SaveTaskCommand { get; private set; }

        public ICommand DeleteSelectedTaskCommand { get; private set; }

        public ICommand DeleteCheckedTasksCommand { get; private set; }

        public ICommand ClearFormCommand { get; private set; }

        public async Task LoadAsync()
        {
            await RunOperationAsync("Загрузка задач...", async () =>
            {
                _allTasks.Clear();
                var tasks = await _repository.LoadAsync();
                foreach (var task in tasks.OrderByDescending(t => t.CreatedAt))
                {
                    _allTasks.Add(new TaskItemViewModel(task));
                }

                ApplyFilter();
                UpdateCounters();
            }, false);
        }

        public async Task AddTaskAsync()
        {
            if (!CanSubmit())
            {
                ValidateInput();
                return;
            }

            await RunOperationAsync("Добавление задачи...", async () =>
            {
                await _operationDelay();
                var task = new TodoTask(TitleInput.Trim(), DescriptionInput.Trim());
                var item = new TaskItemViewModel(task);
                _allTasks.Insert(0, item);
                await SaveAllAsync();
                SelectedTask = item;
                ApplyFilter();
                ClearForm();
                StatusMessage = "Задача добавлена.";
            });
        }

        public async Task SaveSelectedTaskAsync()
        {
            if (SelectedTask == null || !CanSubmit())
            {
                ValidateInput();
                return;
            }

            await RunOperationAsync("Сохранение изменений...", async () =>
            {
                await _operationDelay();
                SelectedTask.Model.Title = TitleInput.Trim();
                SelectedTask.Model.Description = DescriptionInput.Trim();
                SelectedTask.Model.IsCompleted = IsCompletedInput;
                SelectedTask.Refresh();
                await SaveAllAsync();
                ApplyFilter();
                StatusMessage = "Изменения сохранены.";
            });
        }

        public async Task DeleteSelectedTaskAsync()
        {
            if (SelectedTask == null || !_confirmationService.Confirm("Удалить выбранную задачу?", "Подтверждение удаления"))
            {
                return;
            }

            var task = SelectedTask;
            await RunOperationAsync("Удаление задачи...", async () =>
            {
                await _operationDelay();
                _allTasks.Remove(task);
                SelectedTask = null;
                await SaveAllAsync();
                ApplyFilter();
                ClearForm();
                StatusMessage = "Задача удалена.";
            });
        }

        public async Task DeleteCheckedTasksAsync()
        {
            var checkedTasks = _allTasks.Where(t => t.IsSelectedForBatch).ToList();
            if (checkedTasks.Count == 0 || !_confirmationService.Confirm("Удалить отмеченные задачи?", "Подтверждение удаления"))
            {
                return;
            }

            await RunOperationAsync("Удаление отмеченных задач...", async () =>
            {
                await _operationDelay();
                foreach (var task in checkedTasks)
                {
                    _allTasks.Remove(task);
                }

                if (SelectedTask != null && checkedTasks.Any(t => t.Id == SelectedTask.Id))
                {
                    SelectedTask = null;
                    ClearForm();
                }

                await SaveAllAsync();
                ApplyFilter();
                StatusMessage = "Отмеченные задачи удалены.";
            });
        }

        private Task ClearFormAsync()
        {
            ClearForm();
            SelectedTask = null;
            return Task.FromResult(0);
        }

        private bool CanSubmit()
        {
            return !IsBusy && !new TodoTask(TitleInput, DescriptionInput).Validate().Any();
        }

        private void ValidateInput()
        {
            var errors = new TodoTask(TitleInput, DescriptionInput).Validate().ToList();
            ValidationMessage = errors.Count == 0 ? string.Empty : errors[0];
        }

        private void FillFormFromSelection()
        {
            if (SelectedTask == null)
            {
                return;
            }

            TitleInput = SelectedTask.Title;
            DescriptionInput = SelectedTask.Description;
            IsCompletedInput = SelectedTask.IsCompleted;
        }

        private void ClearForm()
        {
            TitleInput = string.Empty;
            DescriptionInput = string.Empty;
            IsCompletedInput = false;
            ValidationMessage = string.Empty;
        }

        private void ApplyFilter()
        {
            VisibleTasks.Clear();
            IEnumerable<TaskItemViewModel> source = _allTasks;

            if (SelectedFilter != null && SelectedFilter.Value == TaskFilter.Active)
            {
                source = source.Where(t => !t.IsCompleted);
            }
            else if (SelectedFilter != null && SelectedFilter.Value == TaskFilter.Completed)
            {
                source = source.Where(t => t.IsCompleted);
            }

            foreach (var task in source)
            {
                VisibleTasks.Add(task);
            }

            UpdateCounters();
            RaiseCommandStates();
        }

        private Task SaveAllAsync()
        {
            return _repository.SaveAsync(_allTasks.Select(t => t.Model).ToList());
        }

        private async Task RunOperationAsync(string busyMessage, Func<Task> operation, bool useReadyMessage = true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = busyMessage;
                await operation();
                if (useReadyMessage && string.IsNullOrWhiteSpace(StatusMessage))
                {
                    StatusMessage = "Готово";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
                if (!useReadyMessage && StatusMessage == busyMessage)
                {
                    StatusMessage = "Готово";
                }
            }
        }

        private void UpdateCounters()
        {
            OnPropertyChanged("TotalCount");
            OnPropertyChanged("ActiveCount");
            OnPropertyChanged("CompletedCount");
        }

        private void RaiseCommandStates()
        {
            Raise(AddTaskCommand);
            Raise(SaveTaskCommand);
            Raise(DeleteSelectedTaskCommand);
            Raise(DeleteCheckedTasksCommand);
            Raise(ClearFormCommand);
            Raise(LoadCommand);
        }

        private static void Raise(ICommand command)
        {
            var asyncCommand = command as AsyncRelayCommand;
            if (asyncCommand != null)
            {
                asyncCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
