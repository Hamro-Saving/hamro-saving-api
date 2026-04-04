using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Members;

public sealed class NonMember : Entity
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public Guid GroupId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private NonMember() { }

    public static NonMember Create(
        string fullName,
        string? email,
        string? phone,
        string? address,
        Guid groupId)
    {
        return new NonMember
        {
            Id = Guid.CreateVersion7(),
            FullName = fullName,
            Email = email,
            Phone = phone,
            Address = address,
            GroupId = groupId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string fullName, string? email, string? phone, string? address)
    {
        FullName = fullName;
        Email = email;
        Phone = phone;
        Address = address;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
