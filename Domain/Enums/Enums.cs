using System;
using System.Collections.Generic;
using System.Text;

namespace WorkQueue.Domain.Enums
{
    public enum WorkItemStatus
    {
        New,
        InProgress,
        Blocked,
        Done
    }

    public enum WorkItemPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }
    public enum UserRole
    {
        Manager,
        Member
    }
}
