using Microsoft.AspNetCore.Http;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace BAOCAOWEBNANGCAO // Đảm bảo đúng Namespace dự án của Châu
{
    public static class SessionHelper
    {
        // Hàm lưu Object vào Session (chuyển sang Json)
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Hàm lấy Object từ Session (giải mã từ Json)
        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }

        // Hàm chuyển đổi DateTime sang giờ Việt Nam
        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        {
            var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";

            var vietnamZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, vietnamZone);
        }
    }
}