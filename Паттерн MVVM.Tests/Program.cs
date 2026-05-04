using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Паттерн_MVVM.Models;
using Паттерн_MVVM.Services;
using Паттерн_MVVM.ViewModels;

namespace Паттерн_MVVM.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                RunAsync().GetAwaiter().GetResult();
                Console.WriteLine("All ViewModel tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static async Task RunAsync()
        {
            await AddTaskAsync_AddsTaskAndPersists();
            await SaveSelectedTaskAsync_UpdatesTask();
            await FilterCompleted_ShowsOnlyCompletedTasks();
            await DeleteCheckedTasksAsync_RemovesMarkedTasks();
        }

        private static async Task AddTaskAsync_AddsTaskAndPersists()
        {
            var repository = new FakeTaskRepository();
            var viewModel = CreateViewModel(repository);

            viewModel.TitleInput = "Подготовить отчет";
            viewModel.DescriptionInput = "Описать реализацию MVVM";
            await viewModel.AddTaskAsync();

            AssertEqual(0, viewModel.TotalCount, "Задача должна добавляться в общий список.");
            AssertEqual(1, repository.SavedTasks.Count, "Добавленная задача должна сохраняться.");
            AssertEqual("Подготовить отчет", repository.SavedTasks[0].Title, "Название сохраняется неверно.");
        }

        private static async Task SaveSelectedTaskAsync_UpdatesTask()
        {
            var repository = new FakeTaskRepository(new[]
            {
                new TodoTask("Исходное название", "Описание")
            });
            var viewModel = CreateViewModel(repository);
            await viewModel.LoadAsync();

            viewModel.SelectedTask = viewModel.VisibleTasks[0];
            viewModel.TitleInput = "Новое название";
            viewModel.IsCompletedInput = true;
            await viewModel.SaveSelectedTaskAsync();

            AssertEqual("Новое название", repository.SavedTasks[0].Title, "Редактирование названия не сохранено.");
            AssertTrue(repository.SavedTasks[0].IsCompleted, "Статус выполнения не сохранен.");
        }

        private static async Task FilterCompleted_ShowsOnlyCompletedTasks()
        {
            var repository = new FakeTaskRepository(new[]
            {
                new TodoTask("Активная", string.Empty),
                new TodoTask("Готовая", string.Empty) { IsCompleted = true }
            });
            var viewModel = CreateViewModel(repository);
            await viewModel.LoadAsync();

            viewModel.SelectedFilter = viewModel.FilterOptions.First(option => option.Value == TaskFilter.Completed);

            AssertEqual(1, viewModel.VisibleTasks.Count, "Фильтр выполненных задач должен скрывать активные задачи.");
            AssertEqual("Готовая", viewModel.VisibleTasks[0].Title, "Фильтр показал неверную задачу.");
        }

        private static async Task DeleteCheckedTasksAsync_RemovesMarkedTasks()
        {
            var repository = new FakeTaskRepository(new[]
            {
                new TodoTask("Первая", string.Empty),
                new TodoTask("Вторая", string.Empty),
                new TodoTask("Третья", string.Empty)
            });
            var viewModel = CreateViewModel(repository);
            await viewModel.LoadAsync();

            viewModel.VisibleTasks[0].IsSelectedForBatch = true;
            viewModel.VisibleTasks[2].IsSelectedForBatch = true;
            await viewModel.DeleteCheckedTasksAsync();

            AssertEqual(1, viewModel.TotalCount, "Отмеченные задачи должны удаляться списком.");
            AssertEqual(1, repository.SavedTasks.Count, "После удаления должен сохраняться актуальный список.");
        }

        private static MainViewModel CreateViewModel(FakeTaskRepository repository)
        {
            return new MainViewModel(repository, new AlwaysConfirmService(), false, () => Task.FromResult(0));
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " Ожидалось: " + expected + ", получено: " + actual + ".");
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    internal class FakeTaskRepository : ITaskRepository
    {
        private readonly List<TodoTask> _tasks;

        public FakeTaskRepository()
            : this(new TodoTask[0])
        {
        }

        public FakeTaskRepository(IEnumerable<TodoTask> tasks)
        {
            _tasks = tasks.ToList();
            SavedTasks = new List<TodoTask>();
        }

        public List<TodoTask> SavedTasks { get; private set; }

        public Task<IList<TodoTask>> LoadAsync()
        {
            return Task.FromResult<IList<TodoTask>>(_tasks.ToList());
        }

        public Task SaveAsync(IList<TodoTask> tasks)
        {
            SavedTasks = tasks.ToList();
            _tasks.Clear();
            _tasks.AddRange(SavedTasks);
            return Task.FromResult(0);
        }
    }

    internal class AlwaysConfirmService : IConfirmationService
    {
        public bool Confirm(string message, string title)
        {
            return true;
        }
    }
}
