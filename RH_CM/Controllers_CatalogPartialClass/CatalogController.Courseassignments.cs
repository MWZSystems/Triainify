using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using System.Data;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseassignments()
        {
            var courseAssignments = _context.CtCourseassignments
                .Join(_context.CtPositions,
                      ca => ca.FkPosition,
                      p => p.PkPosition,
                      (ca, p) => new { ca, p })
                .Join(_context.CtCourses,
                      temp => temp.ca.FkCourse,
                      c => c.PkCourse,
                      (temp, c) => new { temp.ca, temp.p, c })
                .Join(_context.CtLevelcourses,
                      temp => temp.ca.FkRequiredCourseLevels,
                      lc => lc.PkLevelcourse,
                      (temp, lc) => new { temp.ca, temp.p, temp.c, lc })
                .Join(_context.CtDeliverymodes,
                      temp => temp.ca.FkDeliveryMode,
                      dm => dm.PkDeliverymode,
                      (temp, dm) => new
                      {
                          temp.ca.PkCourseAssignment,
                          PositionName = temp.p.NamePosition,
                          CourseName = temp.c.CourseName,
                          RequiredCourseLevelDescription = temp.lc.DescripctionLevel,
                          Requiered = temp.ca.Requiered,
                          Available = temp.ca.Available,
                          DeliveryModeDescription = dm.DescriptionDeliverymode
                      })
                .ToList();

            return View(courseAssignments);
        }
        private void LoadCourseAssignmentViewBags()
        {
            ViewBag.Positions = _context.CtPositions
                .Where(p => p.Available == 1)
                .ToList();

            ViewBag.Courses = _context.CtCourses
                .Where(c => c.Available == 1)
                .ToList();

            ViewBag.LevelCourses = _context.CtLevelcourses
                .Where(lc => lc.Available == 1)
                .ToList();

            ViewBag.DeliveryModes = _context.CtDeliverymodes
                .Where(dm => dm.Available == 1)
                .ToList();
        }

        // GET: CourseAssignments/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseAssignment()
        {
            LoadCourseAssignmentViewBags();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignment(CtCourseassignment courseAssignment)
        {
            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels);

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Guardar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;
            courseAssignment.Available = 1;
            _context.CtCourseassignments.Add(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment created successfully.";
            LoadCourseAssignmentViewBags();
            return RedirectToAction(nameof(CreateCourseAssignment));
        }

        // GET: CourseAssignments/CreateBulk
        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseAssignmentBulk()
        {
            LoadCourseAssignmentViewBags();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignmentBulk(
            [FromForm] int[] SelectedPositions,
            [FromForm] int[] SelectedCourses,
            [FromForm] int FkRequiredCourseLevels,
            [FromForm] int FkDeliveryMode,
            [FromForm] bool Requiered)
        {
            // Validaciones básicas
            if (SelectedPositions == null || SelectedPositions.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona al menos una Position.";
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (SelectedCourses == null || SelectedCourses.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona al menos un Course.";
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level es obligatorio.";
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (FkDeliveryMode <= 0)
            {
                TempData["ErrorMessage"] = "Delivery Mode es obligatorio.";
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }

            // Helper para TVP
            static DataTable ToTvp(int[] ids)
            {
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                foreach (var id in ids.Distinct()) dt.Rows.Add(id);
                return dt;
            }

            var tvpPositions = ToTvp(SelectedPositions);
            var tvpCourses = ToTvp(SelectedCourses);
            var user = User?.Identity?.Name ?? "Unknown";

            try
            {
                string cs = _context.Database.GetDbConnection().ConnectionString;
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand("dbo.sp_BulkCreateCourseAssignments", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var pPos = cmd.Parameters.AddWithValue("@Positions", tvpPositions);
                pPos.SqlDbType = SqlDbType.Structured;
                pPos.TypeName = "dbo.IntIdList";

                var pCou = cmd.Parameters.AddWithValue("@Courses", tvpCourses);
                pCou.SqlDbType = SqlDbType.Structured;
                pCou.TypeName = "dbo.IntIdList";

                cmd.Parameters.Add(new SqlParameter("@FkRequiredCourseLevels", SqlDbType.Int) { Value = FkRequiredCourseLevels });
                cmd.Parameters.Add(new SqlParameter("@FkDeliveryMode", SqlDbType.Int) { Value = FkDeliveryMode });
                cmd.Parameters.Add(new SqlParameter("@Requiered", SqlDbType.Bit) { Value = Requiered });
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = user });

                conn.Open();
                int affected = cmd.ExecuteNonQuery(); // filas insertadas (nuevas combinaciones)

                if (affected == 0)
                {
                    TempData["ErrorMessage"] = "No se generaron asignaciones nuevas (posibles duplicados).";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Se crearon {affected} asignaciones nuevas.";
                }
            }
            catch (SqlException ex)
            {
                // Mensaje de error amigable; puedes loguear ex.Message
                TempData["ErrorMessage"] = "Ocurrió un error al crear las asignaciones masivas.";
            }

            return RedirectToAction(nameof(CreateCourseAssignmentBulk));
        }



        // GET: CourseAssignments/Edit/5
        [Authorize(Roles = "Administrador")]
        public IActionResult EditCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment == null) return NotFound();

            LoadCourseAssignmentViewBags();
            return View(courseAssignment);
        }

        // POST: CourseAssignments/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseAssignment(int id, CtCourseassignment courseAssignment)
        {
            if (id != courseAssignment.PkCourseAssignment) return NotFound();

            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels &&
                ca.PkCourseAssignment != id); // Excluir el registro actual

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Actualizar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;

            _context.CtCourseassignments.Update(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment updated successfully.";
            LoadCourseAssignmentViewBags();
            return RedirectToAction(nameof(EditCourseAssignment));
        }


        // POST: CourseAssignments/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment != null)
            {
                _context.CtCourseassignments.Remove(courseAssignment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Course assignment deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        // POST: /CtCourseassignment/ToggleCourseAssignment/5
        [HttpPost]
        [Route("ToggleCourseAssignment")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseAssignment(int id)
        {
            var courseAssignment = await _context.CtCourseassignments.FindAsync(id);
            if (courseAssignment != null)
            {
                // Alternar el valor de Available
                courseAssignment.Available = courseAssignment.Available == 1 ? 0 : 1;
                courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
                courseAssignment.LastUpdateDate = DateTime.Now;

                _context.CtCourseassignments.Update(courseAssignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The course assignment availability status was successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

    }
}
