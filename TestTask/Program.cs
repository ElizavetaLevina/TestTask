
using MassTransit;
using Microsoft.EntityFrameworkCore;
using TestTask.Data;
using TestTask.Models;
using TestTask.Services;

namespace TestTask
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddAuthorization();

			// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
			builder.Services.AddOpenApi();

			#region Services
			//миграциции делались вручную dotnet ef migrations add, dotnet ef database update
			builder.Services.AddDbContext<TaskDbContext>(o =>
				o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

			// MassTransit — тк только с ней работала
			builder.Services.AddMassTransit(x =>
			{
				x.UsingRabbitMq((context, cfg) =>
				{
					var host = builder.Configuration["RabbitMQ:Host"];
					var port = builder.Configuration["RabbitMQ:Port"];
					var user = builder.Configuration["RabbitMQ:UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName не установлен");
					var pass = builder.Configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException("RabbitMQ:Password не установлен");

					cfg.Host($"rabbitmq://{host}:{port}", h =>
					{
						h.Username(user);
						h.Password(pass);
					});
				});
			});

			builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
			#endregion

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.MapOpenApi();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();

			// HACK: Swagger не успела подключить

			#region Endpoints
			// HACK: пагинацию не успела сделать
			app.MapGet("/tasks", async (TaskDbContext dbContext) => Results.Ok(await dbContext.Tasks.ToListAsync()));

			// CompletedAt = null при создании. Заполняется только в PUT /complete
			app.MapPost("/tasks", async (TaskItem task, TaskDbContext dbContext) =>
			{
				// HACK: Валидация вручную
				if (string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200)
					return Results.BadRequest(new { error = "Title не должен быть пустым или длина его текста не должна превышать 200 символов" });

				task.Id = Guid.NewGuid();
				task.IsCompleted = false;
				task.Priority = Priority.Medium;
				task.CreatedAt = DateTimeOffset.UtcNow;
				task.CompletedAt = null;

				dbContext.Tasks.Add(task);
				await dbContext.SaveChangesAsync();
				return Results.Created($"/tasks/{task.Id}", task);
			});

			// Публикуем после SaveChanges — чтобы не отправить сообщение, если БД упала
			app.MapPut("/tasks/{id}/complete", async (Guid id, TaskDbContext dbContext, IRabbitMqService rabbit) =>
			{
				var task = await dbContext.Tasks.FindAsync(id);
				if (task == null) return Results.NotFound();
				if (task.IsCompleted) return Results.Conflict(new { error = "Задача уже выполнена" });

				task.IsCompleted = true;
				task.CompletedAt = DateTimeOffset.UtcNow;

				try { await dbContext.SaveChangesAsync(); }
				catch (DbUpdateConcurrencyException) { return Results.Conflict(new { error = "Задача была выполнена другим процессом" }); }

				await rabbit.PublishTaskCompletedAsync(task);
				return Results.Ok(task);
			});

			app.MapDelete("/tasks/{id}", async (Guid id, TaskDbContext dbContext) =>
			{
				var task = await dbContext.Tasks.FindAsync(id);
				if (task == null) return Results.NotFound();
				dbContext.Tasks.Remove(task);
				await dbContext.SaveChangesAsync();
				return Results.NoContent();
			});
			#endregion

			app.Run();
		}
	}
}

public partial class Program { }
