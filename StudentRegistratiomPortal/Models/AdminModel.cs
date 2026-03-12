namespace StudentRegistrationPortal.Models
{
    public class AdminModel
    {
        public int Admin_Id { get; set; }
        public string Admin_Name { get; set; }
        public string Admin_Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
