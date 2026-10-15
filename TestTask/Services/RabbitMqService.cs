using MassTransit;
using TestTask.Models;

namespace TestTask.Services
{
	public class RabbitMqService(IPublishEndpoint publishEndpoint, ILogger<RabbitMqService> logger) : IRabbitMqService
	{
		private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
		private readonly ILogger<RabbitMqService> _logger = logger;

		public async Task PublishTaskCompletedAsync(TaskItem task)
		{
			try
			{
				await _publishEndpoint.Publish(new TaskCompleted(task.Id, task.Title, task.CompletedAt, task.Priority.ToString()));

				_logger.LogInformation("Задача {TaskId} успешно отправлена", task.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка в публикации задачи {TaskId}", task.Id);
			}
		}
	}
}
