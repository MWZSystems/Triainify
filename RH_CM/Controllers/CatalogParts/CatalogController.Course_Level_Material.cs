using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using ClosedXML.Excel;
using System.Data;
using System.Threading.Tasks;
using RH_CM.Messages.Catalog;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        /// <summary>
        /// Loads the dropdown options (courses, levels, materials) used by the create/edit forms.
        /// </summary>
        private void LoadCourseLevelMaterialViewBags()
        {
            // Available courses
            var courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToList();

            // Available levels
            var levels = _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToList();

            // IDs of materials already used in a Course-Level-Material link
            var usedMaterialIds = _context.CtCourseLevelMaterials
                .AsNoTracking()
                .Select(x => x.FkCourseMaterial)
                .ToHashSet(); // efficient for Contains()


            var partialData = _context.CtCoursematerials
                    .AsNoTracking()
                    .Where(m => m.Available == 1 && !usedMaterialIds.Contains(m.PkCoursematerial))
                    .OrderBy(m => m.NameMaterial)
                    .Select(m => new
                    {
                        m.PkCoursematerial,
                        m.NameMaterial
                    })
                    .ToList();

            // Map to CtCoursematerial so it's compatible with the ViewBag
            var materials = partialData.Select(x => new CtCoursematerial
            {
                PkCoursematerial = x.PkCoursematerial,
                NameMaterial = x.NameMaterial
            }).ToList();


            // Never leave the ViewBags null
            ViewBag.Courses = courses ?? new List<CtCourse>();
            ViewBag.LevelCourses = levels ?? new List<CtLevelcourse>();
            ViewBag.Materials = materials ?? new List<CtCoursematerial>();
        }

        /// <summary>
        /// Displays the list of course-level-material links.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] //OnBoardingView
        public IActionResult IndexCourseLevelMaterial()
        {
            // Had to rework this: it used to rely on SQL Server FK constraints, but after removing the table links this query has to be built manually.
            var items = _context.CtCourseLevelMaterials
                           .Join(_context.CtCourses, 
                               clm => clm.FkCourse,
                               cs => cs.PkCourse,
                               (clm, cs) => new {clm,cs})
                           .Join(_context.CtLevelcourses,
                               temp => temp.clm.FkLevelCourse,
                               lc => lc.PkLevelcourse,
                               (temp, lc) => new { temp.clm, temp.cs, lc })
                           .Join(_context.CtCoursematerials,
                                temp => temp.clm.FkCourseMaterial,
                                cm => cm.PkCoursematerial,
                                (temp, cm) => new //{temp.clm, temp.cs, temp.lc, cm})
                         {
                                    temp.clm.PkCourseLevelMaterial,
                                    CourseId = temp.clm.FkCourse,
                                    CourseName = temp.cs.CourseName,
                                    LevelId = temp.clm.FkLevelCourse,
                                    LevelDescription = temp.lc.DescripctionLevel,
                                    MaterialId = temp.clm.FkCourseMaterial,
                                    MaterialName = cm.NameMaterial,
                                    temp.clm.Available,
                                    temp.clm.CreateUser,
                                    temp.clm.CreateDate,
                                    temp.clm.LastUpdateUser,
                                    temp.clm.LastUpdateDate

                         })
                         .ToList();

            return View("CourseLevelMaterial/IndexCourseLevelMaterial", items);
        }

        /// <summary>
        /// Exports the course-level-material links to an Excel file.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseLevelMaterialToExcel()
        {
            // Had to rework this: it used to rely on SQL Server FK constraints, but after removing the table links this query has to be built manually.
            var data = await (_context.CtCourseLevelMaterials
                           .Join(_context.CtCourses,
                               clm => clm.FkCourse,
                               cs => cs.PkCourse,
                               (clm, cs) => new { clm, cs })
                           .Join(_context.CtLevelcourses,
                               temp => temp.clm.FkLevelCourse,
                               lc => lc.PkLevelcourse,
                               (temp, lc) => new { temp.clm, temp.cs, lc })
                           .Join(_context.CtCoursematerials,
                                temp => temp.clm.FkCourseMaterial,
                                cm => cm.PkCoursematerial,
                                (temp, cm) => new 
                                {
                                    temp.clm.PkCourseLevelMaterial,
                                    Course = temp.cs.CourseName,
                                    Level = temp.clm.FkLevelCourse,
                                    Material = cm.NameMaterial,
                                    temp.clm.Available,
                                    temp.clm.CreateUser,
                                    temp.clm.CreateDate,
                                    temp.clm.LastUpdateUser,
                                    temp.clm.LastUpdateDate

                                })).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseLevelMaterial");

            ws.Cell(1, 1).Value = "PK";
            ws.Cell(1, 2).Value = "Course";
            ws.Cell(1, 3).Value = "Level";
            ws.Cell(1, 4).Value = "Material";
            ws.Cell(1, 5).Value = "Available";
            ws.Cell(1, 6).Value = "CreateUser";
            ws.Cell(1, 7).Value = "CreateDate";
            ws.Cell(1, 8).Value = "LastUpdateUser";
            ws.Cell(1, 9).Value = "LastUpdateDate";

            var header = ws.Range("A1:I1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var it in data)
            {
                ws.Cell(row, 1).Value = it.PkCourseLevelMaterial;
                ws.Cell(row, 2).Value = it.Course;
                ws.Cell(row, 3).Value = it.Level;
                ws.Cell(row, 4).Value = it.Material;
                ws.Cell(row, 5).Value = it.Available == 1 ? "Yes" : "No";
                ws.Cell(row, 6).Value = it.CreateUser;
                ws.Cell(row, 7).Value = it.CreateDate;
                ws.Cell(row, 8).Value = it.LastUpdateUser;
                ws.Cell(row, 9).Value = it.LastUpdateDate;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CourseLevelMaterial_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }

        /// <summary>
        /// Raw export of every column in CtCourseLevelMaterials, with no joins or translations,
        /// so staff can cross-check the data behind the Course Level Material links.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportCourseLevelMaterialFullData()
        {
            var data = await _context.CtCourseLevelMaterials.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "CourseLevelMaterial");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("CourseLevelMaterial"));
        }


        /// <summary>
        /// Displays the form to link a new material to a course level.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public IActionResult CreateCourseLevelMaterial()
        {
            LoadCourseLevelMaterialViewBags();
            return View("CourseLevelMaterial/CreateCourseLevelMaterial", new CtCourseLevelMaterial());
        }

        /// <summary>
        /// Creates a new course-level-material link.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseLevelMaterial(CtCourseLevelMaterial model)
        {
            if (model.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.CourseIsRequired;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/CreateCourseLevelMaterial", model);
            }
            if (model.FkLevelCourse <= 0)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.LevelIsRequired;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/CreateCourseLevelMaterial", model);
            }
            if (model.FkCourseMaterial <= 0)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.MaterialIsRequired;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/CreateCourseLevelMaterial", model);
            }
            if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkLevelCourse))
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.CourseLevelCombinationDoesntExistInCourseassignments;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/CreateCourseLevelMaterial", model);
            }

            // Prevent an exact duplicate (Course, Level, Material)
            bool exists = _context.CtCourseLevelMaterials.Any(x =>
                x.FkCourse == model.FkCourse &&
                x.FkLevelCourse == model.FkLevelCourse &&
                x.FkCourseMaterial == model.FkCourseMaterial
            );
            if (exists)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.CourseLevelMaterialLinkAlreadyExists;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/CreateCourseLevelMaterial", model);
            }

            model.CreateUser = User.Identity?.Name ?? "Unknown";
            model.CreateDate = DateTime.Now;
            model.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            model.LastUpdateDate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseLevelMaterialMessages.LinkCreatedSuccessfully;
            LoadCourseLevelMaterialViewBags();
            return RedirectToAction(nameof(CreateCourseLevelMaterial));
        }

        /// <summary>
        /// Displays the form to edit an existing course-level-material link.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] 
        [HttpGet]
        public IActionResult EditCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null) return NotFound();

            LoadCourseLevelMaterialViewBags();
            return View("CourseLevelMaterial/EditCourseLevelMaterial", link);
        }

        /// <summary>
        /// Updates an existing course-level-material link.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseLevelMaterial(int id, CtCourseLevelMaterial model)
        {
            if (id != model.PkCourseLevelMaterial) return NotFound();

            var existing = _context.CtCourseLevelMaterials.Find(id);
            if (existing == null) return NotFound();

            if (model.FkCourse <= 0 || model.FkLevelCourse <= 0 || model.FkCourseMaterial <= 0)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.CourseLevelAndMaterialAreRequired;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/EditCourseLevelMaterial", model);
            }

            // Avoid a duplicate against other records
            bool duplicate = _context.CtCourseLevelMaterials.Any(x =>
                x.PkCourseLevelMaterial != id &&
                x.FkCourse == model.FkCourse &&
                x.FkLevelCourse == model.FkLevelCourse &&
                x.FkCourseMaterial == model.FkCourseMaterial
            );
            if (duplicate)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.AnotherLinkWithTheSameCourseLevel;
                LoadCourseLevelMaterialViewBags();
                return View("CourseLevelMaterial/EditCourseLevelMaterial", model);
            }

            existing.FkCourse = model.FkCourse;
            existing.FkLevelCourse = model.FkLevelCourse;
            existing.FkCourseMaterial = model.FkCourseMaterial;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            _context.Update(existing);
            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseLevelMaterialMessages.LinkUpdatedSuccessfully;
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }

        /// <summary>
        /// Deletes a course-level-material link.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.LinkNotFound;
                return RedirectToAction(nameof(IndexCourseLevelMaterial));
            }

            _context.CtCourseLevelMaterials.Remove(link);
            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseLevelMaterialMessages.LinkDeletedSuccessfully;
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }

        /// <summary>
        /// Toggles a course-level-material link's availability.
        /// </summary>
        [Authorize(Policy = "ViewAccess")] 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null)
            {
                TempData["ErrorMessage"] = CourseLevelMaterialMessages.LinkNotFound;
                return RedirectToAction(nameof(IndexCourseLevelMaterial));
            }

            link.Available = link.Available == 1 ? 0 : 1;
            link.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            link.LastUpdateDate = DateTime.Now;

            _context.Update(link);
            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseLevelMaterialMessages.AvailabilityUpdatedSuccessfully;
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }
    }
}
