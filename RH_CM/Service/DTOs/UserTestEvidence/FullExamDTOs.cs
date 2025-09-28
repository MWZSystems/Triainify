using static RH_CM.ViewModels.ViewModels;

namespace RH_CM.Service.DTOs.UserTestEvidence
{
    public class FullExamDTOs
    {
        public string TestName { get; set; } = string.Empty;
        public double? Score { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public bool HasMaterial { get; set; }
        public int ControlNumber { get; set; }

        // IDs para continuar con el flujo
        public int NextCourseId { get; set; }
        public int NextLevelId { get; set; }

        // Lista de preguntas con opciones
        public List<DiagnosticQuestion> Questions { get; set; } = new();
    }

    public class DiagnosticQuestion
    {
        public string QuestionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }

        // Opciones seleccionables
        public List<DiagnosticOption> Options { get; set; } = new();

        // IDs auxiliares para mostrar al final
        public List<int> SelectedOptionIds { get; set; } = new();
        public List<int> CorrectOptionIds { get; set; } = new();
    }

    public class DiagnosticOption
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsCorrect { get; set; }
    }
}
