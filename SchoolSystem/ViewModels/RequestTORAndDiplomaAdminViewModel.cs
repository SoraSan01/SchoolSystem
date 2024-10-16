namespace SchoolSystem.ViewModels
{
    public class RequestTORAndDiplomaAdminViewModel
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string RequestType { get; set; }
        public string Reason { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string DocumentPath { get; set; }
    }
}
