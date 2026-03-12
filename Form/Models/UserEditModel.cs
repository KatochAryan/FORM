using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Form.Models
{
    public class QualificationEditModel
    {
        public int Qual_id { get; set; }
        public string? Stream { get; set; }
        public IFormFile? File { get; set; }
        public string? Remark { get; set; }
        public bool DeleteQualification { get; set; }

        public bool DeleteDocument { get; set; }


        [ValidateNever]
        public string FilePath { get; set; }
    }

    public class EditModel
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

       
        public string LastName { get; set; }

        public DateTime Dob { get; set; }

        public string? Photo { get; set; }

        public string Gender { get; set; }

      
        public string Category { get; set; }

        public bool Status { get; set; }

     
        [NotMapped]
        public IFormFile? PhotoFile { get; set; }

        [ValidateNever]
        public List<QualificationEditModel> Qualifications { get; set; } = new();
    }

}
