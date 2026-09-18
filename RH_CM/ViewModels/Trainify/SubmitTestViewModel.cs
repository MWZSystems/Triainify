namespace RH_CM.ViewModels
{
    public class SubmitTestViewModel
    {
        public int FkTest { get; set; }
        public string TestName { get; set; } = string.Empty;
        public List<SubmitQuestionViewModel> Questions { get; set; } = new();
        public int NextCourseId { get; set; }   // to open the material after the diagnostic
        public int NextLevelId { get; set; }
        public int? CourseAssignmentId { get; set; }   // NEW
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
        public bool IsSelected { get; set; } // will be used to capture the user's answer
    }

}
