namespace StudentRegistrationPortal.Models
{
    public class ChatModel
    {
        public int ChatId { get; set; }
        public string UserMessage { get; set; }
        public string BotReply { get; set; }
        public DateTime CreatedOn { get; set; }
    }

}
