using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Паттерн_MVVM.Models
{
    [DataContract]
    public class TodoTask
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public bool IsCompleted { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        public TodoTask()
        {
            Id = Guid.NewGuid();
            Title = string.Empty;
            Description = string.Empty;
            CreatedAt = DateTime.Now;
        }

        public TodoTask(string title, string description)
            : this()
        {
            Title = title;
            Description = description ?? string.Empty;
        }

        public IEnumerable<string> Validate()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                yield return "Название задачи обязательно.";
            }

            if (!string.IsNullOrWhiteSpace(Title) && Title.Trim().Length > 100)
            {
                yield return "Название не должно превышать 100 символов.";
            }

            if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 500)
            {
                yield return "Описание не должно превышать 500 символов.";
            }
        }
    }
}
