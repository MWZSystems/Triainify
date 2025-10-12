namespace RH_CM.Service.DTOs
{
    public class GroupsAccessDTOs
    {
        public bool OnBoarding { get; set; }
        public bool Offboarding { get; set; }
        public bool Supervisor { get; set; }
        public bool Department { get; set; }
        public bool Position { get; set; }
        public bool Course { get; set; }
        public bool CourseMaterial { get; set; }
        public bool CourseAssignments { get; set; }
        public bool CourseCompleted { get; set; }
        public bool CourseLevelMaterial { get; set; }
        public bool ExternalEvidence { get; set; }
        public bool MatrixByPosition { get; set; }
        public bool MatrixBySupervisor { get; set; }
        public bool HRReport { get; set; }
        public bool MaterialExamMissing { get; set; }
        public bool EvidenceByUser { get; set; }
        public bool MatrixByEmployee { get; set; }
        public bool Users { get; set; }
        public bool RegisterNewUser { get; set; }
        public bool Roles { get; set; }
        public bool Permissions { get; set; }
        public bool Resetpassword { get; set; }

        public bool Test { get; set; }
        public bool OcupationCode { get; set; }
        public bool ThematicArea { get; set; }
        public bool ThematicCourse { get; set; }

        public bool ShowCatalogsMenu { get; set; } = false;
        public bool ShowAssignmentsMenu { get; set; } = false;
        public bool ShowReportsMenu { get; set; } = false;

        public bool AlwaysShow { get; } = true;
    }
}
