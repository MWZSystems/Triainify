using DocumentFormat.OpenXml.Office2010.PowerPoint;

namespace RH_CM.Service.DTOs.OcupationKey
{
    public class OcupationDTOs
    {
        public List<OcupationsList>? OcupationsLists {  get; set; }
        public List<Positions>? PositionsList { get; set; }

    }

    public class Positions 
    {
        public string? Position { get; set; }
    }

    public class OcupationsList
    {
        public int Id { get; set; }
        public int FkPosition { get; set; }
        public string? PositionName { get; set; }
        public int OcupationCode { get; set; }
    }

}
