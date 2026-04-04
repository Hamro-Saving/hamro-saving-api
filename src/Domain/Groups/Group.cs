using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Groups;

public sealed class Group : Entity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal MemberInterestRate { get; private set; } = 10m;
    public decimal NonMemberInterestRate { get; private set; } = 18m;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Group() { }

    public static Group Create(
        string name,
        string code,
        string? description = null,
        decimal memberRate = 10m,
        decimal nonMemberRate = 18m)
    {
        var group = new Group
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Code = code.ToUpperInvariant(),
            Description = description,
            IsActive = true,
            MemberInterestRate = memberRate,
            NonMemberInterestRate = nonMemberRate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        group.Raise(new GroupCreatedDomainEvent(group.Id));
        return group;
    }

    public void Update(string name, string? description, decimal memberRate, decimal nonMemberRate)
    {
        Name = name;
        Description = description;
        MemberInterestRate = memberRate;
        NonMemberInterestRate = nonMemberRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
