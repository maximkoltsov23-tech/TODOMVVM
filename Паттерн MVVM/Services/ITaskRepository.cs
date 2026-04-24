using System.Collections.Generic;
using System.Threading.Tasks;
using Паттерн_MVVM.Models;

namespace Паттерн_MVVM.Services
{
    public interface ITaskRepository
    {
        Task<IList<TodoTask>> LoadAsync();

        Task SaveAsync(IList<TodoTask> tasks);
    }
}
