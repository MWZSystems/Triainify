using RH_CM.Models;

namespace RH_CM.ViewModels
{
    public class TestCreateViewModel
    {
        public int FkCourse { get; set; }
        public int FkRequiredCourseLevels { get; set; }
        public string TestName { get; set; } = null!;
        public List<QuestionCreateViewModel> Questions { get; set; } = new();

        public List<CtCourse> AvailableCourses { get; set; } = new();
        public List<CtLevelcourse> AvailableLevels { get; set; } = new();
        public List<CtOptiontype> AvailableOptionTypes { get; set; } = new(); // nuevo

    }

    public class QuestionCreateViewModel
    {
        public string QuestionText { get; set; } = null!;
        public int FkTypeOption { get; set; }  // Nuevo campo
        public List<OptionCreateViewModel> Options { get; set; } = new();
    }

    public class OptionCreateViewModel
    {
        public string OptionText { get; set; } = null!;
        public bool IsCorrect { get; set; }
    }


}
