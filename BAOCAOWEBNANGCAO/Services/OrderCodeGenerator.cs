using System.Security.Cryptography;

namespace BAOCAOWEBNANGCAO.Services
{
    public static class OrderCodeGenerator
    {
        public static string Create() => $"CR-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}";
    }
}
