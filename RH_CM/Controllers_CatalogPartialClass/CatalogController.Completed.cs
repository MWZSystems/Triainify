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
                join ca in _context.CtCourseassignments.AsNoTracking()
                    on s.FkCourseAssignment equals ca.PkCourseAssignment
                join c in _context.CtCourses.AsNoTracking()
                    on ca.FkCourse equals c.PkCourse
                join lc in _context.CtLevelcourses.AsNoTracking()                 // <-- NUEVO
                    on ca.FkRequiredCourseLevels equals lc.PkLevelcourse          // <-- NUEVO
                join hc in _context.SyHeadcounts.AsNoTracking()
                    on s.FkHeadcount equals hc.PkHeadcount
                orderby s.PkCourseCompleted descending
                select new
                {
                    s.PkCourseCompleted,
                    hc.ControlNumber,
                    HeadcountName = hc.Names + " " + (hc.LastName ?? "") + " " + (hc.SecondName ?? ""),
                    CourseName = c.CourseName,
                    LevelDescription = lc.DescripctionLevel,   // <-- usa el nombre exacto de tu campo
                    s.Avaialble,
                    s.LastUpdateDate
                };

            var data = await query.ToListAsync();
            return View(data);
        }

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseCompletedToExcel()
        {
            // 1) Mismo query que IndexCourseCompleted (mismas uniones y columnas)
            var data =
                await (from s in _context.SyCoursecompleteds.AsNoTracking()
                       join ca in _context.CtCourseassignments.AsNoTracking()
                            on s.FkCourseAssignment equals ca.PkCourseAssignment
                       join c in _context.CtCourses.AsNoTracking()
                            on ca.FkCourse equals c.PkCourse
                       join lc in _context.CtLevelcourses.AsNoTracking()
                            on ca.FkRequiredCourseLevels equals lc.PkLevelcourse
                       join hc in _context.SyHeadcounts.AsNoTracking()
                            on s.FkHeadcount equals hc.PkHeadcount
                       orderby s.PkCourseCompleted descending
                       select new
                       {
                           s.PkCourseCompleted,
                           hc.ControlNumber,
                           HeadcountName = hc.Names + " " + (hc.LastName ?? "") + " " + (hc.SecondName ?? ""),
                           CourseName = c.CourseName,
                           LevelDescription = lc.DescripctionLevel, // ajusta a DescriptionLevel si aplica
                           s.Avaialble,                              // (int 0/1 en tu modelo)
                           s.LastUpdateDate
                       }).ToListAsync();

            // 2) Crear Excel
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseCompleted");

            // Encabezados EXACTOS (mismo orden que el select new de arriba)
            ws.Cell(1, 1).Value = "PkCourseCompleted";
            ws.Cell(1, 2).Value = "ControlNumber";
            ws.Cell(1, 3).Value = "HeadcountName";
            ws.Cell(1, 4).Value = "CourseName";
            ws.Cell(1, 5).Value = "LevelDescription";
            ws.Cell(1, 6).Value = "Available";          // corresponde a Avaialble (0/1)
            ws.Cell(1, 7).Value = "LastUpdateDate";

            var header = ws.Range("A1:G1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // 3) Escribir filas (manteniendo tipos)
            int row = 2;
            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.PkCourseCompleted;
                ws.Cell(row, 2).Value = x.ControlNumber;
                ws.Cell(row, 3).Value = x.HeadcountName?.Trim();
                ws.Cell(row, 4).Value = x.CourseName;
                ws.Cell(row, 5).Value = x.LevelDescription;

                // Si prefieres 0/1, usa: ws.Cell(row, 6).Value = x.Avaialble;
                // Si prefieres texto, deja esta línea:
                ws.Cell(row, 6).Value = (Convert.ToInt32(x.Avaialble) == 1) ? "Sí" : "No";

                ws.Cell(row, 7).Value = x.LastUpdateDate;
                row++;
            }

            // 4) Estilos y formatos
            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 7);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            // Formato de fecha/hora para LastUpdateDate
            ws.Column(7).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

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
                $"CourseCompleted_{fechaActual}.xlsx"
            );
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
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateCourseCompletedBulk(
            int FkCourse,
            int FkRequiredCourseLevels,
            int[] SelectedHeadcounts,
            int Score,
            bool allowUpsert = true)
        {
            if (FkCourse <= 0 || FkRequiredCourseLevels <= 0 || SelectedHeadcounts == null || SelectedHeadcounts.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona curso, nivel y al menos una persona.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
            }

            // Construir TVP dbo.IntIdList(Id INT)
            var tvp = new System.Data.DataTable();
            tvp.Columns.Add("Id", typeof(int));
            foreach (var id in SelectedHeadcounts.Distinct()) tvp.Rows.Add(id);

            var pHeadcounts = new Microsoft.Data.SqlClient.SqlParameter("@Headcounts", tvp)
            {
                SqlDbType = System.Data.SqlDbType.Structured,
                TypeName = "dbo.IntIdList"
            };
            var pFkCourse = new Microsoft.Data.SqlClient.SqlParameter("@FkCourse", FkCourse);
            var pScore = new Microsoft.Data.SqlClient.SqlParameter("@Score", Score);
            var pUserName = new Microsoft.Data.SqlClient.SqlParameter("@UserName", User?.Identity?.Name ?? "system");
            var pAllowUpsert = new Microsoft.Data.SqlClient.SqlParameter("@AllowUpsert", allowUpsert);

            // Llama al SP ACTUALIZADO (sin FkCourseStatus / FkDeliveryMode)
            var sql = "EXEC dbo.sp_BulkCreateCourseCompleted_ByCourse @Headcounts, @FkCourse, @Score, @UserName, @AllowUpsert";
            await _context.Database.ExecuteSqlRawAsync(sql, pHeadcounts, pFkCourse, pScore, pUserName, pAllowUpsert);

            TempData["SuccessMessage"] = "Registros creados/actualizados correctamente.";
            return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult DeleteCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            // Combos
            LoadCourseCompletedBulkViewBags(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            // Exigir curso + nivel antes de listar
            bool needFilters = (selectedFkCourse <= 0 || selectedFkLevel <= 0);
            ViewBag.NeedFilters = needFilters;

            var rows = new List<CourseCompletedRowDtoViewModel>();
            if (!needFilters)
            {
                var cs = _context.Database.GetDbConnection().ConnectionString;

                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand("dbo.sp_BulkSearchCourseCompleted", conn)
                { CommandType = CommandType.StoredProcedure };

                cmd.Parameters.Add(new SqlParameter("@FkCourse", SqlDbType.Int) { Value = selectedFkCourse });
                cmd.Parameters.Add(new SqlParameter("@FkLevel", SqlDbType.Int) { Value = selectedFkLevel });

                try
                {
                    conn.Open();
                }
                catch (SqlException ex)
                {
                    var state = ex.Errors.Count > 0 ? ex.Errors[0].State : (byte)0;
                    var msg = $"SQL ERROR: {ex.Number}, STATE: {state}, MESSAGE: {ex.Message}";
                    // Loguea si tienes logger; aquí relanzamos con detalle:
                    throw new Exception($"Error al conectar a la base de datos. Detalle: {msg}", ex);
                }

                using var rdr = cmd.ExecuteReader();

                // helpers para DBNull
                static string GetString(SqlDataReader r, string col)
                    => r.IsDBNull(r.GetOrdinal(col)) ? "" : r.GetString(r.GetOrdinal(col));
                static int GetInt(SqlDataReader r, string col)
                    => r.IsDBNull(r.GetOrdinal(col)) ? 0 : r.GetInt32(r.GetOrdinal(col));
                static DateTime GetDate(SqlDataReader r, string col)
                    => r.IsDBNull(r.GetOrdinal(col)) ? DateTime.MinValue : r.GetDateTime(r.GetOrdinal(col));

                while (rdr.Read())
                {
                    // CONTROL_NUMBER puede no ser nvarchar en algunos diseños; convertir a string por seguridad
                    string control = rdr.IsDBNull(rdr.GetOrdinal("HeadcountControlNumber"))
                        ? ""
                        : Convert.ToString(rdr.GetValue(rdr.GetOrdinal("HeadcountControlNumber")));

                    rows.Add(new CourseCompletedRowDtoViewModel
                    {
                        PkCourseCompleted = GetInt(rdr, "PK_CourseCompleted"),
                        HeadcountControlNumber = control,
                        HeadcountFullName = GetString(rdr, "HeadcountFullName"),
                        CourseName = GetString(rdr, "CourseName"),
                        LevelName = GetString(rdr, "LevelName"),
                        StatusName = GetString(rdr, "StatusName"),
                        DeliveryModeName = GetString(rdr, "DeliveryModeName"),
                        Score = GetInt(rdr, "Score"),
                        CreateDate = GetDate(rdr, "CreateDate")
                    });
                }
            }

            ViewBag.CourseCompletedRows = rows;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseCompletedBulk(
            [FromForm] int[] SelectedCourseCompletedIds,
            [FromForm] bool SoftDelete,
            // Hidden para rehidratar filtros al volver
            [FromForm] int FkCourse,
            [FromForm] int FkLevel
        )
        {
            if (SelectedCourseCompletedIds == null || SelectedCourseCompletedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona al menos un registro a eliminar.";
                return RedirectToAction(nameof(DeleteCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkLevel });
            }

            static DataTable ToTvp(int[] ids)
            {
                var dt = new DataTable();
                dt.Columns.Add("Id", typeof(int));
                foreach (var id in ids.Distinct()) dt.Rows.Add(id);
                return dt;
            }

            var tvpIds = ToTvp(SelectedCourseCompletedIds);
            var user = User?.Identity?.Name ?? "Unknown";

            try
            {
                string cs = _context.Database.GetDbConnection().ConnectionString;
                using var conn = new SqlConnection(cs);
                using var cmd = new SqlCommand("dbo.sp_BulkDeleteCourseCompleted", conn)
                { CommandType = CommandType.StoredProcedure };

                var pIds = cmd.Parameters.AddWithValue("@CourseCompletedIds", tvpIds);
                pIds.SqlDbType = SqlDbType.Structured;
                pIds.TypeName = "dbo.IntIdList";

                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = user });
                cmd.Parameters.Add(new SqlParameter("@SoftDelete", SqlDbType.Bit) { Value = SoftDelete });

                conn.Open();
                int affected = cmd.ExecuteNonQuery();

                if (affected == 0)
                    TempData["ErrorMessage"] = "No se eliminaron registros (verifica selección y filtros).";
                else
                    TempData["SuccessMessage"] = $"Operación completada. Filas afectadas: {affected}.";
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al eliminar los completados masivos.";
            }

            // Volver con los mismos filtros (solo curso y nivel)
            return RedirectToAction(nameof(DeleteCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkLevel });
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
