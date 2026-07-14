using System;
using System.Collections.Generic;
using System.Text;

namespace WorkQueue.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;

        public Guid WorkItemId { get; set; }
        public WorkItem? WorkItem { get; set; }

        public Guid CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        // Tenant Isolation (Add these two lines)
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
