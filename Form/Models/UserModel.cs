using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Form.Models
{
    public class QualificationModel
    {
        public int Qual_id { get; set; }
        public string Stream { get; set; }
        public IFormFile File { get; set; }
        public string Remark { get; set; }

        [ValidateNever]
        public string FilePath { get; set; }
    }

    public class UserModel
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public DateTime? Dob { get; set; }

       
        public string? Photo { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string Category { get; set; }

        public bool Status { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Photo is required")]
        public IFormFile? PhotoFile { get; set; }


        [ValidateNever]
        public List<QualificationModel> Qualifications { get; set; } = new();
    }
}
