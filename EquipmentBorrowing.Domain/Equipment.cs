namespace EquipmentBorrowing.Domain;

public class Equipment
{
    public int Id { get; private set; }
    public string Identifier { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAvailable { get; private set; }

    private Equipment() { } // Required by EF Core

    public Equipment(
        int id,
        string identifier,
        string name,
        string? description = null,
        bool isAvailable = true)
    {
        Id = id;
        Identifier = NormalizeIdentifier(identifier);
        Name = name.Trim();
        Description = NormalizeOptional(description);
        IsAvailable = isAvailable;
    }

    public void UpdateDetails(string identifier, string name, string? description)
    {
        Identifier = NormalizeIdentifier(identifier);
        Name = name.Trim();
        Description = NormalizeOptional(description);
    }

    public void MarkAsBorrowed() => IsAvailable = false;

    public void MarkAsAvailable() => IsAvailable = true;

    public static string NormalizeIdentifier(string identifier) => identifier.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}