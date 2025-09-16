namespace RH_CM.ViewModels
{
    public class ViewModels
    {
        public class DiagnosticResultViewModel
        {
            public int FkTest { get; set; }
            public string TestName { get; set; } = string.Empty;
            public int TotalQuestions { get; set; }
            public int CorrectCount { get; set; }
            public int Score { get; set; } // 0..100
            public List<DiagnosticQuestionResultViewModel> Questions { get; set; } = new();
        }

        public class DiagnosticQuestionResultViewModel
        {
            public int FkQuestion { get; set; }
            public string QuestionText { get; set; } = string.Empty;
            public bool IsMultiple { get; set; }
            public List<int> SelectedOptionIds { get; set; } = new();
            public List<int> CorrectOptionIds { get; set; } = new();
            public bool IsCorrect { get; set; }
            public List<DiagnosticOptionResultViewModel> Options { get; set; } = new();
        }

        public class DiagnosticOptionResultViewModel
        {
            public int FkOption { get; set; }
            public string OptionText { get; set; } = string.Empty;
            public bool IsSelected { get; set; }
            public bool IsCorrect { get; set; }
        }
    }
}
