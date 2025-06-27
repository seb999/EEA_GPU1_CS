using System.Runtime.CompilerServices;
using System.Text.Json;
using EEA_GPU1_CS.Class;
using EEA_GPU1_CS.Services;
using Microsoft.AspNetCore.Mvc;
// using OpenAI.ObjectModels.RequestModels;

namespace EEA_GPU1_CS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatGptService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatGptService, ILogger<ChatController> logger)
        {
            _logger = logger;
            _chatGptService = chatGptService;
        }

        /// <summary>
        /// Use this method to stream chat responses from the ChatGPT API or Antonio's machine.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("StreamChat")]
        public async Task StreamChat([FromBody] ChatGptMessage request)
        {
            Response.ContentType = "text/event-stream"; // SSE for streaming

            // for interacting with ChatGPT API 
            // await foreach (var chunk in _chatGptService.StreamChatGpt(request.History))
            // {
            //     var json = JsonSerializer.Serialize(new { content = chunk });
            //     await Response.WriteAsync($"data:{json}\n");
            //     await Response.Body.FlushAsync();
            // }

            // for interacting with Antonio's machine
            await foreach (var chunk in _chatGptService.StreamChat(request.History))
            {
                var json = JsonSerializer.Serialize(new { content = chunk });
                await Response.WriteAsync($"data:{json}\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}