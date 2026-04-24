using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Паттерн_MVVM.Models;

namespace Паттерн_MVVM.Services
{
    public class FileTaskRepository : ITaskRepository
    {
        private readonly string _filePath;

        public FileTaskRepository()
            : this(GetDefaultFilePath())
        {
        }

        public FileTaskRepository(string filePath)
        {
            _filePath = filePath;
        }

        public Task<IList<TodoTask>> LoadAsync()
        {
            return Task.Run<IList<TodoTask>>(() =>
            {
                if (!File.Exists(_filePath))
                {
                    return new List<TodoTask>();
                }

                using (var stream = File.OpenRead(_filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<TodoTask>));
                    var result = serializer.ReadObject(stream) as List<TodoTask>;
                    return result ?? new List<TodoTask>();
                }
            });
        }

        public Task SaveAsync(IList<TodoTask> tasks)
        {
            return Task.Run(() =>
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = File.Create(_filePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(List<TodoTask>));
                    serializer.WriteObject(stream, new List<TodoTask>(tasks));
                }
            });
        }

        private static string GetDefaultFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PatternMvvmTodo");
            return Path.Combine(folder, "tasks.json");
        }
    }
}
