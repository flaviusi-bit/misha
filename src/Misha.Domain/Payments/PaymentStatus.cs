namespace Misha.Domain.Payments;

public enum PaymentStatus
{
    Pending = 0,
    RequiresAction = 1,
    Paid = 2,
    Failed = 3,
    Cancelled = 4
}
