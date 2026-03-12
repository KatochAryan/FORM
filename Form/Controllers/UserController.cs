using Microsoft.AspNetCore.Mvc;
using Dapper;
using Microsoft.Data.SqlClient;
using Form.Models;

namespace Form.Controllers
{
    public class UserController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public UserController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _env = env;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(UserModel um)
        {
            um.Qualifications ??= new List<QualificationModel>();

            // Filter and validate qualifications
            var validQualifications = um.Qualifications
                .Where(q => !string.IsNullOrWhiteSpace(q.Stream) ||
                            !string.IsNullOrWhiteSpace(q.Remark) ||
                            (q.File != null && q.File.Length > 0))
                .ToList();

            if (validQualifications.Count == 0)
            {
                ModelState.AddModelError("Qualifications[0].Stream", "At least one qualification is required");
            }

            for (int i = 0; i < validQualifications.Count; i++)
            {
                var q = validQualifications[i];
                if (string.IsNullOrWhiteSpace(q.Stream))
                    ModelState.AddModelError($"Qualifications[{i}].Stream", "Stream is required");
                if (string.IsNullOrWhiteSpace(q.Remark))
                    ModelState.AddModelError($"Qualifications[{i}].Remark", "Remark is required");
                if (q.File == null || q.File.Length == 0)
                    ModelState.AddModelError($"Qualifications[{i}].File", "Document is required");
            }

            if (um.PhotoFile == null || um.PhotoFile.Length == 0)
            {
                ModelState.AddModelError(nameof(um.PhotoFile), "Photo is required");
            }

            if (!ModelState.IsValid)
            {
                um.Qualifications = validQualifications;
                return View(um);
            }

            // Save photo
            var photoFileName = $"{Guid.NewGuid()}{Path.GetExtension(um.PhotoFile!.FileName)}";
            var photoPath = Path.Combine(_env.WebRootPath, "images", photoFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
            using (var stream = new FileStream(photoPath, FileMode.Create))
                um.PhotoFile.CopyTo(stream);
            um.Photo = "/images/" + photoFileName;

            // Save qualification files
            foreach (var q in validQualifications)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(q.File!.FileName)}";
                var filePath = Path.Combine(_env.WebRootPath, "files", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                using var fs = new FileStream(filePath, FileMode.Create);
                q.File.CopyTo(fs);
                q.FilePath = "/files/" + fileName;
            }

            // Database operations
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var studentId = connection.QuerySingle<int>(@"
                    INSERT INTO STUDENT_INFO (FirstName, LastName, Dob, Photo, Gender, Category, Status)
                    OUTPUT INSERTED.USER_ID
                    VALUES (@FirstName, @LastName, @Dob, @Photo, @Gender, @Category, @Status)",
                    um, transaction);

