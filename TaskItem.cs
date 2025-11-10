using System;

namespace ExamBuddy
{
    internal enum TaskStatus
    {
        Pending,
        InProgress,
        Completed
    }

    internal static class TaskStatusExtensions
    {
        public static string ToDisplay(this TaskStatus status) =>
            status switch
            {
                TaskStatus.Pending => "Beklemede",
                TaskStatus.InProgress => "Çalışılıyor",
                TaskStatus.Completed => "Tamamlandı",
                _ => status.ToString()
            };

        public static TaskStatus FromToken(string token)
        {
            return token switch
            {
                "Pending" or "Beklemede" => TaskStatus.Pending,
                "InProgress" or "Çalışılıyor" or "In-Progress" => TaskStatus.InProgress,
                "Completed" or "Tamamlandı" => TaskStatus.Completed,
                _ => TaskStatus.Pending
            };
        }

        public static string ToToken(this TaskStatus status) =>
            status switch
            {
                TaskStatus.Pending => "Pending",
                TaskStatus.InProgress => "In-Progress",
                TaskStatus.Completed => "Completed",
                _ => "Pending"
            };
    }

    internal sealed class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string DueDate { get; set; } = string.Empty;

        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        public string Description { get; set; } = string.Empty;

        public string[] ToRow()
        {
            var due = string.IsNullOrWhiteSpace(DueDate) ? "-" : DueDate;
            return new[]
            {
                Id.ToString(),
                Title,
                Status.ToDisplay(),
                Shorten(Description, 60),
                due
            };
        }

        private static string Shorten(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "-";
            }

            var trimmed = value.Replace(Environment.NewLine, " ");
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed.Substring(0, maxLength - 3) + "...";
        }
    }
}


