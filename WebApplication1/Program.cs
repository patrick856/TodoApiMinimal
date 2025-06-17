using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new TodoRepository());

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));

var todolist = new List<Todo>();

app.MapGet("/todos", (TodoRepository repo) => repo.GetAll());

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound> (int id, TodoRepository repo) =>
{
    var targetTodo = repo.GetById(id);
    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo todo, TodoRepository repo) =>
{
    var created = repo.Add(todo);
    return TypedResults.Created("/todos/{id}", created);
})
.AddEndpointFilter(async (context, next) =>
{
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(taskArgument.Name))
        errors.Add(nameof(Todo.Name), ["Name cannot be null"]);
    if (taskArgument.DueDate < DateTime.Now)
        errors.Add(nameof(Todo.DueDate), ["Due date cannot be in the past"]);
    if (taskArgument.IsCompleted)
        errors.Add(nameof(Todo.IsCompleted), ["Task cannot be already completed"]);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id, TodoRepository repo) =>
{
    repo.Delete(id);
    return TypedResults.NoContent();
})
.AddEndpointFilter(async (context, next) =>
{
    var repo = context.HttpContext.RequestServices.GetRequiredService<TodoRepository>();
    var id = context.GetArgument<int>(0);
    var errors = new Dictionary<string, string[]>();
    if (repo.GetById(id) == null)
        errors.Add(nameof(Todo), ["Task does not exist"]);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    return await next(context);
});

app.MapPut("/todos/{id}", (int id, Todo update, TodoRepository repo) =>
{
    var updatedTodo = repo.Update(id, update);
    if (updatedTodo is null)
        return Results.Ok("Task Completed");
    else
        return Results.Created("/todos/{id}", updatedTodo);
    
})
.AddEndpointFilter(async (context, next) =>
{
    var repo = context.HttpContext.RequestServices.GetRequiredService<TodoRepository>();
    var id = context.GetArgument<int>(0);
    var taskArgument = context.GetArgument<Todo>(1);
    var errors = new Dictionary<string, string[]>();

    if (repo.GetAll().FindIndex(t => t.Id == id) == -1)
        errors.Add(nameof(Todo), ["Task does not exist"]);
    if (string.IsNullOrWhiteSpace(taskArgument.Name))
        errors.Add(nameof(Todo.Name), ["Name cannot be null"]);
    if (taskArgument.DueDate < DateTime.Now)
        errors.Add(nameof(Todo.DueDate), ["Due date cannot be in the past"]);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);
    
    return await next(context);
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate,bool IsCompleted);