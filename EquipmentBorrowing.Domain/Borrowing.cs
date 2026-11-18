namespace EquipmentBorrowing.Domain;

public class Borrowing
{
    public int Id { get; private set; }
    public int StudentId { get; private set; }
    public int EquipmentId { get; private set; }
    public DateTime DateBorrowed { get; private set; }
    public DateTime ExpectedReturnDate { get; private set; }
    public DateTime? ActualReturnDate { get; private set; }
    public BorrowingStatus Status { get; private set; }

    private Borrowing() { } // Required by EF Core

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

    public void MarkAsReturned(DateTime? returnDate = null)
    {
        Status = BorrowingStatus.Returned;
        ActualReturnDate = returnDate ?? DateTime.Today;
    }
}