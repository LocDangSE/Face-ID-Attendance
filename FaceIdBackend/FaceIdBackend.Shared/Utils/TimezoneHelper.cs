using System;

namespace FaceIdBackend.Shared.Utils
{
    /// <summary>
    /// Timezone utilities for UTC+7 (Indochina Time / Bangkok Time)
    /// </summary>
    public static class TimezoneHelper
    {
        // UTC+7 timezone
        private static readonly TimeZoneInfo IndochinaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // UTC+7

        /// <summary>
        /// Get current date/time in UTC+7
        /// </summary>
        public static DateTime GetNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndochinaTimeZone);
        }

        /// <summary>
        /// Convert UTC to UTC+7
        /// </summary>
        public static DateTime ToLocalTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, IndochinaTimeZone);
        }

        /// <summary>
        /// Convert UTC+7 to UTC
        /// </summary>
        public static DateTime ToUtc(DateTime localDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, IndochinaTimeZone);
        }

        /// <summary>
        /// Get UTC+7 DateTime for database storage (stored as UTC)
        /// </summary>
        public static DateTime GetUtcNowForStorage()
        {
            // Store UTC in database, but represent current ICT time
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Timezone info for display
        /// </summary>
        public static class Info
        {
            public const string Name = "SE Asia Standard Time";
            public const string DisplayName = "Indochina Time (ICT)";
            public const string Abbreviation = "ICT";
            public const int OffsetHours = 7;
        }
    }
}
