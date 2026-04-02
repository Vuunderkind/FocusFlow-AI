using FocusFlow_LMS.Data;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    public class WorkflowService
    {
        private readonly AIService       _ai;
        private readonly AgentRepository _agents;

        public WorkflowService(AIService ai, AgentRepository agents)
        {
            _ai     = ai;
            _agents = agents;
        }

        public async Task<WorkflowRunResult> RunAsync(
            Workflow          workflow,
            string            initialInput,
            IProgress<string>? progress = null,
            CancellationToken ct        = default)
        {
            var result = new WorkflowRunResult { RunAt = DateTime.Now };

            if (workflow.Steps.Count == 0)
                return Fail("Воркфлоу не содержит шагов.");

            string currentInput = initialInput;

            for (int i = 0; i < workflow.Steps.Count; i++)
            {
                var step  = workflow.Steps[i];
                var agent = _agents.GetById(step.AgentId);

                progress?.Report($"Шаг {i+1}/{workflow.Steps.Count}: {step.StepName}...");

                if (ct.IsCancellationRequested)
                    return Fail("Воркфлоу отменён пользователем.");

                var systemPrompt = agent?.SystemPrompt ??
                    "Ты — умный AI ассистент. Обрабатывай задачу точно и профессионально.";

                var instruction = string.IsNullOrWhiteSpace(step.Instruction)
                    ? currentInput
                    : $"{step.Instruction}\n\n---\nВходные данные:\n{currentInput}";

                var resp = await _ai.SendAsync(
                    systemPrompt : systemPrompt,
                    history      : [],
                    userMessage  : instruction,
                    modelId      : agent?.Model,
                    temperature  : agent?.Temperature ?? 0.7f,
                    maxTokens    : agent?.MaxTokens   ?? 4096,
                    ct           : ct);

                var stepResult = new StepResult
                {
                    StepName = step.StepName,
                    Input    = currentInput,
                    Success  = resp.Success,
                    Output   = resp.Success ? resp.Text : string.Empty,
                    Error    = resp.Error,
                };

                result.StepResults.Add(stepResult);

                if (!resp.Success)
                {
                    result.Error   = $"Шаг {i+1} «{step.StepName}» завершился с ошибкой: {resp.Error}";
                    result.Success = false;
                    return result;
                }

                currentInput = resp.Text;
            }

            result.Output  = currentInput;
            result.Success = true;
            progress?.Report("Воркфлоу завершён успешно!");
            return result;
        }

        private static WorkflowRunResult Fail(string error) =>
            new() { Success = false, Error = error };
    }
}
