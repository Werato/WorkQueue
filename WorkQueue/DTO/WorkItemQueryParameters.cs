namespace WorkQueue.DTO
{
    public class WorkItemQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public string? Search { get; set; }
        public int? Status { get; set; }
        public int? Priority { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? SortBy { get; set; }
    }
}
