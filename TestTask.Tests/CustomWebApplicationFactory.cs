using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TestTask.Services;
using Moq;

namespace TestTask.Tests
{
	public class CustomWebApplicationFactory : WebApplicationFactory<Program>
	{
		public Mock<IRabbitMqService> RabbitMock { get; } = new();

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			builder.ConfigureServices(services =>
			{
				services.AddScoped(_ => RabbitMock.Object);
			});
		}
	}
}
