using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentRegistrationPortal.Models;

namespace StudentRegistrationPortal.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly string _connectionString;

        public ChatbotController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Chatbot
        public IActionResult Chatbot()
        {
            return View();
        }

        // POST: Chatbot/SendMessage
        [HttpPost]
        public IActionResult SendMessage(string userMessage)
        {
            string botReply = GetRuleBasedReply(userMessage);

            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"INSERT INTO StudentChat (UserMessage, BotReply)
                                 VALUES (@UserMessage, @BotReply)";

                connection.Execute(query, new
                {
                    UserMessage = userMessage,
                    BotReply = botReply
                });
            }

            return Json(new { reply = botReply });
        }

        // ✅ RULE-BASED CHATBOT (FREE)
        private string GetRuleBasedReply(string message)
        {
            message = message.ToLower().Trim();

            if (message.Contains("hi") || message.Contains("hello"))
                return "Hello! 👋 How can I help you today?";

            if (message.Contains("fees") || message.Contains("fee"))
                return "You can check your fee details in the Fees section.";

            if (message.Contains("attendance"))
                return "Your attendance details are available in the Attendance module.";

            if (message.Contains("result") || message.Contains("marks"))
                return "Results will be published after exams are evaluated.";

            if (message.Contains("login") || message.Contains("password"))
                return "Please use the Forgot Password option to reset your password.";

            if (message.Contains("profile"))
                return "You can update your profile from the Update Profile section.";

            if (message.Contains("contact") || message.Contains("admin"))
                return "Please contact the admin office during working hours.";

            return "Sorry 😔 I didn’t understand that. Please contact admin.";
        }
    }
}
