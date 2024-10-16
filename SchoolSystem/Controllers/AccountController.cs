using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SchoolSystem.Models;
using SchoolSystem.Services; // Assuming EmailService is in this namespace
using SchoolSystem.ViewModels;
using System;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace SchoolSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AccountController(SignInManager<Users> signInManager, UserManager<Users> userManager, IEmailSender emailSender, IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._emailSender = emailSender;
            this._configuration = configuration; // Use this if you need to access configuration
        }

        private string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public IActionResult VerifyOTP(string email)
        {
            var model = new VerifyOTPViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public IActionResult VerifyOTP(VerifyOTPViewModel model)
        {
            var storedOTP = TempData["OTP"]?.ToString();
            var expiryTime = TempData["OTPExpiry"] != null ? (DateTime)TempData["OTPExpiry"] : DateTime.MinValue;

            if (storedOTP == null || expiryTime < DateTime.Now)
            {
                ModelState.AddModelError("", "OTP has expired. Please request a new one.");
                return View(model);
            }

            if (model.OTPCode == storedOTP)
            {
                return RedirectToAction("ChangePassword", new { username = model.Email });
            }
            else
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                return View(model);
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            using var smtpClient = new SmtpClient(smtpSettings["Server"])
            {
                Port = int.Parse(smtpSettings["Port"]),
                Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["Username"]),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully from ASP.NET!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email from ASP.NET: {ex.Message}");
            }
        }


        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Admin");
                }
                ModelState.AddModelError("", "Email or Password is incorrect");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Users
                {
                    FullName = model.Name,
                    Email = model.Email,
                    UserName = model.Email,
                };

                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Email not found. Please try again.");
                    return View(model);
                }

                // Generate and store OTP
                var otpCode = GenerateOTP();
                TempData["OTP"] = otpCode; // Store the OTP in TempData
                TempData["OTPExpiry"] = DateTime.Now.AddMinutes(10); // Set the expiry time for the OTP

                // Prepare the email message
                string message = $"Your OTP code is {otpCode}. It will expire in 10 minutes.";
                try
                {
                    // Use _emailSender instead of emailService
                    await _emailSender.SendEmailAsync(user.Email, "Password Reset OTP", message); // Send the email with OTP
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to send email: {ex.Message}");
                    return View(model);
                }

                return RedirectToAction("VerifyOTP", new { email = model.Email });
            }

            return View(model);
        }

        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        if (result.Succeeded)
                        {
                            return RedirectToAction("Login", "Account");
                        }
                        AddErrors(result);
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email Not Found");
                }
            }

            ModelState.AddModelError("", "Something went wrong. Try again.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
    }
}
