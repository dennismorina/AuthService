namespace AuthService.Domain.Entities;

public sealed class Permission
{
    private Permission()
    {
    }

    private Permission(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Permission Create(string name)
        => new(Guid.NewGuid(), name);
}
