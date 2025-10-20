namespace RH_CM.ViewModels
{
    public class CourseMaterialViewerViewModel
    {
        public int CourseId { get; set; }
        public int LevelId { get; set; }

        // URL que sirve el PDF (tu flujo actual)
        public string StreamUrl { get; set; } = string.Empty;

        // Opcionales
        public string? CourseName { get; set; }
        public int CourseAssignmentId { get; set; }
        public string? ExamUrl { get; set; }

        // 👇 NUEVO para el tutorial/condición
        public string? MaterialType { get; set; } // "PDF" | "VIDEO"
        public string? UrlPath { get; set; }      // Lo que mostrarás al usuario para copiar/pegar en Explorer
    }
}
