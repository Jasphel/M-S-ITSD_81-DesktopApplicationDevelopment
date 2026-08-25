namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; }
    public int StudentId { get; }
    public int EquipmentId { get; }
    public DateTime DateBorrowed { get; }
    public DateTime ExpectedReturnDate { get; }
    public BorrowingStatus Status { get; private set; }

    public Borrowing(
        int id,
        int studentId,
        int equipmentId,
        DateTime dateBorrowed,
        DateTime expectedReturnDate)
    {
        Id = id;
        StudentId = studentId;
        EquipmentId = equipmentId;
        DateBorrowed = dateBorrowed;
        ExpectedReturnDate = expectedReturnDate;
        Status = BorrowingStatus.Active;
    }

    public void MarkAsReturned()
    {
        Status = BorrowingStatus.Returned;
    }
}