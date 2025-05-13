using RH_CM.Models;

namespace RH_CM.ViewModels
{
    public class IndexHeadCountListViewModel
    {
        public IEnumerable<CtDepartment> Departments { get; set; }
        public IEnumerable<CtPosition> Positions { get; set; }
        public IEnumerable<SyHeadCount> SyHeadCounts { get; set; }
        public Dictionary<int, int> HeadCountAges { get; set; } // Agregar este diccionario para almacenar edades
    }
}
