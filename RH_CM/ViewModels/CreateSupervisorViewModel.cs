using RH_CM.Models;

namespace RH_CM.ViewModels
{
    public class CreateSupervisorViewModel
    {
        public CtSupervisor Supervisor { get; set; } = new CtSupervisor();
        public List<SyHeadCount> SyHeadCounts { get; set; } = new List<SyHeadCount>();
        public List<CtDepartment> Departments { get; set; } = new List<CtDepartment>();
        public List<CtPosition> Positions { get; set; } = new List<CtPosition>();
    }
}
