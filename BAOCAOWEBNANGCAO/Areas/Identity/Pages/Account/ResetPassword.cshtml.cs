// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Thêm thư viện này để gọi IEmailSender
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BAOCAOWEBNANGCAO.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        // BƯỚC 1: Khai báo anh bưu tá ở đây
        private readonly IEmailSender _emailSender;

        // BƯỚC 2: Bơm anh bưu tá vào constructor
        public ResetPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
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

            [Required]
            [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }

            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Phải có mã xác nhận để đổi mật khẩu.");
            }
            else
            {
                Input = new InputModel
                {
                    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
                };
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Bảo mật: Không tiết lộ email có tồn tại hay không
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            // Thực hiện đổi mật khẩu (Chỉ khai báo 'var result' 1 lần duy nhất)
            var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);

            if (result.Succeeded)
            {
                // BƯỚC 3: Gửi email BÁO ĐỘNG ĐỎ
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "🚨 [CẢNH BÁO BẢO MẬT] Mật khẩu đã được thay đổi",
                    $@"<div style='font-family: Arial, sans-serif; border: 2px solid #dc3545; border-radius: 8px; padding: 25px; max-width: 500px; margin: 0 auto; background-color: #fffafb;'>
                         <h3 style='color: #dc3545; text-align: center; border-bottom: 1px solid #f5c6cb; padding-bottom: 15px;'>MẬT KHẨU VỪA BỊ THAY ĐỔI!</h3>
                         <p>Hệ thống Camping Rental ghi nhận tài khoản <b>{user.Email}</b> vừa thực hiện đổi mật khẩu thành công.</p>
                         <div style='background-color: #f8d7da; color: #721c24; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                            <b>CẢNH BÁO:</b> Nếu bạn <b>KHÔNG</b> thực hiện thao tác này, tài khoản của bạn đang bị xâm phạm trái phép! Vui lòng báo ngay cho Quản trị viên (Admin) để khóa hệ thống.
                         </div>
                       </div>");

                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}