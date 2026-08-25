namespace EquipmentBorrowing.Domain;

public class Student
{
    public int Id { get; }
    public string Name { get; }
    public bool IsAllowedToBorrow { get; }

    public Student(int id, string name, bool isAllowedToBorrow)
    {
        Id = id;
        Name = name;
        IsAllowedToBorrow = isAllowedToBorrow;
    }
}