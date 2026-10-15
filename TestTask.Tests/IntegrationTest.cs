using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TestTask.Models;
using TestTask.Services;

namespace TestTask.Tests
{
	public class IntegrationTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
	{
		private readonly CustomWebApplicationFactory _factory = factory;

		[Fact]
		public async Task CompleteTask_ShouldPublishToRabbitMQ()
		{
			var client = _factory.CreateClient();

			var createResponse = await client.PostAsJsonAsync("/tasks", new TaskItem
			{
				Title = "Test task"
			});

			Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

			var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskItem>();

			var completeResponse = await client.PutAsync($"/tasks/{createdTask.Id}/complete", null);

			Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

			_factory.RabbitMock.Verify(
				r => r.PublishTaskCompletedAsync(It.Is<TaskItem>(t => t.Id == createdTask.Id && t.IsCompleted)),
				Times.Once);
		}
	}
}
