using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EEA_GPU1_CS.Services
{
    public class ChatService : IChatService
    {
        private ILogger _logger;
        private OpenAIService _openAIService;
        private readonly string _apiKey;
        const string baseUrlLocal = "https://llmgw.eea.europa.eu/v1/chat/completions";
        const string modelLocal = "Inhouse-LLM/Mistral-Small-3.1-24B-Instruct-2503";

        public ChatService(ILogger<ChatService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"];
            _openAIService = new OpenAIService(new OpenAiOptions { ApiKey = "YOUR CHATGPT API IF NEEDED" });
        }

        public async IAsyncEnumerable<string> StreamChat(List<ChatMessage> chatList)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            if (chatList == null)
            {
                chatList = new List<ChatMessage>();
            }

            // Ensure first message is always the system message
            var systemMessage = ChatMessage.FromSystem(BuildSystemPrompt());

            if (chatList.Count > 0 && chatList[0].Role == OpenAI.ObjectModels.StaticValues.ChatMessageRoles.System)
            {
                chatList[0] = systemMessage; // replace old system prompt
            }
            else
            {
                chatList.Insert(0, systemMessage); //s insert if missing
            }

            //Convert ChatMessage list using openAI class to anonimous messages for httpRequest to Antonio machine
            var messagesAntonio = chatList.Select(msg => new { role = msg.Role, content = msg.Content }).ToArray();

            var payload = new
            {
                model = modelLocal,
                messages = messagesAntonio,
                stream = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await client.PostAsync(baseUrlLocal, content);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            
             while (!reader.EndOfStream)
            {
                  var line = await reader.ReadLineAsync();
                  if (string.IsNullOrWhiteSpace(line)) continue;
                  if (!line.StartsWith("data: ")) continue;

                  var jsonData = line["data: ".Length..];
                  if (jsonData == "[DONE]") break;


                  using var doc = JsonDocument.Parse(jsonData);

                  if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                      choices.GetArrayLength() > 0 &&
                      choices[0].TryGetProperty("delta", out var delta) &&
                      delta.TryGetProperty("content", out var contentElement))
                  {
                        var contentPart = contentElement.GetString();
                        if (!string.IsNullOrEmpty(contentPart))
                        {
                            // await Task.Delay(15); //delay to simulate streaming
                            yield return contentPart;
                        }
                  }

            }
        }
        
        public async IAsyncEnumerable<string> StreamChatGpt(List<ChatMessage> chatList)
        {
            if (chatList == null)
            {
                chatList = new List<ChatMessage>();
            }

             // Ensure first message is always the system message
            var systemMessage = ChatMessage.FromSystem(BuildSystemPrompt());

            if (chatList.Count > 0 && chatList[0].Role == OpenAI.ObjectModels.StaticValues.ChatMessageRoles.System)
            {
                chatList[0] = systemMessage; // replace old system prompt
            }
            else
            {
                chatList.Insert(0, systemMessage); // insert if missing
            }

            var response = _openAIService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
            {
                Model = "gpt-4o",
                Temperature = 0.7f,
                MaxTokens = 3000,
                TopP = 0.95f,
                Messages = chatList,
                Stream = true
            });

            string assistantReply = "";

            await foreach (var result in response)
            {
                if (!result.Successful)
                {
                    _logger.LogError($"ChatGPT call failed: {result.HttpStatusCode}");
                }
                else
                {
                    var chunk = result.Choices[0].Message.Content;
                    assistantReply += chunk;
                    yield return chunk;
                }
            }
        }

        private string BuildSystemPrompt()
        {

            const string basePrompt =
            "You are an assistant that is an expert in pyhton langauge and help users fixing and coding in this programming language. ";

            return basePrompt;
        }
    }
}