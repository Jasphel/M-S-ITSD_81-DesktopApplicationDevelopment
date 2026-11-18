namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; private set; }
    public string StudentNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactInfo { get; private set; }
    public bool IsAllowedToBorrow { get; private set; }

    private Student() { } // Required by EF Core

    public Student(
        int id,
        string studentNumber,
        string name,
        string? contactInfo = null,
        bool isAllowedToBorrow = true)
    {
        Id = id;
        StudentNumber = NormalizeNumber(studentNumber);
        Name = name.Trim();
        ContactInfo = NormalizeOptional(contactInfo);
        IsAllowedToBorrow = isAllowedToBorrow;
    }

    public void UpdateDetails(string studentNumber, string name, string? contactInfo)
    {
        StudentNumber = NormalizeNumber(studentNumber);
        Name = name.Trim();
        ContactInfo = NormalizeOptional(contactInfo);
    }

    public void SetEligibility(bool isAllowed)
    {
        IsAllowedToBorrow = isAllowed;
    }

    public static string NormalizeNumber(string number) => number.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}