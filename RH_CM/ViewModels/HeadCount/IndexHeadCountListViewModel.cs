using RH_CM.Models;

namespace RH_CM.ViewModels
{
    public class IndexHeadCountListViewModel
    {
        public IEnumerable<CtDepartment> Departments { get; set; } = Enumerable.Empty<CtDepartment>();
        public IEnumerable<CtPosition> Positions { get; set; } = Enumerable.Empty<CtPosition>();
        public IEnumerable<SyHeadcount> SyHeadCount { get; set; } = Enumerable.Empty<SyHeadcount>();
        public Dictionary<int, int> HeadCountAges { get; set; } = new(); // Dictionary to store ages
    }
}
