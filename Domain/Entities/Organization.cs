using System;
using System.Collections.Generic;
using System.Text;

namespace WorkQueue.Domain.Entities
{
    public class Organization
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Навигационное свойство
        public ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
    }
}
