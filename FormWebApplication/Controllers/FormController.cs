using Microsoft.AspNetCore.Mvc;
using Dapper;
using FormWebApplication.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FormWebApplication.Controllers
{

    public class FormController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public FormController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _env = env;
        }

        [HttpGet]
        public IActionResult Create() => View();



        [HttpPost]
        public IActionResult Create(PersonalModel pm)
        {
            ResponceModel res = new ResponceModel();
            // checks is  pm is null and  stores the document model data on  Details in list form
            pm.Details ??= new List<DocumentsModel>();

            // remove invalid and empty spaces row in Document Details
            var ValidDetails = pm.Details.
                Where(d => !string.IsNullOrEmpty(d.Stream) ||
               !string.IsNullOrEmpty(d.Remarks) ||
               (d.File != null && d.File.Length > 0))
                .ToList();
            // check is my valid Detail model is correct if not then shown message in backend
            if(ValidDetails.Count == 0)
            {
                res.MESSEGE='At least one document detail is required',
                    res.STATUS=false;

            }
            //it checks my every document details is correct or filled or if not filled it shows the error message in backend
            for (int i = 0; i < ValidDetails.Count; i++)
            {
                var vd = ValidDetails[i];
                if (string.IsNullOrEmpty(vd.Stream))
                {
                    ModelState.AddModelError($"Details[{i}].Stream", "Stream is required.");
                }
                if (string.IsNullOrEmpty(vd.Remarks))
                {
                    ModelState.AddModelError($"Details[{i}].Remarks", "Remark is Required");
                }
                if (vd.File == null || vd.File.Length == 0)
                {
                    ModelState.AddModelError($"Details[{i}].File", "Document is required");
                }
            }


                // this checks is my photofile is empty or not if empty then it shows the error message in backend
            if (pm.PhotoFile == null || pm.PhotoFile.Length == 0)
            {
                    ModelState.AddModelError(nameof(pm.PhotoFile),"Photo file is Required");
            }
                // after checking all this this check that out model is now correct
            if (!ModelState.IsValid)
            {
                    pm.Details = ValidDetails;
                    return View(pm);
            }

                // save photo easy
                //Generate PhotoFile Name random with extension
            var PhotoFileName = $"{Guid.NewGuid()}{Path.GetExtension(pm.PhotoFile!.FileName)}";

            var PhotoPath = Path.Combine(_env.WebRootPath, "IMAGES", PhotoFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(PhotoPath)!);
            using (var stream = new FileStream(PhotoPath, FileMode.Create))
            pm.PhotoFile.CopyTo(stream);
            pm.Photo = "/IMAGES" + PhotoFileName;
            //Each document save process
            foreach (var d in ValidDetails)
            {

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(d.File!.FileName)}";
                var filepath = Path.Combine(_env.WebRootPath, "FILES", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);                    
                using var fs = new FileStream(filepath, FileMode.Create); 
                d.File.CopyTo(fs);                 
                d.FilePath = "/FILES" + fileName;
            }

                //now  open a sql connection
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(); //A transaction is a group of database actions that must all succeed together.
            try
            {
                var personId = connection.QuerySingle<int>(@"InsertPersonalDetails", new
                {

                    pm.FirstName,
                    pm.LastName,
                    pm.Dob,
                    pm.Photo,
                    pm.Gender,
                    pm.Category,
                    pm.Status
                },
                transaction,
                commandType: CommandType.StoredProcedure);
                foreach (var d in ValidDetails)
                {
                    connection.Execute(@"InsertDocumentDetails", new
                    {
                            USER_ID = personId,
                            d.Stream,
                            d.FilePath,
                            d.Remarks
                    },
                    transaction,
                    commandType: CommandType.StoredProcedure);
                }
                transaction.Commit();


            }
            catch
                {
                    transaction.Rollback();
                    throw;
                }


            TempData["SuccessMessage"] = res;

            res.REDIRECT_URL = 'form/create';

            return json(res);

        }
        
    }
}
  