using Microsoft.EntityFrameworkCore;
using RH_CM.Data;

namespace RH_CM.Service.Catalog
{
    /// <summary>
    /// Centralizes dependency checks for destructive catalog operations.
    /// The database model does not expose navigation properties for these relationships,
    /// so every delete endpoint must explicitly protect the records that still reference it.
    /// </summary>
    public sealed class CatalogIntegrityService
    {
        private readonly db_abcd61_rhchdbContext _context;

        public CatalogIntegrityService(db_abcd61_rhchdbContext context)
        {
            _context = context;
        }

        public async Task<string?> DepartmentDependencyAsync(int id, bool activeOnly = false)
        {
            var query = _context.SyHeadcounts.AsNoTracking().Where(x => x.FkDepartment == id);
            if (activeOnly) query = query.Where(x => x.Available == 1);

            return await query.AnyAsync()
                ? "This department is assigned to employees. Reassign or offboard those employees before deleting or disabling it."
                : null;
        }

        public async Task<string?> PositionDependencyAsync(int id, bool activeOnly = false)
        {
            var employees = _context.SyHeadcounts.AsNoTracking().Where(x => x.FkPosition == id);
            if (activeOnly) employees = employees.Where(x => x.Available == 1);

            if (await employees.AnyAsync())
                return "This position is assigned to employees. Reassign or offboard those employees before deleting or disabling it.";

            if (!activeOnly && await _context.CtSupervisors.AsNoTracking().AnyAsync(x => x.FkPosition == id))
                return "This position is used by supervisor records. Remove or reassign those supervisor records first.";

            if (!activeOnly && await _context.CtCourseassignments.AsNoTracking().AnyAsync(x => x.FkPosition == id))
                return "This position has course assignments. Remove or reassign those course assignments first.";

            if (!activeOnly && await _context.CtOcupationcodes.AsNoTracking().AnyAsync(x => x.FkPosition == id))
                return "This position has occupation codes. Remove or reassign those occupation codes first.";

            return null;
        }

        public async Task<string?> SupervisorDependencyAsync(int id, bool activeOnly = false)
        {
            var query = _context.SyHeadcounts.AsNoTracking().Where(x => x.FkSupervisorId == id);
            if (activeOnly) query = query.Where(x => x.Available == 1);

            return await query.AnyAsync()
                ? "This supervisor is assigned to employees. Reassign or offboard those employees first."
                : null;
        }

        public async Task<string?> CourseDependencyAsync(int id)
        {
            if (await _context.CtCourseassignments.AsNoTracking().AnyAsync(x => x.FkCourse == id))
                return "This course has course assignments. Remove those assignments first.";
            if (await _context.CtTests.AsNoTracking().AnyAsync(x => x.FkCourse == id))
                return "This course has tests. Disable or remove those tests first.";
            if (await _context.CtCourseLevelMaterials.AsNoTracking().AnyAsync(x => x.FkCourse == id))
                return "This course has linked materials. Unlink those materials first.";
            if (await _context.CtThematiccourses.AsNoTracking().AnyAsync(x => x.FkCourse == id))
                return "This course is linked to thematic areas. Remove those links first.";
            return null;
        }

        public async Task<string?> CourseAssignmentDependencyAsync(IEnumerable<int> ids)
        {
            var assignmentIds = ids.Distinct().ToArray();
            if (assignmentIds.Length == 0) return null;

            if (await _context.SyCoursemovements.AsNoTracking().AnyAsync(x => assignmentIds.Contains(x.FkCourseAssignment)) ||
                await _context.SyCoursecompleteds.AsNoTracking().AnyAsync(x => assignmentIds.Contains(x.FkCourseAssignment)))
            {
                return "One or more course assignments contain exam or completion history. Disable them instead of deleting them.";
            }

            if (await _context.SyExcludedcourseassignments.AsNoTracking().AnyAsync(x => assignmentIds.Contains(x.FkCourseAssignment)))
                return "One or more course assignments are referenced by exclusions. Remove those exclusions first.";

            return null;
        }

        public async Task<string?> CourseMaterialDependencyAsync(int id)
        {
            return await _context.CtCourseLevelMaterials.AsNoTracking().AnyAsync(x => x.FkCourseMaterial == id)
                ? "This material is linked to a course and level. Unlink it first, or disable the material to preserve the relationship."
                : null;
        }

        public async Task<string?> ThematicAreaDependencyAsync(int id)
        {
            return await _context.CtThematiccourses.AsNoTracking().AnyAsync(x => x.FkThematicarea == id)
                ? "This thematic area is linked to courses. Remove those links first, or disable the area."
                : null;
        }
    }
}
