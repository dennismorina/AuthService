namespace AuthService.Domain.Entities;

public sealed class Role
{
    private Role()
    {
    }

    private Role(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Role Create(string name)
        => new(Guid.NewGuid(), name);
}
