public class TodoRepository
{
    private readonly List<Todo> _todos = new();

    public List<Todo> GetAll() => _todos;

    public Todo? GetById(int id) => _todos.SingleOrDefault(t => t.Id == id);

    public Todo Add(Todo todo)
    {
        var newId = _todos.Any() ? _todos.Max(t => t.Id) + 1 : 1;
        Todo todoIdAdded = new(newId, todo.Name, todo.DueDate, todo.IsCompleted);
        _todos.Add(todoIdAdded);
        return todoIdAdded;
    }

    public void Delete(int id) => _todos.RemoveAll(t => t.Id == id);

    public Todo? Update(int id, Todo updatedTodo)
    {
        var index = _todos.FindIndex(t => t.Id == id);
        _todos[index] = updatedTodo with { Id = _todos[index].Id};
        if (_todos[index].IsCompleted)
        {
            Delete(_todos[index].Id);
            return null;
        }
        return _todos[index];
    }
    
}