using OpenAI.ObjectModels.RequestModels;

namespace EEA_GPU1_CS.Class
{
    public class ChatGptMessage
    {
        public List<ChatMessage> History { get; set; } = new();
    }
}
