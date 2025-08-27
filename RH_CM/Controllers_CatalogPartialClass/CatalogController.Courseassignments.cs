using ClosedXML.Excel;
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

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseAssignmentsToExcel()
        {
            // 1) Obtener datos respetando los tipos del modelo
            var data = await _context.CtCourseassignments
                .AsNoTracking()
                .Join(_context.CtPositions,
                      ca => ca.FkPosition,
                      p => p.PkPosition,
                      (ca, p) => new { ca, p })
                .Join(_context.CtCourses,
                      tp => tp.ca.FkCourse,
                      c => c.PkCourse,
                      (tp, c) => new { tp.ca, tp.p, c })
                .Join(_context.CtLevelcourses,
                      tpc => tpc.ca.FkRequiredCourseLevels,
                      lc => lc.PkLevelcourse,
                      (tpc, lc) => new { tpc.ca, tpc.p, tpc.c, lc })
                // LEFT JOIN con DeliveryMode (FkDeliveryMode es nullable)
                .GroupJoin(_context.CtDeliverymodes,
                      t => t.ca.FkDeliveryMode,
                      dm => dm.PkDeliverymode,
                      (t, dms) => new { t.ca, t.p, t.c, t.lc, dms })
                .SelectMany(x => x.dms.DefaultIfEmpty(), (x, dm) => new
                {
                    x.ca.PkCourseAssignment,
                    PositionName = x.p.NamePosition,
                    CourseName = x.c.CourseName,
                    RequiredCourseLevelDescription = x.lc.DescripctionLevel,
                    // Tipos tal cual el modelo
                    x.ca.Requiered,              // bool
                    x.ca.Available,              // int (0/1)
                    DeliveryModeDescription = dm != null ? dm.DescriptionDeliverymode : null, // puede ser null
                                                                                              // Auditoría
                    x.ca.CreateUser,
                    x.ca.CreateDate,
                    x.ca.LastUpdateUser,
                    x.ca.LastUpdateDate
                })
                .OrderBy(x => x.PositionName)
                .ThenBy(x => x.CourseName)
                .ToListAsync();

            // 2) Crear Excel
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseAssignments");

            // Encabezados (incluye auditoría)
            ws.Cell(1, 1).Value = "PkCourseAssignment";
            ws.Cell(1, 2).Value = "Puesto";
            ws.Cell(1, 3).Value = "Curso";
            ws.Cell(1, 4).Value = "Nivel requerido";
            ws.Cell(1, 5).Value = "Requerido";
            ws.Cell(1, 6).Value = "Disponible";
            ws.Cell(1, 7).Value = "Modo de entrega";
            ws.Cell(1, 8).Value = "CreateUser";
            ws.Cell(1, 9).Value = "CreateDate";
            ws.Cell(1, 10).Value = "LastUpdateUser";
            ws.Cell(1, 11).Value = "LastUpdateDate";

            var header = ws.Range("A1:K1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // 3) Datos (mapeo directo según tipos)
            int row = 2;
            foreach (var it in data)
            {
                ws.Cell(row, 1).Value = it.PkCourseAssignment;
                ws.Cell(row, 2).Value = it.PositionName;
                ws.Cell(row, 3).Value = it.CourseName;
                ws.Cell(row, 4).Value = it.RequiredCourseLevelDescription;

                // Requiered es bool -> "Sí/No"
                ws.Cell(row, 5).Value = it.Requiered ? "Sí" : "No";

                // Available es int (0/1) -> "Sí/No"
                ws.Cell(row, 6).Value = it.Available == 1 ? "Sí" : "No";

                ws.Cell(row, 7).Value = it.DeliveryModeDescription ?? ""; // vacío si null

                ws.Cell(row, 8).Value = it.CreateUser;
                ws.Cell(row, 9).Value = it.CreateDate;
                ws.Cell(row, 10).Value = it.LastUpdateUser;
                ws.Cell(row, 11).Value = it.LastUpdateDate;

                row++;
            }

            // 4) Estilos y formato
            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 11);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            // Formatos de fecha/hora
            ws.Column(9).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
            ws.Column(11).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

            // Ajuste de columnas y congelar encabezado
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // 5) Descargar
            string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var content = stream.ToArray();
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CourseAssignments_{fechaActual}.xlsx"
            );
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
            [FromForm] int[] SelectedLevels,      // <-- NUEVO
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
            if (SelectedLevels == null || SelectedLevels.Length == 0)     // <-- NUEVO
            {
                TempData["ErrorMessage"] = "Selecciona al menos un Course Level.";
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
            var tvpLevels = ToTvp(SelectedLevels);   // <-- NUEVO
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

                var pLev = cmd.Parameters.AddWithValue("@Levels", tvpLevels);  // <-- NUEVO
                pLev.SqlDbType = SqlDbType.Structured;
                pLev.TypeName = "dbo.IntIdList";

                cmd.Parameters.Add(new SqlParameter("@FkDeliveryMode", SqlDbType.Int) { Value = FkDeliveryMode });
                cmd.Parameters.Add(new SqlParameter("@Requiered", SqlDbType.Bit) { Value = Requiered });
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = user });

                conn.Open();
                int affected = cmd.ExecuteNonQuery();

                if (affected == 0)
                    TempData["ErrorMessage"] = "No se generaron asignaciones nuevas (posibles duplicados).";
                else
                    TempData["SuccessMessage"] = $"Se crearon {affected} asignaciones nuevas.";
            }
            catch (SqlException)
            {
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

        // GET: /CourseAssignments
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexDeleteCourseassignments()
        {
            var model = _context.CtCourseassignments
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
                      (temp, dm) => new RH_CM.ViewModels.CourseAssignmentListItemViewModel
                      {
                          PkCourseAssignment = temp.ca.PkCourseAssignment,
                          PositionName = temp.p.NamePosition,
                          CourseName = temp.c.CourseName,
                          RequiredCourseLevelDescription = temp.lc.DescripctionLevel,

                          // 👇 NUEVO
                          FkRequiredCourseLevels = temp.ca.FkRequiredCourseLevels,

                          Requiered = temp.ca.Requiered,
                          Available = temp.ca.Available,
                          DeliveryModeDescription = dm.DescriptionDeliverymode
                      })
                .ToList();

            return View(model);
        }


        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseAssignmentsBulk([FromForm] int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona al menos un registro.";
                return RedirectToAction(nameof(IndexCourseassignments));
            }

            // Helper para TVP (mismo que usas en CreateBulk)
            static DataTable ToTvp(int[] ids)
            {
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                foreach (var id in ids.Distinct())
                    dt.Rows.Add(id);
                return dt;
            }

            try
            {
                var tvp = ToTvp(selectedIds);

                string cs = _context.Database.GetDbConnection().ConnectionString;
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand("dbo.sp_BulkDeleteCourseAssignments", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                var p = cmd.Parameters.AddWithValue("@Ids", tvp);
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = "dbo.IntIdList";

                conn.Open();

                // El SP hace: SELECT @@ROWCOUNT AS DeletedCount;
                object? scalar = cmd.ExecuteScalar();
                int deleted = 0;
                if (scalar != null && int.TryParse(Convert.ToString(scalar), out var d))
                    deleted = d;

                if (deleted == 0)
                    TempData["ErrorMessage"] = "No se eliminaron registros (¿IDs inexistentes?).";
                else
                    TempData["SuccessMessage"] = $"Se eliminaron {deleted} asignaciones.";
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al eliminar las asignaciones.";
            }

            return RedirectToAction(nameof(IndexCourseassignments));
        }


    }
}
