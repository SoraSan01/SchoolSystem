using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;
using System.Diagnostics;

namespace SchoolSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult RequestTOROrDiploma()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RequestTOROrDiploma(RequestTORAndDiplomaViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Map the view model to a new instance of RequestTORAndDiploma
                var request = new RequestTORAndDiploma
                {
                    StudentId = viewModel.StudentId,
                    StudentName = viewModel.StudentName,
                    StudentEmail = viewModel.StudentEmail,
                    RequestType = viewModel.RequestType,
                    Reason = viewModel.Reason,
                };

                // Save the request to the database
                _dbContext.RequestTORAndDiplomas.Add(request);
                _dbContext.SaveChanges();

                // Return a JSON response that includes a success message
                return Json(new
                {
                    success = true,
                    message = "Your request has been sent successfully."
                });
            }

            // Log the validation errors
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            _logger.LogError("Model validation failed: {Errors}", string.Join(", ", errors));

            return Json(new
            {
                success = false,
                message = "An error occurred while sending your request."
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
