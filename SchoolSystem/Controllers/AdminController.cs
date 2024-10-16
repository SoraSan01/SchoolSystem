using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Models;
using SchoolSystem.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using SchoolSystem.ViewModels;
using Xceed.Words.NET;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using SchoolSystem.Services;
using System.Text.RegularExpressions;

namespace SchoolSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _dbContext; // Declare _dbContext here
        private readonly SmtpSettings _smtpSettings; // Declare _smtpSettings here
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext dbContext, IOptions<SmtpSettings> smtpSettings, ILogger<AdminController> logger)
        {
            _dbContext = dbContext;
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Request()
        {
            var requests = await _dbContext.RequestTORAndDiplomas
                               .Where(r => r.Status == "Pending" || r.Status == "Approved")
                               .Select(r => new RequestTORAndDiplomaAdminViewModel
                               {
                                   Id = r.Id,
                                   StudentId = r.StudentId,
                                   StudentName = r.StudentName,
                                   StudentEmail = r.StudentEmail,
                                   RequestType = r.RequestType,
                                   Reason = r.Reason,
                                   RequestDate = r.RequestDate,
                                   Status = r.Status,
                                   Remarks = r.Remarks,
                                   DocumentPath = r.DocumentPath
                               }).ToListAsync();

            return View(requests); // Make sure requests is passed to the view
        }

        public async Task<IActionResult> ApproveRequest(int requestId)
        {
            var request = await _dbContext.RequestTORAndDiplomas.FindAsync(requestId);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Request not found.";
                return RedirectToAction("Request");
            }

            request.Status = "Approved";

            try
            {
                await _dbContext.SaveChangesAsync();
                string documentPath = GenerateDocument(request);

                // Send the document via email
                await SendDocumentViaEmailAsync(request, documentPath);

                TempData["SuccessMessage"] = "Request approved and document sent via email.";
            }
            catch (Exception ex)
            {
                // Log exception
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Request");
        }

        [HttpPost]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var request = await _dbContext.RequestTORAndDiplomas.FindAsync(requestId);
            if (request != null)
            {
                request.Status = "Rejected";
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Request");
        }

        private string GenerateDocument(RequestTORAndDiploma request)
        {
            // Define the path for the template file
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", $"{request.RequestType}.docx");
            Console.WriteLine($"Checking template path: {templatePath}"); // Debugging output

            // Check if the template file exists
            if (!System.IO.File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found: {templatePath}");
            }

            // Define the path for the generated document
            string documentPath = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedDocuments", $"{request.StudentId}_{request.RequestType}.docx");

            // Ensure the directory exists
            if (!Directory.Exists(Path.GetDirectoryName(documentPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(documentPath));
            }

            // Copy the template to the new file location
            System.IO.File.Copy(templatePath, documentPath, true);

            // Customize the document with the student's name
            using (var document = DocX.Load(documentPath))
            {
                // Replace placeholders with actual values (you can define placeholders like {{StudentName}} in your template)
                document.ReplaceText("{{StudentName}}", request.StudentName);
                // Save the changes
                document.Save();
            }

            return documentPath;
        }


        public async Task SendDocumentViaEmailAsync(RequestTORAndDiploma request, string documentPath)
        {
            if (string.IsNullOrEmpty(request.StudentEmail))
            {
                throw new ArgumentException("Student email is required", nameof(request.StudentEmail));
            }

            if (!Regex.IsMatch(request.StudentEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                throw new ArgumentException("Invalid email format", nameof(request.StudentEmail));
            }

            var mailMessage = CreateMailMessage(request, documentPath);
            await SendEmailAsync(mailMessage);
        }
        private MailMessage CreateMailMessage(RequestTORAndDiploma request, string documentPath)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Username),
                To = { request.StudentEmail },
                Subject = $"Your {request.RequestType} Request has been Approved",
                Body = $"Dear {request.StudentName},\n\n" +
                       $"We are pleased to inform you that your request for {request.RequestType} has been approved. " +
                       $"Please find the attached document containing your {request.RequestType}.\n\n" +
                       $"Best regards,\nSchool Administration",
                Attachments = { new Attachment(documentPath) }
            };

            return mailMessage;

        }
        public async Task SendEmailAsync(MailMessage mailMessage)
        {
            try
            {
                using (var smtp = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port))
                {
                    smtp.EnableSsl = _smtpSettings.EnableSsl;
                    smtp.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);

                    await smtp.SendMailAsync(mailMessage);
                    Console.WriteLine("Email sent successfully.");
                }
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP error: {smtpEx.Message}");
                // Consider logging the error
                throw; // Optionally re-throw to handle it upstream
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Consider logging the error
                throw; // Optionally re-throw to handle it upstream
            }
        }
    }
}
