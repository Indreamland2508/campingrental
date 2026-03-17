using Microsoft.AspNetCore.Http;
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
    }
}