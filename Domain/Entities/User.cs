using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using WorkQueue.Domain.Enums;

namespace WorkQueue.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Member;

        // Изоляция арендатора (Tenant)
        public Guid OrganizationId { get; set; }
        public Organization? Organization { get; set; }

        // Навигационные свойства для связей с WorkItem
        public ICollection<WorkItem> AssignedWorkItems { get; set; } = new List<WorkItem>();
        public ICollection<WorkItem> CreatedWorkItems { get; set; } = new List<WorkItem>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
