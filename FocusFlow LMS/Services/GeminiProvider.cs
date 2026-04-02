using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    public class GeminiProvider : ILLMProvider
    {
        private readonly Func<string> _apiKeyGetter;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

        public ProviderType ProviderType => ProviderType.Google;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKeyGetter());

        public GeminiProvider(Func<string> apiKeyGetter) => _apiKeyGetter = apiKeyGetter;

        public async Task<AIResponse> SendAsync(
            string systemPrompt, List<ChatMessage> history, string userMessage,
            string modelApiName, float temperature = 0.7f, int maxTokens = 4096,
            CancellationToken ct = default)
        {
            var key = _apiKeyGetter();
            if (string.IsNullOrWhiteSpace(key))
                return Fail("Google Gemini API ключ не настроен.");

            // Build contents array
            var contents = new List<object>();
            foreach (var m in history)
            {
                contents.Add(new
                {
                    role  = m.Role == "user" ? "user" : "model",
                    parts = new[] { new { text = m.Content } }
                });
            }
            contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

            var body = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents,
                generationConfig   = new { temperature, maxOutputTokens = maxTokens },
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelApiName}:generateContent?key={key}";
            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Content = new StringContent(JsonConvert.SerializeObject(body),
                System.Text.Encoding.UTF8, "application/json");

            try
            {
                var resp  = await _http.SendAsync(req, ct);
                var body2 = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var errMsg = JObject.Parse(body2)["error"]?["message"]?.ToString() ?? body2;
                    return Fail($"Gemini ({(int)resp.StatusCode}): {errMsg}");
                }
                var j    = JObject.Parse(body2);
                var text = j["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";
                var inT  = j["usageMetadata"]?["promptTokenCount"]?.Value<int>()    ?? 0;
                var outT = j["usageMetadata"]?["candidatesTokenCount"]?.Value<int>() ?? 0;
                return new AIResponse
                {
                    Text = text, InputTokens = inT, OutputTokens = outT,
                    Model = modelApiName, Provider = "Google", Success = true,
                };
            }
            catch (OperationCanceledException) { return Fail("Запрос отменён."); }
            catch (Exception ex)               { return Fail($"Ошибка сети: {ex.Message}"); }
        }

        private static AIResponse Fail(string e) => new() { Success = false, Error = e };
    }
}
