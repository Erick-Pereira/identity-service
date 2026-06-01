using System.Collections.Generic;

public class ConformanceItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateOnly? CompletedAt { get; private set; }
    public bool IsCompleted => CompletedAt.HasValue;
    public Guid CondominioId { get; private set; }
    public string Status { get => _status; private set => _status = value; }
    private string _status;
    public Guid? UploadedByUserId { get; private set; }
    public DateOnly CreatedAt { get; private set; }

    public ConformanceItem()
    {
        Id = Guid.NewGuid();
        Status = "PENDING";
    }

    public void SetCompleted(Guid userId)
    {
        CompletedAt = DateOnly.FromDateTime(DateTime.Now);
        Status = "VERIFIED";
    }

    public static ConformanceItem Empty => new()
    {
        Id = Guid.NewGuid(), Title = "", Description = "",
        DueDate = DateOnly.MinValue,
        Status = "PENDING", CondominioId = default!
    };

    public override string ToString() => $"{Title} [{Status}]";
}
