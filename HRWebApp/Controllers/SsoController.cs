using System;
using System.Text;
using System.Security.Cryptography;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Script.Serialization;

namespace HRWebApp.Controllers
{
    public class SsoController : Controller
    {
        // KHÓA BÍ MẬT: Bắt buộc phải giống hệt khóa bên Java
        private const string SECRET_KEY = "MySuperSecretKeyForSsoCourse2024";

        [AllowAnonymous]
        public ActionResult Consume(string token)
        {
            try
            {
                // Nếu không có token, đẩy về trang đăng nhập của HR
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction("Index", "Login");
                }

                // 1. Tách Token làm 2 phần: Payload và Signature
                string[] parts = token.Split('.');
                if (parts.Length != 2) return Content("Lỗi: Định dạng Token không hợp lệ.");

                string payloadB64 = parts[0];
                string signatureIn = parts[1];

                // 2. Xác thực Chữ ký (Anti-tampering)
                string expectedSignature = CreateHMAC(payloadB64, SECRET_KEY);

                if (expectedSignature != signatureIn)
                {
                    return Content("Lỗi: Chữ ký không hợp lệ (Token có thể đã bị chỉnh sửa).");
                }

                // 3. Giải mã Payload để đọc thông tin
                string payloadJson = DecodeBase64Url(payloadB64);

                var serializer = new JavaScriptSerializer();
                var payload = serializer.Deserialize<dynamic>(payloadJson);

                string username = payload["sub"];
                long exp = Convert.ToInt64(payload["exp"]);

                // 4. Kiểm tra thời gian hết hạn (Chống Replay Attack)
                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                if (now > exp)
                {
                    return Content("Lỗi: Token SSO đã quá hạn 60 giây. Vui lòng thử lại.");
                }

                // 5. Xác thực thành công -> Cấp Session và Cookie đăng nhập cho HR
                FormsAuthentication.SetAuthCookie(username, false);
                Session["Username"] = username;

                // 6. Đẩy vào thẳng trang HR
                return RedirectToAction("Index", "Admin");

            }
            catch (Exception ex)
            {
                // In ra lỗi để debug nếu có vấn đề
                return Content("Lỗi hệ thống SSO: " + ex.Message);
            }
        }

        // Thuật toán tạo chữ ký số HMAC-SHA256 (Tương đương bên Java)
        private string CreateHMAC(string message, string secret)
        {
            var encoding = new UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);

            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return EncodeBase64Url(hashmessage);
            }
        }

        // Tiện ích: Encode Base64 an toàn cho URL
        private string EncodeBase64Url(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        // Tiện ích: Decode Base64 an toàn cho URL
        private string DecodeBase64Url(string input)
        {
            string base64 = input.Replace("-", "+").Replace("_", "/");
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}