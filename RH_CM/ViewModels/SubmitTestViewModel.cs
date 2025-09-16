namespace RH_CM.ViewModels
{
    public class SubmitTestViewModel
    {
        public int FkTest { get; set; }
        public string TestName { get; set; } = string.Empty;
        public List<SubmitQuestionViewModel> Questions { get; set; } = new();
        public int NextCourseId { get; set; }   // para abrir material luego del diagnóstico
        public int NextLevelId { get; set; }
    }

    public class SubmitQuestionViewModel
    {
        public int FkQuestion { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public bool IsMultiple { get; set; } // true = checkbox, false = radio
        public List<SubmitOptionViewModel> Options { get; set; } = new();
    }

    public class SubmitOptionViewModel
    {
        public int FkOption { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsSelected { get; set; } // se usará para capturar respuesta del usuario
    }

}
