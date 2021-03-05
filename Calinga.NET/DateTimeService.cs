using System;

namespace Calinga.NET
{
    public interface IDateTimeService
    {
        DateTime GetCurrentDateTime();

        DateTime ConvertToDateTime(object? date);

        DateTime GetExpirationDate(uint? expiration);
    }

    public class DateTimeService : IDateTimeService
    {
        public DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }

        public DateTime ConvertToDateTime(object? date)
        {
            return Convert.ToDateTime(date);
        }

        public DateTime GetExpirationDate(uint? expiration)
        {
            return expiration == null || expiration == 0 ? DateTime.MaxValue : DateTime.Now.AddSeconds(expiration.Value);
        }
    }
}