                foreach (var q in validQualifications)
                {
                    connection.Execute(@"
                        INSERT INTO STUDENT_QUALIFICATION (USER_ID, STREAM, FILE_PATH, REMARK)
                        VALUES (@UserId, @Stream, @FilePath, @Remark)",
                        new { UserId = studentId, q.Stream, q.FilePath, q.Remark }, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            TempData["SuccessMessage"] = "Student saved successfully!";
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var user = connection.QuerySingleOrDefault<EditModel>(
                "SELECT * FROM STUDENT_INFO WHERE USER_ID = @Id", new { Id = id });

            if (user == null) return NotFound();

            var qualifications = connection.Query<QualificationEditModel>(@"
                SELECT QUAL_ID AS Qual_id, STREAM, FILE_PATH AS FilePath, REMARK
                FROM STUDENT_QUALIFICATION WHERE USER_ID = @Id", new { Id = id }).ToList();

            foreach (var q in qualifications)
            {
                if (!string.IsNullOrWhiteSpace(q.FilePath))
                    q.FilePath = "/" + q.FilePath.TrimStart('/');
            }

            user.Qualifications = qualifications;
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(EditModel um)
        {
            if (!ModelState.IsValid) return View(um);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Update photo if provided
                if (um.PhotoFile != null && um.PhotoFile.Length > 0)
                {
                    var photoName = Guid.NewGuid() + Path.GetExtension(um.PhotoFile.FileName);
                    var photoPath = Path.Combine(_env.WebRootPath, "images", photoName);
                    Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
                    using var ps = new FileStream(photoPath, FileMode.Create);
                    um.PhotoFile.CopyTo(ps);
                    um.Photo = "/images/" + photoName;
                }

                // Update student info
                connection.Execute(@"
                    UPDATE STUDENT_INFO SET
                        FirstName = @FirstName, LastName = @LastName, Dob = @Dob,
                        Gender = @Gender, Category = @Category, Status = @Status,
                        Photo = COALESCE(@Photo, Photo)
                    WHERE USER_ID = @Id", um, transaction);

                // Handle deletions
                var deletedQualifications = um.Qualifications.Where(q => q.DeleteQualification && q.Qual_id > 0).ToList();
                foreach (var dq in deletedQualifications)
                {
                    if (!string.IsNullOrEmpty(dq.FilePath))
                    {
                        var fullPath = Path.Combine(_env.WebRootPath, dq.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);
                    }
                    connection.Execute("DELETE FROM STUDENT_QUALIFICATION WHERE QUAL_ID = @QualId",
                        new { QualId = dq.Qual_id }, transaction);
                }

                // Remove deleted from list
                um.Qualifications = um.Qualifications.Where(q => !q.DeleteQualification).ToList();

                // Handle inserts/updates
                foreach (var q in um.Qualifications)
                {
                    string filePath = q.FilePath;

                    if (q.DeleteDocument && !string.IsNullOrEmpty(q.FilePath))
                    {
                        var fullPath = Path.Combine(_env.WebRootPath, q.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                            System.IO.File.Delete(fullPath);
                        filePath = null;
                    }

                    if (q.File != null && q.File.Length > 0)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(q.File.FileName);
                        var savePath = Path.Combine(_env.WebRootPath, "files", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                        using var fs = new FileStream(savePath, FileMode.Create);
                        q.File.CopyTo(fs);
                        filePath = "/files/" + fileName;
                    }

                    if (q.Qual_id == 0)
                    {
                        connection.Execute(@"
                            INSERT INTO STUDENT_QUALIFICATION (USER_ID, STREAM, REMARK, FILE_PATH)
                            VALUES (@UserId, @Stream, @Remark, @FilePath)",
                            new { UserId = um.Id, q.Stream, q.Remark, FilePath = filePath }, transaction);
                    }
                    else
                    {
                        connection.Execute(@"
                            UPDATE STUDENT_QUALIFICATION SET
                                STREAM = @Stream, REMARK = @Remark, FILE_PATH = @FilePath
                            WHERE QUAL_ID = @QualId",
                            new { q.Stream, q.Remark, FilePath = filePath, QualId = q.Qual_id }, transaction);
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            TempData["SuccessMessage"] = "Student updated successfully!";
            return RedirectToAction(nameof(Edit), new { id = um.Id });
        }

        [HttpGet]
        public IActionResult Index()
        {
            using var connection = new SqlConnection(_connectionString);

            var users = connection.Query<UserListViewModel>(@"
        SELECT 
            USER_ID AS Id,
            FirstName,
            LastName,
            Dob,
            Gender,
            Category,
            Status,
            Photo
        FROM STUDENT_INFO
        ORDER BY USER_ID DESC
    ").ToList();

            return View(users);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete qualification files
                var files = connection.Query<string>(
                    "SELECT FILE_PATH FROM STUDENT_QUALIFICATION WHERE USER_ID = @Id",
                    new { Id = id }, transaction);

                foreach (var file in files)
                {
                    var path = Path.Combine(_env.WebRootPath, file.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                connection.Execute("DELETE FROM STUDENT_QUALIFICATION WHERE USER_ID = @Id", new { Id = id }, transaction);
                connection.Execute("DELETE FROM STUDENT_INFO WHERE USER_ID = @Id", new { Id = id }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            TempData["SuccessMessage"] = "Student deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetDocuments(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            var docs = connection.Query<StudentDocumentViewModel>(@"
        SELECT 
            STREAM AS Stream,
            FILE_PATH AS FilePath,
            REMARK AS Remark
        FROM STUDENT_QUALIFICATION
        WHERE USER_ID = @Id
    ", new { Id = id }).ToList();

            return PartialView("_DocumentListPartial", docs);
        }






    }
}