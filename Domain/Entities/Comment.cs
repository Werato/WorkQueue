using System;
using System.Collections.Generic;
using System.Text;

namespace WorkQueue.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;

        // Связь с задачей
        public Guid WorkItemId { get; set; }
        public WorkItem? WorkItem { get; set; }

        // Связь с автором
        public Guid CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
