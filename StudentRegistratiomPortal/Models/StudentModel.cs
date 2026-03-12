using System.ComponentModel.DataAnnotations.Schema;

namespace StudentRegistrationPortal.Models
{
    public class StudentModel
    {
        public int id { get; set; }
        public string StudentName { get; set; }
        public string Student_Email { get; set; }
        public string Dob { get; set; }
        public string Password { get; set; }
        public string FatherName { get; set; }
        public string MotherName { get; set; }
        public string Course { get; set; }
        public float?  CGPA { get; set; }
        public int? Semester { get; set; }
        public string Mobile { get; set; }
        public int? Admission_Year { get; set; }

        [NotMapped]
        public IFormFile? Photo { get; set; }

        public string? PhotoPath { get; set; }
    }
}
