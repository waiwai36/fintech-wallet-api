namespace wallet.Services.Infrastructure
{
    public interface IClock
    {
        DateTime UtcNow { get; }
        DateTime BusinessToday { get; }
        (DateTime StartUtc, DateTime EndUtc) BusinessDayUtcRange { get; }
    }
}
