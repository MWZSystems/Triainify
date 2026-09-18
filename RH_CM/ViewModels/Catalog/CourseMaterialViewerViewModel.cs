namespace RH_CM.ViewModels
{
    public class CourseMaterialViewerViewModel
    {
        public int CourseId { get; set; }
        public int LevelId { get; set; }

        // URL that serves the PDF (current flow)
        public string StreamUrl { get; set; } = string.Empty;

        // Optional
        public string? CourseName { get; set; }
        public int CourseAssignmentId { get; set; }
        public string? ExamUrl { get; set; }

        // NEW: for the tutorial/condition
        public string? MaterialType { get; set; } // "PDF" | "VIDEO"
        public string? UrlPath { get; set; }      // What you'll show the user to copy/paste into Explorer
    }
}
