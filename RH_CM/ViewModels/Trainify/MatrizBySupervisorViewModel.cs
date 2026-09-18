namespace RH_CM.ViewModels
{
    public class MatrizBySupervisorViewModel
    {
        // Identity (do NOT remove)
        public int PK_HEADCOUNT { get; set; }
        public string? UserName { get; set; }
        public string? CONTROL_NUMBER { get; set; }
        public string? NAMES { get; set; }
        public string? LAST_NAME { get; set; }
        public string? SECOND_NAME { get; set; }

        // Position
        public int FK_Position { get; set; }
        public string? NAME_POSITION_ENGLISH { get; set; }

        // Summary by status
        public int Completed { get; set; }
        public int Pending { get; set; }
        public int ExpiringSoon { get; set; }   // Maps [Expiring Soon]
        public int Permanent { get; set; }
        public int Scheduled { get; set; }

        // Convenience for the view
        public string FullName =>
            $"{(NAMES ?? "").Trim()} {(LAST_NAME ?? "").Trim()} {(SECOND_NAME ?? "").Trim()}".Replace("  ", " ").Trim();
    }
}
