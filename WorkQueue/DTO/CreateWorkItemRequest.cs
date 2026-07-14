namespace WorkQueue.DTO
{
    public class CreateWorkItemRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? AssigneeId { get; set; }
    }

    public class UpdateWorkItemRequest : CreateWorkItemRequest
    {
        public int Status { get; set; }
    }
}
