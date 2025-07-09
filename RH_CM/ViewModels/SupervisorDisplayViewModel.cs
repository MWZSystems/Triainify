namespace RH_CM.ViewModels
{
    public class SupervisorDisplayViewModel
    {
        public int PkSupervisorId { get; set; }
        public int FkHeadcount { get; set; }
        public int ControlNumber { get; set; }
        public string Names { get; set; } = null!;
        public string? SecondName { get; set; }
        public string? LastName { get; set; }
    }
}
