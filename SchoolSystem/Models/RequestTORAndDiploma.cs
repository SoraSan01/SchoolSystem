using System;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models
{
    public class RequestTORAndDiploma
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Student ID is required.")]
        public string StudentId { get; set; }

        [Required(ErrorMessage = "Student Name is required.")]
        public string StudentName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string StudentEmail { get; set; }

        [Required(ErrorMessage = "Request Type is required.")]
        public string RequestType { get; set; } // TOR or Diploma

        [Required(ErrorMessage = "Request Date is required.")]
        public DateTime RequestDate { get; set; } = DateTime.Now; // Automatically set the request date to the current date and time

        public string? Status { get; set; } = "Pending";// Pending, Approved, Rejected 

        [Required(ErrorMessage = "Reason is required.")]
        public string Reason { get; set; }

        public string? Remarks { get; set; }

        public string? DocumentPath { get; set; } // Path to the generated TOR or diploma document

        public DateTime? ApprovedDate { get; set; }

        public DateTime? RejectedDate { get; set; }
    }
}
