namespace wallet.Services.Infrastructure
{
    public class SystemClock : IClock
    {
        private readonly TimeZoneInfo _businessTimeZone;

        public SystemClock(IConfiguration configuration)
        {
            var timeZoneId = configuration["Wallet:BusinessTimeZone"] ?? "Myanmar Standard Time";
            _businessTimeZone = FindTimeZone(timeZoneId);
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime BusinessToday => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _businessTimeZone).Date;

        public (DateTime StartUtc, DateTime EndUtc) BusinessDayUtcRange
        {
            get
            {
                var startLocal = BusinessToday;
                var endLocal = startLocal.AddDays(1);

                return (
                    TimeZoneInfo.ConvertTimeToUtc(startLocal, _businessTimeZone),
                    TimeZoneInfo.ConvertTimeToUtc(endLocal, _businessTimeZone));
            }
        }

        private static TimeZoneInfo FindTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Yangon");
            }
        }
    }
}
