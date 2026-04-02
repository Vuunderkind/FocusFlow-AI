using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    public class AnthropicProvider : ILLMProvider
    {
        private readonly Func<string> _apiKeyGetter;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

        public ProviderType ProviderType => ProviderType.Anthropic;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKeyGetter());

        public AnthropicProvider(Func<string> apiKeyGetter) => _apiKeyGetter = apiKeyGetter;

        public async Task<AIResponse> SendAsync(
            string systemPrompt, List<ChatMessage> history, string userMessage,
            string modelApiName, float temperature = 0.7f, int maxTokens = 4096,
            CancellationToken ct = default)
        {
            var key = _apiKeyGetter();
            if (string.IsNullOrWhiteSpace(key))
                return Fail("Anthropic API ключ не настроен.");

            var msgs = new List<object>();
            foreach (var m in history)
                msgs.Add(new { role = m.Role, content = m.Content });
            msgs.Add(new { role = "user", content = userMessage });

            var body = new { model = modelApiName, max_tokens = maxTokens, temperature, system = systemPrompt, messages = msgs };
            var req  = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key",          key);
            req.Headers.Add("anthropic-version",  "2023-06-01");
            req.Content = new StringContent(JsonConvert.SerializeObject(body),
                System.Text.Encoding.UTF8, "application/json");

            try
            {
                var resp  = await _http.SendAsync(req, ct);
                var body2 = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var errMsg = JObject.Parse(body2)["error"]?["message"]?.ToString() ?? body2;
                    return Fail($"Anthropic ({(int)resp.StatusCode}): {errMsg}");
                }
                var j = JObject.Parse(body2);
                return new AIResponse
                {
                    Text         = j["content"]?[0]?["text"]?.ToString() ?? "",
                    InputTokens  = j["usage"]?["input_tokens"]?.Value<int>()  ?? 0,
                    OutputTokens = j["usage"]?["output_tokens"]?.Value<int>() ?? 0,
                    Model        = modelApiName,
                    Provider     = "Anthropic",
                    Success      = true,
                };
            }
            catch (OperationCanceledException) { return Fail("Запрос отменён."); }
            catch (Exception ex)               { return Fail($"Ошибка сети: {ex.Message}"); }
        }

        private static AIResponse Fail(string e) => new() { Success = false, Error = e };
    }
}
