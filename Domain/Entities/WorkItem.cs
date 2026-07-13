using System.ComponentModel.DataAnnotations;
using WorkQueue.Domain.Enums;

namespace WorkQueue.Domain.Entities
{
    public class WorkItem
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public WorkItemStatus Status { get; set; } = WorkItemStatus.New;
        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

        public Guid? AssigneeUserId { get; set; }
        public DateTime? DueDate { get; set; }

        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();


        public User? AssigneeUser { get; set; }
        public User? CreatedByUser { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
