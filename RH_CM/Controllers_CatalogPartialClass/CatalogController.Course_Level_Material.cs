using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using ClosedXML.Excel;
using System.Data;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // =============================
        // Helpers (combos)
        // =============================
        private void LoadCourseLevelMaterialViewBags()
        {
            // Cursos disponibles
            var courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToList();

            // Niveles disponibles
            var levels = _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToList();

            // IDs de materiales YA usados en algún vínculo Course–Level–Material
            var usedMaterialIds = _context.CtCourseLevelMaterials
                .AsNoTracking()
                .Select(x => x.FkCourseMaterial)
                .ToHashSet(); // eficiente para Contains()


            //// Materiales disponibles y NO asignados en CT_COURSE_LEVEL_MATERIAL
            //var materials = _context.CtCoursematerials
            //    .AsNoTracking()
            //    .Where(m => m.Available == 1 && !usedMaterialIds.Contains(m.PkCoursematerial))
            //    .OrderBy(m => m.NameMaterial)
            //    .ToList();


            //Traerse la entidad completa es un problema porque los PDFs son pesados, se cambia para tener un SELECt unicamente lo que se requiere.

            //var partialData = _context.CtCoursematerials
            //     .AsNoTracking()
            //     .Where(m => m.Available == 1)
            //     .OrderBy(m => m.NameMaterial)
            //     .Select(m => new
            //     {
            //         m.PkCoursematerial,
            //         m.NameMaterial
            //     })
            //     .ToList();


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

            // Mapear a CtCoursematerial para que sea compatible con el ViewBag
            var materials = partialData.Select(x => new CtCoursematerial
            {
                PkCoursematerial = x.PkCoursematerial,
                NameMaterial = x.NameMaterial
            }).ToList();


            // Nunca dejes null en los ViewBags
            ViewBag.Courses = courses ?? new List<CtCourse>();
            ViewBag.LevelCourses = levels ?? new List<CtLevelcourse>();
            ViewBag.Materials = materials ?? new List<CtCoursematerial>();
        }

        // =============================
        // INDEX
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseLevelMaterial()
        {
            //Aqui lo tuve que modificar porque usaba los constrains del sql server, al borrar las ligas entre las tablas tuve que armar el query
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

            return View(items);
        }

        // =============================
        // EXPORTAR A EXCEL
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseLevelMaterialToExcel()
        {
            //Aqui lo tuve que modificar porque usaba los constrains del sql server, al borrar las ligas entre las tablas tuve que armar el query
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
                ws.Cell(row, 5).Value = it.Available == 1 ? "Sí" : "No";
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


        // =============================
        // CREATE (GET)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpGet]
        public IActionResult CreateCourseLevelMaterial()
        {
            LoadCourseLevelMaterialViewBags();
            return View(new CtCourseLevelMaterial());
        }

        // =============================
        // CREATE (POST)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseLevelMaterial(CtCourseLevelMaterial model)
        {
            if (model.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }
            if (model.FkLevelCourse <= 0)
            {
                TempData["ErrorMessage"] = "Level is required.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }
            if (model.FkCourseMaterial <= 0)
            {
                TempData["ErrorMessage"] = "Material is required.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }
            if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkLevelCourse))
            {
                TempData["ErrorMessage"] = "This Course-Level combination doesn't exist in Courseassignments.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }

            // Evita duplicado exacto (Course, Level, Material)
            bool exists = _context.CtCourseLevelMaterials.Any(x =>
                x.FkCourse == model.FkCourse &&
                x.FkLevelCourse == model.FkLevelCourse &&
                x.FkCourseMaterial == model.FkCourseMaterial
            );
            if (exists)
            {
                TempData["ErrorMessage"] = "This (Course, Level, Material) link already exists.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }

            model.CreateUser = User.Identity?.Name ?? "Unknown";
            model.CreateDate = DateTime.Now;
            model.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            model.LastUpdateDate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Link created successfully.";
            LoadCourseLevelMaterialViewBags();
            return RedirectToAction(nameof(CreateCourseLevelMaterial));
        }

        // =============================
        // EDIT (GET)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpGet]
        public IActionResult EditCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null) return NotFound();

            LoadCourseLevelMaterialViewBags();
            return View(link);
        }

        // =============================
        // EDIT (POST)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseLevelMaterial(int id, CtCourseLevelMaterial model)
        {
            if (id != model.PkCourseLevelMaterial) return NotFound();

            var existing = _context.CtCourseLevelMaterials.Find(id);
            if (existing == null) return NotFound();

            if (model.FkCourse <= 0 || model.FkLevelCourse <= 0 || model.FkCourseMaterial <= 0)
            {
                TempData["ErrorMessage"] = "Course, Level and Material are required.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }

            // Evitar duplicado con otros registros
            bool duplicate = _context.CtCourseLevelMaterials.Any(x =>
                x.PkCourseLevelMaterial != id &&
                x.FkCourse == model.FkCourse &&
                x.FkLevelCourse == model.FkLevelCourse &&
                x.FkCourseMaterial == model.FkCourseMaterial
            );
            if (duplicate)
            {
                TempData["ErrorMessage"] = "Another link with the same (Course, Level, Material) already exists.";
                LoadCourseLevelMaterialViewBags();
                return View(model);
            }

            existing.FkCourse = model.FkCourse;
            existing.FkLevelCourse = model.FkLevelCourse;
            existing.FkCourseMaterial = model.FkCourseMaterial;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            _context.Update(existing);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Link updated successfully.";
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }

        // =============================
        // DELETE (POST)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null)
            {
                TempData["ErrorMessage"] = "Link not found.";
                return RedirectToAction(nameof(IndexCourseLevelMaterial));
            }

            _context.CtCourseLevelMaterials.Remove(link);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Link deleted successfully.";
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }

        // =============================
        // TOGGLE AVAILABLE (POST)
        // =============================
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCourseLevelMaterial(int id)
        {
            var link = _context.CtCourseLevelMaterials.Find(id);
            if (link == null)
            {
                TempData["ErrorMessage"] = "Link not found.";
                return RedirectToAction(nameof(IndexCourseLevelMaterial));
            }

            link.Available = link.Available == 1 ? 0 : 1;
            link.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            link.LastUpdateDate = DateTime.Now;

            _context.Update(link);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Availability updated successfully.";
            return RedirectToAction(nameof(IndexCourseLevelMaterial));
        }
    }
}
