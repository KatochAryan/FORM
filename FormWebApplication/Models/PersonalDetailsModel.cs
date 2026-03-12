using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


using System.ComponentModel.DataAnnotations.Schema;
namespace FormWebApplication.Models

{

    public class ResponceModel
    {
        
        public string MESSEGE { get; set; }      
        public bool STATUS { get; set; }
        public string REDIRECT_URL { get; set; }

    }
    public class DocumentsModel
    {
        public int Qual_id { get; set; }
        public string Stream { get; set; }
        
        public IFormFile? File { get; set; }
        public string Remarks { get; set; }
        [ValidateNever]
        public string FilePath { get; set; }
    }
    public class PersonalModel
    {
        public int User_Id { get; set; }

        [Required(ErrorMessage = " First Name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = " Last Name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = " D.O.B is required")]
        public DateTime? Dob { get; set; }

        public string? Photo { get; set; }


        [Required(ErrorMessage = " Gender  is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = " Category is required")]
        public string Category { get; set; }

        public bool Status { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Photo is required")]
        public IFormFile? PhotoFile { get;set; }

        [ValidateNever]
        public List<DocumentsModel> Details { get; set; }
    }
}
