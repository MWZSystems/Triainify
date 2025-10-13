namespace RH_CM.ViewModels
{
    public class CompletedbyDepartmentViewModel
    {
        public string Department { get; set; } = "—";
        public int TotalAssigned { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalPending { get; set; }
        public decimal PercentCompleted { get; set; } // 0..100 (numérico)
    }

    // Página: lista + totales para el donut
    public class CompletedByDepartmentPageViewModel
    {
        public List<CompletedbyDepartmentViewModel> Rows { get; set; } = new();
        public int OverallAssigned { get; set; }
        public int OverallCompleted { get; set; }
        public int OverallPending { get; set; }
        public decimal OverallCompletedPct { get; set; } // 0..100
        public decimal OverallPendingPct { get; set; }   // 0..100
    }
}
