using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public async Task<IActionResult> Index()
        {
            var lista = new List<CourseCompletedViewModel>();

            // Obtener el connection string desde el contexto de EF Core
            var connectionString = _context.Database.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("dbo.sp_GetCourseCompleted", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new CourseCompletedViewModel
                            {
                                PkCourseCompleted = reader.GetInt32(reader.GetOrdinal("PK_CourseCompleted")),
                                FkCourseAssignment = reader.GetInt32(reader.GetOrdinal("FK_CourseAssignment")),
                                FkCourseStatus = reader.GetInt32(reader.GetOrdinal("FK_CourseStatus")),
                                FkDeliveryMode = reader.GetInt32(reader.GetOrdinal("FK_DeliveryMode")),
                                FkHeadcount = reader.GetInt32(reader.GetOrdinal("FK_Headcount")),
                                Score = reader.GetInt32(reader.GetOrdinal("Score")),
                                CreateUser = reader.GetString(reader.GetOrdinal("CreateUser")),
                                CreateDate = reader.GetDateTime(reader.GetOrdinal("CreateDate")),
                                LastUpdateUser = reader.GetString(reader.GetOrdinal("LastUpdateUser")),
                                LastUpdateDate = reader.GetDateTime(reader.GetOrdinal("LastUpdateDate")),
                                Avaialble = reader.GetInt32(reader.GetOrdinal("Avaialble"))
                            };

                            lista.Add(item);
                        }
                    }
                }
            }

            return View(lista);
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult IndexCourseCompleted()
        {
            var rows = new List<CourseCompletedSummaryItemViewModel>();
            var cs = _context.Database.GetDbConnection().ConnectionString;

            using var conn = new SqlConnection(cs);
            using var cmd = new SqlCommand("dbo.sp_IndexCourseCompletedSummary", conn)
            { CommandType = CommandType.StoredProcedure };

            conn.Open();
            using var rdr = cmd.ExecuteReader();

            int ordCourse = rdr.GetOrdinal("CourseName");
            int ordLevel = rdr.GetOrdinal("LevelName");
            int ordDeliv = rdr.GetOrdinal("DeliveryModeName");
            int ordStat = rdr.GetOrdinal("Course_Status");
            int ordTotal = rdr.GetOrdinal("TotalCompletions");

            while (rdr.Read())
            {
                rows.Add(new CourseCompletedSummaryItemViewModel
                {
                    CourseName = rdr.IsDBNull(ordCourse) ? "" : rdr.GetString(ordCourse),
                    LevelName = rdr.IsDBNull(ordLevel) ? "" : rdr.GetString(ordLevel),
                    DeliveryModeName = rdr.IsDBNull(ordDeliv) ? "" : rdr.GetString(ordDeliv),
                    Course_Status = rdr.IsDBNull(ordStat) ? "" : rdr.GetString(ordStat),
                    TotalCompletions = rdr.IsDBNull(ordTotal) ? 0 : rdr.GetInt32(ordTotal)
                });
            }

            return View(rows);
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

        private void LoadCourseCompletedBulkViewBags(int selectedFkCourse, int selectedFkLevel)
        {
            // --- Courses (solo disponibles)
            ViewBag.Courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .Select(c => new { c.PkCourse, c.CourseName })
                .ToList();

            // --- Levels (solo disponibles)
            ViewBag.Levels = _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .Select(l => new { l.PkLevelcourse, l.DescripctionLevel })
                .ToList();

            // --- Headcounts:
            //     Solo los disponibles (Available=1) que además tengan al menos un CourseAssignment disponible (Available=1)
            //     que conecte su FK_Position con el curso y nivel seleccionados.
            // --- Headcounts:
            ViewBag.Headcounts = _context.SyHeadcounts
                .AsNoTracking()
                .Where(h =>
                    h.Available == 1
                    // Debe existir un CourseAssignment disponible que conecte curso + nivel + posición
                    && _context.CtCourseassignments.Any(ca =>
                           ca.Available == 1
                           && ca.FkCourse == selectedFkCourse
                           && ca.FkRequiredCourseLevels == selectedFkLevel
                           && ca.FkPosition == h.FkPosition)
                    // Y NO debe existir ya un CourseCompleted (activo) para ese headcount
                    && !_context.SyCoursecompleteds.Any(cc =>
                           cc.Avaialble == 1
                           && cc.FkHeadcount == h.PkHeadcount
                           && _context.CtCourseassignments.Any(ca2 =>
                                  ca2.PkCourseAssignment == cc.FkCourseAssignment
                                  && ca2.FkCourse == selectedFkCourse
                                  && ca2.FkRequiredCourseLevels == selectedFkLevel))
                )
                .Select(h => new
                {
                    h.PkHeadcount,

                    PositionName = _context.CtPositions
                        .AsNoTracking()
                        .Where(p => p.PkPosition == h.FkPosition)
                        .Select(p => p.NamePosition)
                        .FirstOrDefault(),

                    CourseAssignmentId = _context.CtCourseassignments
                        .Where(ca => ca.Available == 1
                                     && ca.FkCourse == selectedFkCourse
                                     && ca.FkRequiredCourseLevels == selectedFkLevel
                                     && ca.FkPosition == h.FkPosition)
                        .OrderBy(ca => ca.PkCourseAssignment)
                        .Select(ca => ca.PkCourseAssignment)
                        .FirstOrDefault(),

                    Display = (
                        (h.ControlNumber + " - " +
                         h.Names + " " + (h.LastName ?? "") + " " + (h.SecondName ?? "")).Trim()
                        + " | " +
                        (_context.CtPositions
                            .Where(p => p.PkPosition == h.FkPosition)
                            .Select(p => p.NamePosition)
                            .FirstOrDefault() ?? "Sin posición")
                    )
                })
                .OrderBy(x => x.Display)
                .ToList();

            // --- Course Status (solo disponibles)
            ViewBag.CourseStatus = _context.CtCoursestatuses
                .AsNoTracking()
                .Where(s => s.Available == 1)
                .OrderBy(s => s.DescriptionCoursestatus)
                .Select(s => new { s.PkCoursestatus, s.DescriptionCoursestatus })
                .ToList();

            // --- Delivery Modes (solo disponibles)
            ViewBag.DeliveryModes = _context.CtDeliverymodes
                .AsNoTracking()
                .Where(d => d.Available == 1)
                .OrderBy(d => d.DescriptionDeliverymode)
                .Select(d => new { Id = d.PkDeliverymode, Text = d.DescriptionDeliverymode })
                .ToList();
        }

        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            LoadCourseCompletedBulkViewBags(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateCourseCompletedBulk(
            int FkCourse,
            int FkRequiredCourseLevels,
            int[] SelectedHeadcounts,
            bool allowUpsert
            = true)
        {
            if (FkCourse <= 0 || FkRequiredCourseLevels <= 0 || SelectedHeadcounts == null || SelectedHeadcounts.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona curso, nivel y al menos una persona.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
            }

            // Headcounts que YA tienen CourseCompleted (activo) para este curso+nivel
            var alreadyCompletedHc = (
                from cc in _context.SyCoursecompleteds.AsNoTracking()
                join ca in _context.CtCourseassignments.AsNoTracking()
                    on cc.FkCourseAssignment equals ca.PkCourseAssignment
                where cc.Avaialble == 1
                   && ca.FkCourse == FkCourse
                   && ca.FkRequiredCourseLevels == FkRequiredCourseLevels
                select cc.FkHeadcount
            ).ToHashSet();

            // Quita de la selección los ya completados
            var filtered = SelectedHeadcounts.Distinct().Where(hc => !alreadyCompletedHc.Contains(hc)).ToArray();

            if (filtered.Length == 0)
            {
                TempData["ErrorMessage"] = "Todas las personas seleccionadas ya tienen el curso completado para ese nivel.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
            }

            // TVP dbo.IntIdList(Id INT)
            var tvp = new System.Data.DataTable();
            tvp.Columns.Add("Id", typeof(int));
            foreach (var id in filtered) tvp.Rows.Add(id);

            var pHeadcounts = new Microsoft.Data.SqlClient.SqlParameter("@Headcounts", tvp)
            {
                SqlDbType = System.Data.SqlDbType.Structured,
                TypeName = "dbo.IntIdList"
            };
            var pFkCourse = new Microsoft.Data.SqlClient.SqlParameter("@FkCourse", FkCourse);
            var pFkLevel = new Microsoft.Data.SqlClient.SqlParameter("@FkLevel", FkRequiredCourseLevels);
            var pUserName = new Microsoft.Data.SqlClient.SqlParameter("@UserName", User?.Identity?.Name ?? "system");
            var pAllowUpsert = new Microsoft.Data.SqlClient.SqlParameter("@AllowUpsert", allowUpsert);

            var sql = "EXEC dbo.sp_BulkCreateCourseCompleted_ByCourse @Headcounts, @FkCourse, @FkLevel, @UserName, @AllowUpsert";
            await _context.Database.ExecuteSqlRawAsync(sql, pHeadcounts, pFkCourse, pFkLevel, pUserName, pAllowUpsert);

            TempData["SuccessMessage"] = "Registros creados/actualizados correctamente";
            return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
        }

        /// <summary>
        /// Pobla combos (Courses/Levels) con Available=1.
        /// </summary>
        private void LoadDeleteCourseCompletedBulkViewBags(int selectedFkCourse, int selectedFkLevel)
        {
            var courses = _context.CtCourses!
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .Select(c => new { c.PkCourse, c.CourseName })
                .ToList();

            var levels = _context.CtLevelcourses!
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .Select(l => new { l.PkLevelcourse, l.DescripctionLevel })
                .ToList();

            ViewBag.Courses = courses
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.PkCourse.ToString(),
                    Text = c.CourseName,
                    Selected = c.PkCourse == selectedFkCourse
                })
                .ToList();

            ViewBag.Levels = levels
                .Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = l.PkLevelcourse.ToString(),
                    Text = l.DescripctionLevel,
                    Selected = l.PkLevelcourse == selectedFkLevel
                })
                .ToList();
        }

        // GET: DeleteCourseCompletedBulk  (LISTA con EF/LINQ — SIN SP)
        [Authorize(Roles = "Administrador")]
        public IActionResult DeleteCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            // Combos
            LoadDeleteCourseCompletedBulkViewBags(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            // Requerir curso + nivel
            bool needFilters = (selectedFkCourse <= 0 || selectedFkLevel <= 0);
            ViewBag.NeedFilters = needFilters;

            var rows = new List<CourseCompletedRowDtoViewModel>();

            if (!needFilters)
            {
                // Construimos la lista con EF (joins) — solo disponibles
                var query =
                    from cc in _context.SyCoursecompleteds.AsNoTracking()
                    join ca in _context.CtCourseassignments.AsNoTracking()
                        on cc.FkCourseAssignment equals ca.PkCourseAssignment
                    join c in _context.CtCourses.AsNoTracking()
                        on ca.FkCourse equals c.PkCourse
                    join lc in _context.CtLevelcourses.AsNoTracking()
                        on ca.FkRequiredCourseLevels equals lc.PkLevelcourse
                    join cs in _context.CtCoursestatuses.AsNoTracking()
                        on cc.FkCourseStatus equals cs.PkCoursestatus
                    join dm in _context.CtDeliverymodes.AsNoTracking()
                        on cc.FkDeliveryMode equals dm.PkDeliverymode
                    join h in _context.SyHeadcounts.AsNoTracking()
                        on cc.FkHeadcount equals h.PkHeadcount
                    where
                        // disponibles
                        cc.Avaialble == 1
                        && (c.Available == 1 || c.Available == null)
                        && (ca.Available == 1 || ca.Available == null)
                        && (lc.Available == 1 || lc.Available == null)
                        && (cs.Available == 1 || cs.Available == null)
                        && (dm.Available == 1 || dm.Available == null)
                        && (h.Available == 1 || h.Available == null)
                        // filtros obligatorios
                        && ca.FkCourse == selectedFkCourse
                        && ca.FkRequiredCourseLevels == selectedFkLevel
                    select new
                    {
                        cc.PkCourseCompleted,
                        cc.Score,
                        cc.CreateDate,
                        HeadcountControlNumber = h.ControlNumber,
                        HeadcountFullName = (
                            (h.Names ?? "") + " "
                            + (h.LastName ?? "") + " "
                            + (h.SecondName ?? "")
                        ).Trim(),
                        CourseName = c.CourseName,
                        LevelName = lc.DescripctionLevel,
                        StatusName = cs.DescriptionCoursestatus,
                        DeliveryModeName = dm.DescriptionDeliverymode
                    };

                rows = query
                    .OrderBy(x => x.CourseName)
                    .ThenByDescending(x => x.CreateDate)
                    .Select(x => new CourseCompletedRowDtoViewModel
                    {
                        PkCourseCompleted = x.PkCourseCompleted,
                        HeadcountControlNumber = x.HeadcountControlNumber.ToString() ?? "",
                        HeadcountFullName = x.HeadcountFullName ?? "",
                        CourseName = x.CourseName ?? "",
                        LevelName = x.LevelName ?? "",
                        StatusName = x.StatusName ?? "",
                        DeliveryModeName = x.DeliveryModeName ?? "",
                        Score = x.Score,
                        CreateDate = x.CreateDate
                    })
                    .ToList();
            }

            ViewBag.CourseCompletedRows = rows;
            return View();
        }

        // POST: DeleteCourseCompletedBulk (BORRADO con SP + TVP, igual que ya tenías)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseCompletedBulk(
            [FromForm] int[] SelectedCourseCompletedIds,
            [FromForm] bool SoftDelete,
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
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error al eliminar los completados masivos. Detalle: {ex.Message}";
            }

            return RedirectToAction(nameof(DeleteCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkLevel });
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
