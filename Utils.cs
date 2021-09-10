using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoshinoKiller
{
    internal static class Utils
    {
        private static DateTime dateTimeStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);

        // 时间戳转为C#格式时间
        public static DateTime ToDateTime(this long timeStamp)
        {
            return dateTimeStart.Add(new TimeSpan(10000 * timeStamp));
        }

        // DateTime时间格式转换为Unix时间戳格式
        public static long ToTimestamp(this DateTime time)
        {
            return (long)(time - dateTimeStart).TotalMilliseconds;
        }
    }
}
