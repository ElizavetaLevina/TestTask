namespace TestTask.Models
{
	public record TaskCompleted(Guid TaskId, string Title, DateTimeOffset? CompletedAt, string Priority);
}
