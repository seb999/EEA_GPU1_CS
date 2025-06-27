using System.Collections.Generic;
using System.Timers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;

namespace EEA_GPU1_CS.Services
{
    public interface IChatService
    {
        IAsyncEnumerable<string> StreamChatGpt(List<ChatMessage> history);
         IAsyncEnumerable<string> StreamChat(List<ChatMessage> history);


        // void UploadContext(ChatGptContext context);

    }
}