using Dapper;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentRegistrationPortal.Helpers;
using StudentRegistrationPortal.Models;

namespace StudentRegistrationPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // LIST STUDENTS
        public IActionResult Index(string searchTerm)
        {
            string query = @"
        SELECT * FROM StudentData
        WHERE
            (@search IS NULL OR LTRIM(RTRIM(@search)) = '')
            OR StudentName LIKE '%' + @search + '%'
            OR Student_Email LIKE '%' + @search + '%'
            OR Course LIKE '%' + @search + '%'";

            using var connection = new SqlConnection(_connectionString);
            var students = connection
                .Query<StudentModel>(query, new { search = searchTerm })
                .ToList();

            return View(students);
        }

        // ===================== CREATE STUDENT =====================
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(StudentModel sm)
        {
            if (!ModelState.IsValid)
            {
                return View(sm);
            }

            if (string.IsNullOrWhiteSpace(sm.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
                return View(sm);
            }
            sm.Password = PasswordHelper.HashPassword(sm.Password);

            if (sm.Photo != null && sm.Photo.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/studentPhotos");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string ext = Path.GetExtension(sm.Photo.FileName).ToLower();
                if (ext != ".jpg" && ext != ".png")
                {
                    ModelState.AddModelError("", "Only JPG and PNG allowed");
                    return View(sm);
                }
                string fileName = Guid.NewGuid() + ext;
                string path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                sm.Photo.CopyTo(stream);

                sm.PhotoPath = "/studentPhotos/" + fileName;
            }

            string query = @"
                INSERT INTO StudentData
                (StudentName, Student_Email, Password, Dob, Mobile,
                 FatherName, MotherName, Course, CGPA,
                 Semester, Admission_Year, PhotoPath)
                VALUES
                (@StudentName, @Student_Email, @Password, @Dob, @Mobile,
                 @FatherName, @MotherName, @Course, @CGPA,
                 @Semester, @Admission_Year, @PhotoPath)";

            using var con = new SqlConnection(_connectionString);
            int rows = con.Execute(query, sm);

            if (rows == 0)
            {
                ModelState.AddModelError("", "Insert failed");
                return View(sm);
            }

            return RedirectToAction("Index");
        }

        // ===================== UPDATE STUDENT =====================
        [HttpGet]
        public IActionResult Update(int id)
        {
            using var con = new SqlConnection(_connectionString);
            var student = con.QueryFirstOrDefault<StudentModel>(
                "SELECT * FROM StudentData Where id=@id", new { id });
            return View(student);


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(StudentModel sm)
        {
            if (sm.Photo != null && sm.Photo.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/studentPhotos");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(sm.Photo.FileName);
                using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                sm.Photo.CopyTo(fs);

                sm.PhotoPath = "/studentPhotos/" + fileName;
            }

            string query = @"
                UPDATE StudentData SET
                StudentName=@StudentName,
                Dob=@Dob,
                Mobile=@Mobile,
                FatherName=@FatherName,
                MotherName=@MotherName,
                Course=@Course,
                CGPA=@CGPA,
                Semester=@Semester,
                Admission_Year=@Admission_Year,
                PhotoPath=ISNULL(@PhotoPath, PhotoPath)
                WHERE Id=@id";


            using var con = new SqlConnection(_connectionString);
            con.Execute(query, sm);

            return RedirectToAction("Index");
        }



        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "StudentErp",
                new { area = "" }
            );
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            using var con = new SqlConnection(_connectionString);
            con.Execute("DELETE FROM StudentData WHERE Id=@id", new { id });
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult ImportExcel(IFormFile excelFile)
        {
            if (excelFile?.Length <= 0)
                return BadRequest("Excel file not selected");

            var students = new List<StudentModel>();

            using var stream = new MemoryStream();
            excelFile.CopyTo(stream);

            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets[0];

            for (int row = 2; row <= sheet.Dimension.Rows; row++)
            {
                if (string.IsNullOrWhiteSpace(sheet.Cells[row, 1].Text))
                    continue;

                students.Add(new StudentModel
                {
                    StudentName = sheet.Cells[row, 1].Text,
                    Student_Email = sheet.Cells[row, 2].Text,
                    FatherName = sheet.Cells[row, 3].Text,
                    MotherName = sheet.Cells[row, 4].Text,
                    Course = sheet.Cells[row, 5].Text,
                    CGPA = float.TryParse(sheet.Cells[row, 6].Text, out var cgpa) ? cgpa : null,
                    Semester = int.TryParse(sheet.Cells[row, 7].Text, out var sem) ? sem : null,
                    Admission_Year = int.TryParse(sheet.Cells[row, 8].Text, out var year) ? year : null,
                    Mobile = sheet.Cells[row, 9].Text,
                    Dob = sheet.Cells[row, 10].Text,
                    Password = PasswordHelper.HashPassword(sheet.Cells[row, 11].Text)

                });
            }

            InsertStudents(students);
            return RedirectToAction("Index");
        }

        private void InsertStudents(List<StudentModel> students)
        {
            const string sql = @"IF NOT EXISTS (
        SELECT 1 
        FROM StudentData 
        WHERE Student_Email = @Student_Email
        )
        BEGIN
            INSERT INTO StudentData
            (StudentName, Student_Email, FatherName, MotherName, Course,
             CGPA, Semester, Admission_Year, Mobile, Dob, Password, PhotoPath)
            VALUES
            (@StudentName, @Student_Email, @FatherName, @MotherName, @Course,
             @CGPA, @Semester, @Admission_Year, @Mobile, @Dob, @Password, NULL)
        END
        ";

            using var con = new SqlConnection(_connectionString);
            con.Execute(sql, students);
        }





    }
}
