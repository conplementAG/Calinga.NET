using System;

namespace Calinga.NET
{
    public interface IDateTimeService
    {
        DateTime GetCurrentDateTime();
    }

    public class DateTimeService : IDateTimeService
    {
        public DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }
    }
}