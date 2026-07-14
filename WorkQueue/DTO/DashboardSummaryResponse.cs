namespace WorkQueue.DTO
{
    public record DashboardSummaryResponse(
        int New,
        int InProgress,
        int Blocked,
        int Done,
        int Overdue
    );
}
