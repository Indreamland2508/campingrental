// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BAOCAOWEBNANGCAO.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Đừng tiết lộ việc email có tồn tại hay không để bảo mật
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // --- GIAO DIỆN HTML EMAIL CHUẨN DOANH NGHIỆP ---
                string emailBody = $@"
                <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; background-color: #f4f7f5; padding: 50px 20px;'>
                    <div style='max-width: 550px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.05); text-align: center;'>
                        
                        <h1 style='color: #1a4d3a; font-size: 28px; font-weight: 800; margin-bottom: 5px; letter-spacing: 1px;'>CAMPING RENTAL</h1>
                        <p style='color: #888; font-size: 12px; text-transform: uppercase; letter-spacing: 2px; margin-bottom: 30px;'>Hệ thống quản trị nội bộ</p>
                        
                        <div style='background-color: #f8f9fa; border-radius: 50%; width: 80px; height: 80px; line-height: 80px; margin: 0 auto 20px auto;'>
                            <img src='https://cdn-icons-png.flaticon.com/512/6195/6195696.png' alt='Lock Icon' style='width: 40px; vertical-align: middle;' />
                        </div>

                        <h2 style='color: #333; font-size: 20px; margin-bottom: 15px;'>Yêu cầu đặt lại mật khẩu</h2>
                        
                        <p style='color: #555; font-size: 15px; line-height: 1.6; margin-bottom: 30px;'>
                            Chào bạn,<br><br>
                            Hệ thống nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn. Vui lòng nhấn vào nút bên dưới để tiến hành thiết lập mật khẩu mới an toàn hơn.
                        </p>

                        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='display: inline-block; background: linear-gradient(135deg, #198754 0%, #115c30 100%); color: #ffffff; text-decoration: none; padding: 15px 35px; border-radius: 10px; font-weight: bold; font-size: 16px; text-transform: uppercase; box-shadow: 0 4px 10px rgba(25,135,84,0.3);'>Đổi mật khẩu ngay</a>

                        <div style='margin-top: 40px; border-top: 1px solid #eee; padding-top: 20px;'>
                            <p style='color: #999; font-size: 13px; line-height: 1.5; margin: 0;'>
                                Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này. <br>Đường dẫn sẽ tự động hết hạn sau 2 giờ để đảm bảo bảo mật.
                            </p>
                        </div>
                    </div>
                </div>";

                // Bắn email đi
                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "[Camping Rental] Khôi phục mật khẩu tài khoản",
                    emailBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}