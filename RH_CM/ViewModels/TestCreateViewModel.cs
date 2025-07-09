namespace RH_CM.ViewModels
{
    public class TestCreateViewModel
    {
        public int FkCourse { get; set; }
        public int CourseLevel { get; set; }
        public string TestName { get; set; } = null!;
        public List<QuestionCreateViewModel> Questions { get; set; } = new();
    }

    public class QuestionCreateViewModel
    {
        public string QuestionText { get; set; } = null!;
        public List<OptionCreateViewModel> Options { get; set; } = new();
        public int CorrectOptionIndex { get; set; }
    }

    public class OptionCreateViewModel
    {
        public string OptionText { get; set; } = null!;
    }
}
