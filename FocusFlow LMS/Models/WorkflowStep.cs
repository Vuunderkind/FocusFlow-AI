namespace FocusFlow_LMS.Models
{
    public class WorkflowStep
    {
        public int    Id          { get; set; }
        public int    WorkflowId  { get; set; }
        public int    StepOrder   { get; set; }
        public string AgentId     { get; set; } = "default";
        public string StepName    { get; set; } = string.Empty;
        public string Instruction { get; set; } = string.Empty;
    }

    public class Workflow
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Emoji       { get; set; } = "⚡";
        public bool   IsActive    { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<WorkflowStep> Steps { get; set; } = new();
    }

    public class WorkflowRunResult
    {
        public bool   Success     { get; set; }
        public string Output      { get; set; } = string.Empty;
        public List<StepResult> StepResults { get; set; } = new();
        public string? Error      { get; set; }
        public DateTime RunAt     { get; set; } = DateTime.Now;
    }

    public class StepResult
    {
        public string StepName  { get; set; } = string.Empty;
        public string Input     { get; set; } = string.Empty;
        public string Output    { get; set; } = string.Empty;
        public bool   Success   { get; set; }
        public string? Error    { get; set; }
    }
}
