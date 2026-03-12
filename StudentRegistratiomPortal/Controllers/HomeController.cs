using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentRegistrationPortal.Models;
using System.Diagnostics;

namespace StudentRegistrationPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;
        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            string query = @"Select * from StudentData";

            var users = new List<StudentModel>();
            using (var connection = new SqlConnection(_connectionString))
                users = connection.Query<StudentModel>(query).ToList();
            {
                return View(users);
            }



        }

      



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
