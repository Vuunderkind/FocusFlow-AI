using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    // ════════════════════════════════════════════════════════════
    //  OrchestrationService
    //
    //  AUTO   → Router picks single best model → sends request
    //  FUSION → 2 models answer in parallel → synthesizer merges
    //  MANUAL → Use specified model directly
    // ════════════════════════════════════════════════════════════
    public class OrchestrationResult
    {
        public bool             Success      { get; set; }
        public string           Text         { get; set; } = string.Empty;
        public string?          Error        { get; set; }
        public RouteDecision    Decision     { get; set; } = null!;
        public List<AIResponse> SubResponses { get; set; } = new();
        public int              TotalTokens  { get; set; }
        public string           StatusInfo   { get; set; } = string.Empty;
    }

    public class OrchestrationService
    {
        private readonly ProviderRegistry _registry;
        private readonly AIRouter         _router;
        private readonly AppConfig        _config;

        // Synthesis prompt: merges two AI answers into one perfect answer
        private const string SynthesisPrompt = """
            Ты — синтезатор ответов. Тебе даны два ответа от разных AI моделей на один вопрос пользователя.
            Твоя задача: создать ОДИН идеальный ответ, объединив лучшее из обоих.

            Правила синтеза:
            - Возьми сильные стороны каждого ответа
            - Убери дубликаты и противоречия
            - Сохрани форматирование Markdown если оно есть
            - Ответ должен быть лучше любого из исходных
            - Не упоминай, что ты объединял ответы — просто дай лучший ответ
            """;

        public OrchestrationService(ProviderRegistry registry, AIRouter router, AppConfig config)
        {
            _registry = registry;
            _router   = router;
            _config   = config;
        }

        public async Task<OrchestrationResult> RunAsync(
            string             systemPrompt,
            List<ChatMessage>  history,
            string             userMessage,
            OrchestrationMode  mode,
            ModelInfo?         manualModel  = null,
            float              temperature  = 0.7f,
            int                maxTokens    = 4096,
            IProgress<string>? progress     = null,
            CancellationToken  ct           = default)
        {
            var decision = _router.Route(userMessage, mode);

            // ── MANUAL ───────────────────────────────────────────
            if (mode == OrchestrationMode.Manual)
            {
                if (manualModel == null)
                    return Fail(decision, "Выберите модель в ручном режиме.");

                progress?.Report($"Отправка в {manualModel.Emoji} {manualModel.DisplayName}...");
                decision.Models.Clear();
                decision.Models.Add(manualModel);

                var resp = await _registry.SendAsync(manualModel, systemPrompt, history,
                    userMessage, temperature, maxTokens, ct);

                return BuildResult(decision, resp);
            }

            // ── AUTO ─────────────────────────────────────────────
            if (mode == OrchestrationMode.Auto)
            {
                if (!decision.Models.Any())
                    return Fail(decision, decision.Reasoning);

                var model = decision.Models[0];
                progress?.Report($"{model.Emoji} {model.DisplayName} обрабатывает запрос...");

                var resp = await _registry.SendAsync(model, systemPrompt, history,
                    userMessage, temperature, maxTokens, ct);

                var result        = BuildResult(decision, resp);
                result.StatusInfo = decision.Reasoning;
                return result;
            }

            // ── FUSION ───────────────────────────────────────────
            if (mode == OrchestrationMode.Fusion)
            {
                var models = decision.Models;
                if (!models.Any())
                    return Fail(decision, "Нет доступных моделей для FUSION. Добавьте API ключи.");

                if (models.Count == 1)
                {
                    // Only one provider configured — fall back to AUTO
                    progress?.Report($"Только один провайдер. Используем {models[0].Emoji} {models[0].DisplayName}...");
                    var single = await _registry.SendAsync(models[0], systemPrompt, history,
                        userMessage, temperature, maxTokens, ct);
                    return BuildResult(decision, single);
                }

                // Send to 2 models in parallel
                progress?.Report($"FUSION: отправка в {models[0].Emoji} {models[0].DisplayName} и {models[1].Emoji} {models[1].DisplayName} одновременно...");

                var tasks = models.Take(2).Select(m =>
                    _registry.SendAsync(m, systemPrompt, history, userMessage, temperature, maxTokens, ct)
                ).ToList();

                var responses = await Task.WhenAll(tasks);

                var successes = responses.Where(r => r.Success).ToList();
                if (!successes.Any())
                {
                    var err = string.Join("; ", responses.Select(r => r.Error));
                    return Fail(decision, $"Все модели вернули ошибку: {err}");
                }

                if (successes.Count == 1)
                {
                    // One failed — return the successful one
                    return BuildResult(decision, successes[0], responses.ToList());
                }

                // Both succeeded → synthesize
                progress?.Report("Синтез лучшего ответа из двух моделей...");

                var synthText = $"""
                    Вопрос пользователя: {userMessage}

                    === Ответ модели 1 ({models[0].DisplayName}) ===
                    {successes[0].Text}

                    === Ответ модели 2 ({models[1].DisplayName}) ===
                    {successes[1].Text}
                    """;

                // Use cheapest/fastest available model for synthesis
                var synthModel = _registry.GetAvailableModels()
                    .Where(m => m.IsFast || m.CostPer1K < 0.002f)
                    .OrderBy(m => m.CostPer1K)
                    .FirstOrDefault() ?? models[0];

                var synthResp = await _registry.SendAsync(
                    synthModel, SynthesisPrompt, [],
                    synthText, 0.3f, maxTokens, ct);

                var finalText = synthResp.Success ? synthResp.Text : successes[0].Text;
                var result    = new OrchestrationResult
                {
                    Success      = true,
                    Text         = finalText,
                    Decision     = decision,
                    SubResponses = responses.ToList(),
                    TotalTokens  = responses.Sum(r => r.OutputTokens) + (synthResp.Success ? synthResp.OutputTokens : 0),
                    StatusInfo   = $"FUSION: {string.Join(" + ", models.Take(2).Select(m => m.DisplayName))} → синтез ({synthModel.DisplayName})",
                };
                return result;
            }

            return Fail(decision, "Неизвестный режим оркестрации.");
        }

        private static OrchestrationResult BuildResult(RouteDecision d, AIResponse resp,
            List<AIResponse>? subs = null) => new()
        {
            Success      = resp.Success,
            Text         = resp.Success ? resp.Text : "",
            Error        = resp.Error,
            Decision     = d,
            SubResponses = subs ?? new List<AIResponse> { resp },
            TotalTokens  = resp.OutputTokens,
            StatusInfo   = d.Reasoning,
        };

        private static OrchestrationResult Fail(RouteDecision d, string error) => new()
        {
            Success = false, Error = error, Decision = d,
        };
    }
}
