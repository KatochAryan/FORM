using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentRegistrationPortal.Helpers;
using StudentRegistrationPortal.Models;

namespace StudentRegistrationPortal.Controllers
{
    public class StudentErpController : Controller
    {
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        public StudentErpController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // ===================== CREATE STUDENT =====================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(StudentModel sm)
        {
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
            con.Execute(query, sm);

            return RedirectToAction("Login");
        }

        // ===================== LOGIN =====================
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") == "Admin")
                return RedirectToAction("Dashboard", "Admin");

            if (HttpContext.Session.GetString("UserRole") == "Student")
                return RedirectToAction("Update", "StudentErp");

            TempData.Clear();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string Email, string Password, string Role)
        {
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(Role))
            {
                ViewBag.Error = "Email and Password are required";
                return View();
            }

            using var con = new SqlConnection(_connectionString);

            // -------- STUDENT LOGIN --------
            if (Role == "Student")
            {
                var student = con.QueryFirstOrDefault<StudentModel>(
                    "SELECT * FROM StudentData WHERE Student_Email=@email",
                    new { email = Email });

                if (student != null &&
                    PasswordHelper.VerifyPassword(student.Password, Password))
                {
                    HttpContext.Session.SetString("StudentEmail", student.Student_Email);
                    HttpContext.Session.SetString("UserRole", "Student");

                    TempData["Success"] = "Student Login Successful!";
                    return RedirectToAction("Update", "StudentErp");
                }
            }

            // -------- ADMIN LOGIN --------
            else if (Role == "Admin")
            {
                var admin = con.QueryFirstOrDefault<AdminModel>(
                    "SELECT * FROM AdminData WHERE Admin_Email=@email AND IsActive=1",
                    new { email = Email });

                if (admin != null &&
                    PasswordHelper.VerifyPassword(admin.Password, Password))
                {
                    HttpContext.Session.SetString("AdminEmail", admin.Admin_Email);
                    HttpContext.Session.SetString("UserRole", "Admin");

                    TempData["Success"] = "Admin Login Successful!";
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
            }

            ViewBag.Error = "Invalid email or password";
            return View();
        }

        // ====================Logout============
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "StudentErp", new { area = "" });
        }


        // ===================== UPDATE STUDENT =====================
        [HttpGet]
        public IActionResult Update()
        {
            if (HttpContext.Session.GetString("UserRole") != "Student")
                return RedirectToAction("Login");

            string email = HttpContext.Session.GetString("StudentEmail");

            using var con = new SqlConnection(_connectionString);
            var student = con.QueryFirstOrDefault<StudentModel>(
                "SELECT * FROM StudentData WHERE Student_Email=@email",
                new { email });

            return student == null ? NotFound() : View(student);
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
                WHERE Id=@Id";

            using var con = new SqlConnection(_connectionString);
            con.Execute(query, sm);

            return RedirectToAction("Update");
        }

        // ===================== DELETE =====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            using var con = new SqlConnection(_connectionString);
            con.Execute("DELETE FROM StudentData WHERE Id=@id", new { id });
            return RedirectToAction("Index", "Home");
        }

        // ===================== FORGOT PASSWORD =====================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            using var con = new SqlConnection(_connectionString);
            var student = con.QueryFirstOrDefault<StudentModel>(
                "SELECT * FROM StudentData WHERE Student_Email=@email",
                new { email });

            if (student == null)
            {
                ViewBag.Message = "Email not registered";
                return View();
            }

            int otp = new Random().Next(100000, 999999);
            HttpContext.Session.SetString("OTP", otp.ToString());
            HttpContext.Session.SetString("ResetEmail", email);
            HttpContext.Session.SetString("OtpExpiry", DateTime.Now.AddMinutes(5).ToString());
            HttpContext.Session.SetInt32("OtpAttempts", 0);

            SendOtpEmail(email, otp);
            return RedirectToAction("VerifyOtp");
        }

        // ===================== VERIFY OTP =====================
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            ViewBag.OtpExpiry = HttpContext.Session.GetString("OtpExpiry");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            int attempts = HttpContext.Session.GetInt32("OtpAttempts") ?? 0;
            attempts++;

            if (attempts > 3)
            {
                HttpContext.Session.Clear();
                TempData["Error"] = "Too many attempts. OTP expired.";
                return RedirectToAction("ForgotPassword");
            }

            HttpContext.Session.SetInt32("OtpAttempts", attempts);

            var sessionOtp = HttpContext.Session.GetString("OTP");
            var expiryString = HttpContext.Session.GetString("OtpExpiry");

            if (expiryString == null)
            {
                ViewBag.Error = "OTP expired. Please resend.";
                return View();
            }

            DateTime expiry = DateTime.Parse(expiryString);

            if (otp == sessionOtp && DateTime.Now <= expiry)
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Invalid OTP";
            ViewBag.OtpExpiry = expiryString; // IMPORTANT: keep timer alive
            return View();
        }


        // ===================== RESET PASSWORD =====================
        [HttpGet]
        public IActionResult ResetPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string newPassword)
        {
            string email = HttpContext.Session.GetString("ResetEmail");
            if (email == null) return RedirectToAction("ForgotPassword");

            string hash = PasswordHelper.HashPassword(newPassword);

            using var con = new SqlConnection(_connectionString);
            con.Execute(
                "UPDATE StudentData SET Password=@pwd WHERE Student_Email=@email",
                new { pwd = hash, email });

            HttpContext.Session.Clear();
            TempData["Success"] = "Password reset successful";
            return RedirectToAction("Login");
        }

        // ===================== EMAIL =====================
        private void SendOtpEmail(string toEmail, int otp)
        {
            var from = _configuration["EmailSettings:FromEmail"];
            var pass = _configuration["EmailSettings:AppPassword"];

            var mail = new System.Net.Mail.MailMessage(from, toEmail)
            {
                Subject = "Password Reset OTP",
                Body = $"Your OTP is {otp}. Valid for 5 minutes."
            };

            var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new System.Net.NetworkCredential(from, pass),
                EnableSsl = true
            };

            smtp.Send(mail);
        }


        //[HttpGet]
        //public IActionResult GenerateHash()
        //{
        //    return Content(                                                       *If you need to change the password
        //        StudentRegistrationPortal.Helpers.PasswordHelper                  uncomment this  add run update query
        //            .HashPassword("Admin@123")
        //    );
        //}

    }
}
