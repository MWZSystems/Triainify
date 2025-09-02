using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.ViewModels;
using System.Data;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        //// GET: SyCoursecompleted
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        //public async Task<IActionResult> IndexCourseCompleted()
        //{
        //    var items = await _context.SyCoursecompleteds.AsNoTracking().ToListAsync();
        //    return View(items);
        //}
        public async Task<IActionResult> IndexCourseCompleted()
        {
            var query =
                from s in _context.SyCoursecompleteds.AsNoTracking()
                join ca in _context.CtCourseassignments.AsNoTracking() on s.FkCourseAssignment equals ca.PkCourseAssignment
                join cs in _context.CtCoursestatuses.AsNoTracking() on s.FkCourseStatus equals cs.PkCoursestatus
                join dm in _context.CtDeliverymodes.AsNoTracking() on s.FkDeliveryMode equals dm.PkDeliverymode into dmj
                from dm in dmj.DefaultIfEmpty()
                join hc in _context.SyHeadcounts.AsNoTracking() on s.FkHeadcount equals hc.PkHeadcount
                orderby s.PkCourseCompleted descending
                select new
                {
                    s.PkCourseCompleted,
                    s.FkCourseAssignment,
                    s.FkCourseStatus,
                    s.FkDeliveryMode,
                    s.FkHeadcount,
                    StatusName = cs.DescriptionCoursestatus,
                    DeliveryName = s.FkDeliveryMode == 0 ? "Not Assigned" : (dm != null ? dm.DescriptionDeliverymode : "Not Assigned"),
                    HeadcountName = hc.Names + " " + (hc.LastName ?? "") + " " + (hc.SecondName ?? ""),
                    hc.ControlNumber,
                    s.Avaialble,
                    s.CreateUser,
                    s.CreateDate,
                    s.LastUpdateUser,
                    s.LastUpdateDate
                };

            var data = await query.ToListAsync();
            return View(data);
        }

        // GET: SyCoursecompleted/Create
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        public IActionResult CreateCourseCompleted()
        {
            return View();
        }

        // POST: SyCoursecompleted/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseCompleted(SyCoursecompleted model)
        {
            // Validaciones manuales de requeridos / rangos
            if (model.FkCourseAssignment <= 0)
            {
                TempData["ErrorMessage"] = "El campo Course Assignment es requerido.";
                return View(model);
            }
            if (model.FkCourseStatus <= 0)
            {
                TempData["ErrorMessage"] = "El campo Course Status es requerido.";
                return View(model);
            }
            if (model.FkDeliveryMode <= 0)
            {
                TempData["ErrorMessage"] = "El campo Delivery Mode es requerido.";
                return View(model);
            }
            if (model.FkHeadcount <= 0)
            {
                TempData["ErrorMessage"] = "El campo Headcount es requerido.";
                return View(model);
            }
            if (model.Score < 0 || model.Score > 100)
            {
                TempData["ErrorMessage"] = "Score debe estar entre 0 y 100.";
                return View(model);
            }

            // (Opcional) Validación de duplicados según tu lógica de negocio:
            // Una finalización por asignación y empleado.
            bool alreadyExists = await _context.SyCoursecompleteds
                .AnyAsync(c =>
                    c.FkCourseAssignment == model.FkCourseAssignment &&
                    c.FkHeadcount == model.FkHeadcount);

            if (alreadyExists)
            {
                TempData["ErrorMessage"] = "Ya existe un registro de finalización para esta asignación y empleado.";
                return View(model);
            }

            // Asignar valores automáticos
            model.CreateUser = User?.Identity?.Name ?? "Unknown";
            model.CreateDate = DateTime.Now;
            model.LastUpdateUser = User?.Identity?.Name ?? "Unknown";
            model.LastUpdateDate = DateTime.Now;
            model.Avaialble = 1; // ojo: se respeta el nombre del modelo

            try
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registro de curso completado creado correctamente.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error al crear el registro: {ex.Message}";
                return View(model);
            }
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            //int selectedFkCourse = fkCourse ??
            //    _context.CtCourses.AsNoTracking()
            //        .OrderBy(c => c.CourseName)
            //        .Select(c => c.PkCourse)
            //        .FirstOrDefault();

            //// AHORA desde CtLevelcourse (PkLevelcourse = FkRequiredCourseLevels)
            //int selectedFkLevel = fkLevel ??
            //    _context.CtLevelcourses.AsNoTracking()
            //        .OrderBy(l => l.DescripctionLevel) // <-- campo del modelo
            //        .Select(l => l.PkLevelcourse)
            //        .FirstOrDefault();

            // Si no se pasa parámetro, dejamos 0 para que no haya selección al inicio
            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            LoadCourseCompletedBulkViewBags(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            return View();
        }

        private void LoadCourseCompletedBulkViewBags(int selectedFkCourse, int selectedFkLevel)
        {
            // Cursos
            ViewBag.Courses = _context.CtCourses
                .AsNoTracking()
                .Select(c => new { c.PkCourse, c.CourseName })
                .OrderBy(x => x.CourseName)
                .ToList();

            // Niveles requeridos: CtLevelcourse (PkLevelcourse = FkRequiredCourseLevels)
            ViewBag.Levels = _context.CtLevelcourses
                .AsNoTracking()
                .Select(l => new { l.PkLevelcourse, l.DescripctionLevel })
                .OrderBy(x => x.DescripctionLevel)
                .ToList();

            // Headcounts relacionados por posición contra CourseAssignments (curso + nivel)
            // Muestra el NamePosition desde CtPosition
            ViewBag.Headcounts = _context.SyHeadcounts
                .AsNoTracking()
                .Where(h => _context.CtCourseassignments
                    .Any(ca => ca.FkCourse == selectedFkCourse
                               && ca.FkRequiredCourseLevels == selectedFkLevel
                               && ca.FkPosition == h.FkPosition))
                .Select(h => new
                {
                    h.PkHeadcount,
                    // Trae el nombre de la posición (LEFT JOIN via subquery)
                    PositionName = _context.CtPositions
                        .AsNoTracking()
                        .Where(p => p.PkPosition == h.FkPosition)
                        .Select(p => p.NamePosition)
                        .FirstOrDefault(),

                    // Mantén tu CourseAssignmentId (el PK más bajo que cumple curso+nivel+posición)
                    CourseAssignmentId = _context.CtCourseassignments
                        .Where(ca => ca.FkCourse == selectedFkCourse
                                     && ca.FkRequiredCourseLevels == selectedFkLevel
                                     && ca.FkPosition == h.FkPosition)
                        .OrderBy(ca => ca.PkCourseAssignment)
                        .Select(ca => ca.PkCourseAssignment)
                        .FirstOrDefault(),

                    // Display amigable: "Control - Nombre Apellidos | Puesto"
                    // (se agrega el NamePosition y se limpian espacios)
                    Display = (
                        (h.ControlNumber + " - " +
                        h.Names + " " +
                        (h.LastName ?? "") + " " +
                        (h.SecondName ?? "")).Trim() +
                        " | " +
                        (_context.CtPositions
                            .Where(p => p.PkPosition == h.FkPosition)
                            .Select(p => p.NamePosition)
                            .FirstOrDefault() ?? "Sin posición")
                    )
                })
                .OrderBy(x => x.Display)
                .ToList();

            // CourseStatus
            ViewBag.CourseStatus = _context.CtCoursestatuses
                .AsNoTracking()
                .Select(s => new { s.PkCoursestatus, s.DescriptionCoursestatus })
                .OrderBy(s => s.DescriptionCoursestatus)
                .ToList();

            // DeliveryModes
            ViewBag.DeliveryModes = _context.CtDeliverymodes
                .AsNoTracking()
                .Select(d => new { Id = d.PkDeliverymode, Text = d.DescriptionDeliverymode })
                .OrderBy(x => x.Text)
                .ToList();
        }



        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseCompletedBulk(
            [FromForm] int[] SelectedHeadcounts,
            [FromForm] int FkCourse,
            [FromForm] int FkCourseStatus,
            [FromForm] int FkDeliveryMode,
            [FromForm] int Score
        )
        {
            // Validaciones (igual)
            if (SelectedHeadcounts == null || SelectedHeadcounts.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona al menos un Headcount.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse });
            }
            if (FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Selecciona el Curso a completar.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk));
            }
            if (FkCourseStatus <= 0)
            {
                TempData["ErrorMessage"] = "Course Status es obligatorio.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse });
            }
            if (FkDeliveryMode <= 0)
            {
                TempData["ErrorMessage"] = "Delivery Mode es obligatorio.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse });
            }
            if (Score < 0 || Score > 100)
            {
                TempData["ErrorMessage"] = "Score debe estar entre 0 y 100.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse });
            }

            static DataTable ToTvp(int[] ids)
            {
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                foreach (var id in ids.Distinct()) dt.Rows.Add(id);
                return dt;
            }

            var tvpHeadcounts = ToTvp(SelectedHeadcounts);
            var user = User?.Identity?.Name ?? "Unknown";

            try
            {
                string cs = _context.Database.GetDbConnection().ConnectionString;
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand("dbo.sp_BulkCreateCourseCompleted_ByCourse", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var pHc = cmd.Parameters.AddWithValue("@Headcounts", tvpHeadcounts);
                pHc.SqlDbType = SqlDbType.Structured;
                pHc.TypeName = "dbo.IntIdList";

                cmd.Parameters.Add(new SqlParameter("@FkCourse", SqlDbType.Int) { Value = FkCourse });
                cmd.Parameters.Add(new SqlParameter("@FkCourseStatus", SqlDbType.Int) { Value = FkCourseStatus });
                cmd.Parameters.Add(new SqlParameter("@FkDeliveryMode", SqlDbType.Int) { Value = FkDeliveryMode });
                cmd.Parameters.Add(new SqlParameter("@Score", SqlDbType.Int) { Value = Score });
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = user });
                cmd.Parameters.Add(new SqlParameter("@AllowUpsert", SqlDbType.Bit) { Value = 1 });

                conn.Open();
                // Tu SP NO devuelve result sets. Usa ExecuteNonQuery.
                int affected = cmd.ExecuteNonQuery();

                if (affected == 0)
                    TempData["ErrorMessage"] = "No se generaron cambios (posibles duplicados o sin CourseAssignment por posición/curso).";
                else
                    TempData["SuccessMessage"] = $"Operación completada. Filas afectadas: {affected}.";
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al crear los completados masivos.";
            }

            // Mantén el curso seleccionado al regresar al GET
            return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse });
        }

        // GET: SyCoursecompleted/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditCourseCompleted(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.SyCoursecompleteds.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: SyCoursecompleted/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseCompleted(int id, SyCoursecompleted model)
        {
            if (id != model.PkCourseCompleted)
            {
                TempData["ErrorMessage"] = "El registro especificado no fue encontrado.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }

            // Validaciones manuales
            if (model.FkCourseAssignment <= 0)
            {
                TempData["ErrorMessage"] = "El campo Course Assignment es requerido.";
                return View(model);
            }
            if (model.FkCourseStatus <= 0)
            {
                TempData["ErrorMessage"] = "El campo Course Status es requerido.";
                return View(model);
            }
            if (model.FkDeliveryMode <= 0)
            {
                TempData["ErrorMessage"] = "El campo Delivery Mode es requerido.";
                return View(model);
            }
            if (model.FkHeadcount <= 0)
            {
                TempData["ErrorMessage"] = "El campo Headcount es requerido.";
                return View(model);
            }
            if (model.Score < 0 || model.Score > 100)
            {
                TempData["ErrorMessage"] = "Score debe estar entre 0 y 100.";
                return View(model);
            }

            // (Opcional) Chequeo de duplicados excluyendo el registro actual
            bool alreadyExists = await _context.SyCoursecompleteds
                .AnyAsync(c =>
                    c.FkCourseAssignment == model.FkCourseAssignment &&
                    c.FkHeadcount == model.FkHeadcount &&
                    c.PkCourseCompleted != model.PkCourseCompleted);

            if (alreadyExists)
            {
                TempData["ErrorMessage"] = "Ya existe un registro de finalización para esta asignación y empleado.";
                return View(model);
            }

            try
            {
                var existing = await _context.SyCoursecompleteds.FindAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "El registro ya no existe en la base de datos.";
                    return NotFound();
                }

                // Actualizar campos permitidos
                existing.FkCourseAssignment = model.FkCourseAssignment;
                existing.FkCourseStatus = model.FkCourseStatus;
                existing.FkDeliveryMode = model.FkDeliveryMode;
                existing.FkHeadcount = model.FkHeadcount;
                existing.Score = model.Score;
                existing.LastUpdateUser = User?.Identity?.Name ?? "Unknown";
                existing.LastUpdateDate = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registro actualizado correctamente.";
                return RedirectToAction(nameof(EditCourseCompleted), new { id = existing.PkCourseCompleted });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Hubo un error de concurrencia al actualizar el registro.";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error al actualizar el registro: {ex.Message}";
                return View(model);
            }
        }

        // POST: SyCoursecompleted/Toggle/5
        [HttpPost]
        [Route("ToggleCourseCompleted")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseCompleted(int id)
        {
            var item = await _context.SyCoursecompleteds.FindAsync(id);
            if (item != null)
            {
                item.Avaialble = item.Avaialble == 1 ? 0 : 1; // nombre exacto del modelo
                item.LastUpdateUser = User?.Identity?.Name ?? "Unknown";
                item.LastUpdateDate = DateTime.Now;

                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        // POST: SyCoursecompleted/Delete/5
        [HttpPost]
        [Route("DeleteCourseCompleted")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseCompleted(int id)
        {
            var item = await _context.SyCoursecompleteds.FindAsync(id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "El registro no existe o ya fue eliminado.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }

            try
            {
                _context.SyCoursecompleteds.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Registro eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "No se puede eliminar el registro debido a relaciones en la base de datos.";
            }

            return RedirectToAction(nameof(IndexCourseCompleted));
        }



        private bool SyCourseCompletedExists(int id)
        {
            return _context.SyCoursecompleteds.Any(e => e.PkCourseCompleted == id);
        }
    }
}
