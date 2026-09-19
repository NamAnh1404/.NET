using System;

namespace HRMDesktop.Services
{
    public static class SystemTimeService
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        public static DateTime Now
        {
            get { return DateTimeOffset.UtcNow.ToOffset(VietnamOffset).DateTime; }
        }

        public static DateTime Today
        {
            get { return Now.Date; }
        }
    }
}
