using System.Threading.Tasks;
using TestTask.Models;

namespace TestTask.Services
{
	public interface IRabbitMqService
	{
		Task PublishTaskCompletedAsync(TaskItem task);
	}
}
