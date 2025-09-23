using ClosedXML;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;
using System.Data;
using static RH_CM.ViewModels.ViewModels;

namespace RH_CM.Controllers
{
    public class TrainifyController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TrainifyController(db_abcd61_rhchdbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        // Helper: combo de posiciones
        private async Task<List<SelectListItem>> GetPositionsAsync()
        {
            return await _context.CtPositions
                .AsNoTracking()
                .Where(p => p.Available == 1)
                .OrderBy(p => p.NamePositionEnglish)
                .Select(p => new SelectListItem
                {
                    Value = p.PkPosition.ToString(),
                    Text = $"{p.NamePositionEnglish} (#{p.PkPosition})"
                })
                .ToListAsync();
        }

        // ------------------- Ventana A: Selector -------------------

        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> MatrizbyPositionSelectPosition()
        {
            var vm = new SelectPositionViewModel
            {
                Positions = await GetPositionsAsync()
            };
            return View(vm); // View: MatrizbyPositionSelectPosition.cshtml
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MatrizbyPositionSelectPosition(SelectPositionViewModel vm)
        {
            if (!vm.SelectedPositionId.HasValue || vm.SelectedPositionId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Selecciona una posición válida.";
                vm.Positions = await GetPositionsAsync();
                return View(vm);
            }

            return RedirectToAction(nameof(MatrizByPosition), new { fkPosition = vm.SelectedPositionId.Value });
        }

        // ------------------- Ventana B: Matriz (SOLO muestra) -------------------

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MatrizByPosition(int fkPosition)
        {
            if (fkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Primero selecciona una posición.";
                return RedirectToAction(nameof(MatrizbyPositionSelectPosition));
            }

            var pageVm = new MatrizByPositionPageViewModel
            {
                SelectedPositionId = fkPosition
            };

            var results = new List<MatrizByPositionViewModel>();

            try
            {
                string connectionString = _context.Database.GetDbConnection().ConnectionString;

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("sp_GetMatrizbyPositionCourseAssignments", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@FkPosition", SqlDbType.Int) { Value = fkPosition });

                await using var reader = await command.ExecuteReaderAsync();

                int Ord(string n) => reader.GetOrdinal(n);
                bool IsNull(string n) => reader.IsDBNull(Ord(n));

                while (await reader.ReadAsync())
                {
                    results.Add(new MatrizByPositionViewModel
                    {
                        FK_Position = IsNull("FK_Position") ? 0 : reader.GetInt32(Ord("FK_Position")),
                        NAME_POSITION_ENGLISH = IsNull("NAME_POSITION_ENGLISH") ? "—" : reader.GetString(Ord("NAME_POSITION_ENGLISH")),
                        FK_Course = IsNull("FK_Course") ? 0 : reader.GetInt32(Ord("FK_Course")),
                        CourseName = IsNull("CourseName") ? "—" : reader.GetString(Ord("CourseName")),
                        FK_RequiredCourseLevels = IsNull("FK_RequiredCourseLevels") ? 0 : reader.GetInt32(Ord("FK_RequiredCourseLevels")),
                        DESCRIPCTION_LEVEL = IsNull("DESCRIPCTION_LEVEL") ? null : reader.GetString(Ord("DESCRIPCTION_LEVEL")),
                        Requiered = !IsNull("Requiered") && Convert.ToBoolean(reader["Requiered"]),
                        FK_DeliveryMode = IsNull("FK_DeliveryMode") ? 0 : Convert.ToInt32(reader["FK_DeliveryMode"]),
                        CourseValidityDays = IsNull("CourseValidityDays") ? (int?)null : Convert.ToInt32(reader["CourseValidityDays"])
                    });
                }

                pageVm.Results = results;
                pageVm.PositionNameEnglish = results.FirstOrDefault()?.NAME_POSITION_ENGLISH;

                if (!results.Any())
                    TempData["ErrorMessage"] = $"No hay cursos asignados para la posición {fkPosition}.";
                else
                    TempData["SuccessMessage"] = $"Se encontraron {results.Count} asignaciones para la posición {pageVm.PositionNameEnglish} ({fkPosition}).";
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"Error SQL al consultar: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ocurrió un error: {ex.Message}";
            }

            return View(pageVm); // View: MatrizByPosition.cshtml (solo muestra)
        }

        [Authorize]
        public async Task<IActionResult> MatrizbyEmployee()
        {
            var result = new List<MatrizByEmployeeViewModel>();
            var userName = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName))
            {
                TempData["ErrorMessage"] = "No se pudo obtener el usuario actual.";
                return View(result);
            }

            string connectionString = _context.Database.GetDbConnection().ConnectionString;

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("sp_GetMatrizbyEmployeeCourseAssignments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = userName });

            await using var reader = await command.ExecuteReaderAsync();

            // Helpers locales
            int Ord(string n) => reader.GetOrdinal(n);
            bool IsNull(string n) => reader.IsDBNull(Ord(n));

            while (await reader.ReadAsync())
            {
                result.Add(new MatrizByEmployeeViewModel
                {
                    FK_Position = IsNull("FK_Position") ? 0 : reader.GetInt32(Ord("FK_Position")),
                    NAME_POSITION_ENGLISH = IsNull("NAME_POSITION_ENGLISH") ? "—" : reader.GetString(Ord("NAME_POSITION_ENGLISH")),

                    FK_Course = IsNull("FK_Course") ? 0 : reader.GetInt32(Ord("FK_Course")),
                    CourseName = IsNull("CourseName") ? "—" : reader.GetString(Ord("CourseName")),

                    FK_RequiredCourseLevels = IsNull("FK_RequiredCourseLevels") ? 0 : reader.GetInt32(Ord("FK_RequiredCourseLevels")),
                    DESCRIPCTION_LEVEL = IsNull("DESCRIPCTION_LEVEL") ? null : reader.GetString(Ord("DESCRIPCTION_LEVEL")),

                    Requiered = !IsNull("Requiered") && Convert.ToBoolean(reader["Requiered"]),
                    FK_DeliveryMode = IsNull("FK_DeliveryMode") ? 0 : Convert.ToInt32(reader["FK_DeliveryMode"]),
                    // DESCRIPTION_DELIVERYMODE -> no viene en el SP actual

                    CourseValidityDays = IsNull("CourseValidityDays") ? (int?)null : Convert.ToInt32(reader["CourseValidityDays"]),

                    UserName = IsNull("UserName") ? userName : reader["UserName"]?.ToString() ?? userName,
                    CONTROL_NUMBER = IsNull("CONTROL_NUMBER") ? "—" : reader["CONTROL_NUMBER"]?.ToString() ?? "—",
                    NAMES = IsNull("NAMES") ? "—" : reader["NAMES"]?.ToString() ?? "—",
                    LAST_NAME = IsNull("LAST_NAME") ? "—" : reader["LAST_NAME"]?.ToString() ?? "—",
                    SECOND_NAME = IsNull("SECOND_NAME") ? "—" : reader["SECOND_NAME"]?.ToString() ?? "—",

                    LastUpdateDate = IsNull("LastUpdateDate") ? (DateTime?)null : Convert.ToDateTime(reader["LastUpdateDate"]),
                    CourseStatus = IsNull("CourseStatus") ? "—" : reader["CourseStatus"]?.ToString() ?? "—"
                });
            }

            return View(result);
        }

        // GET: IndexCourseCompleted
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

        // GET: CreateCourseCompleted
        public async Task<IActionResult> CreateCourseCompleted()
        {
            var vm = new RH_CM.ViewModels.SyCourseCompletedVM();
            await PopulateSelects(vm);
            return View(vm);
        }

        // POST: CreateCourseCompleted
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseCompleted(RH_CM.ViewModels.SyCourseCompletedVM vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelects(vm);
                return View(vm);
            }

            var now = DateTime.Now;
            var user = User?.Identity?.Name ?? "system";

            var entity = new RH_CM.Models.SyCoursecompleted
            {
                FkCourseAssignment = vm.FkCourseAssignment,
                FkCourseStatus = vm.FkCourseStatus,
                FkDeliveryMode = vm.FkDeliveryMode, // 0 = Not Assigned permitido
                FkHeadcount = vm.FkHeadcount,

                // 🔒 Auditoría por defecto (no se pide al usuario)
                CreateUser = user,
                CreateDate = now,
                LastUpdateUser = user,
                LastUpdateDate = now,
                Avaialble = 1 // por defecto habilitado
            };

            _context.SyCoursecompleteds.Add(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registro creado correctamente.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        // GET: EditCourseCompleted/5
        public async Task<IActionResult> EditCourseCompleted(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = await ToViewModel(entity);
            await PopulateSelects(vm);
            return View(vm);
        }

        // POST: EditCourseCompleted/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseCompleted(int id, RH_CM.ViewModels.SyCourseCompletedVM vm)
        {
            if (id != vm.PkCourseCompleted) return BadRequest();

            if (!ModelState.IsValid)
            {
                await PopulateSelects(vm);
                return View(vm);
            }

            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            // Actualiza SOLO campos editables
            entity.FkCourseAssignment = vm.FkCourseAssignment;
            entity.FkCourseStatus = vm.FkCourseStatus;
            entity.FkDeliveryMode = vm.FkDeliveryMode; // 0 permitido
            entity.FkHeadcount = vm.FkHeadcount;

            // 🔒 Auditoría auto
            entity.LastUpdateUser = User?.Identity?.Name ?? "system";
            entity.LastUpdateDate = DateTime.Now;

            // 🔒 NO tocar CreateUser/CreateDate ni Avaialble aquí (no se piden en UI)
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registro actualizado correctamente.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        // GET: DeleteCourseCompleted/5
        public async Task<IActionResult> DeleteCourseCompleted(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = await ToViewModel(entity);
            return View(vm);
        }

        // POST: DeleteCourseCompleted/5
        [HttpPost, ActionName("DeleteCourseCompleted"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseCompletedConfirmed(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            _context.SyCoursecompleteds.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Registro eliminado correctamente.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        // Helpers (ToViewModel y PopulateSelects igual que antes)
        private async Task<SyCourseCompletedVM> ToViewModel(SyCoursecompleted e)
        {
            return new SyCourseCompletedVM
            {
                PkCourseCompleted = e.PkCourseCompleted,
                FkCourseAssignment = e.FkCourseAssignment,
                FkCourseStatus = e.FkCourseStatus,
                FkDeliveryMode = e.FkDeliveryMode,
                FkHeadcount = e.FkHeadcount,
                CreateUser = e.CreateUser,
                CreateDate = e.CreateDate,
                LastUpdateUser = e.LastUpdateUser,
                LastUpdateDate = e.LastUpdateDate,
                Avaialble = e.Avaialble
            };
        }

        private async Task PopulateSelects(SyCourseCompletedVM vm)
        {
            var caList = await _context.CtCourseassignments
                .OrderBy(x => x.PkCourseAssignment)
                .Select(x => new
                {
                    x.PkCourseAssignment,
                    Text = $"#{x.PkCourseAssignment} - Course:{x.FkCourse} Level:{x.FkRequiredCourseLevels}"
                })
                .ToListAsync();

            var csList = await _context.CtCoursestatuses
                .Where(x => x.Available == 1)
                .OrderBy(x => x.DescriptionCoursestatus)
                .ToListAsync();

            var dmList = await _context.CtDeliverymodes
                .Where(x => x.Available == 1)
                .OrderBy(x => x.DescriptionDeliverymode)
                .ToListAsync();
            dmList.Insert(0, new CtDeliverymode { PkDeliverymode = 0, DescriptionDeliverymode = "Not Assigned" });

            var hcList = await _context.SyHeadcounts
                .Where(x => x.Available == 1)
                .OrderBy(x => x.Names)
                .Select(x => new
                {
                    x.PkHeadcount,
                    Text = $"{x.ControlNumber} - {x.Names} {x.LastName ?? ""} {x.SecondName ?? ""}".Trim()
                })
                .ToListAsync();

            vm.CourseAssignments = new SelectList(caList, "PkCourseAssignment", "Text", vm.FkCourseAssignment);
            vm.CourseStatuses = new SelectList(csList, "PkCoursestatus", "DescriptionCoursestatus", vm.FkCourseStatus);
            vm.DeliveryModes = new SelectList(dmList, "PkDeliverymode", "DescriptionDeliverymode", vm.FkDeliveryMode);
            vm.Headcounts = new SelectList(hcList, "PkHeadcount", "Text", vm.FkHeadcount);
        }

        // GET: descarga template
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("CourseCompleted");

            // Headers
            ws.Cell(1, 1).Value = "RowNum";
            ws.Cell(1, 2).Value = "CourseAssignmentId";
            ws.Cell(1, 3).Value = "CourseStatusId";
            ws.Cell(1, 4).Value = "DeliveryModeId";
            ws.Cell(1, 5).Value = "ControlNumber";
            ws.Cell(1, 6).Value = "Available";
            ws.Cell(1, 7).Value = "CreateUser";
            ws.Cell(1, 8).Value = "LastUpdateUser";

            ws.Range("A1:H1").Style.Font.Bold = true;
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CourseCompleted_Import_Template.xlsx");
        }

        // GET: página de carga
        [HttpGet]
        public IActionResult Index()
        {
            return View(); // muestra formulario de upload
        }

        // POST: carga Excel y llama SP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, bool allowUpsert = true)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Selecciona un archivo Excel.";
                return RedirectToAction(nameof(Index));
            }

            DataTable tvp = BuildTvpSchema(); // DataTable con columnas del type
            try
            {
                using var stream = file.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault(x => x.Name.Equals("CourseCompleted", StringComparison.OrdinalIgnoreCase))
                         ?? wb.Worksheet(1);

                int row = 2; // empieza datos
                while (!ws.Row(row).IsEmpty())
                {
                    int RowNum = GetInt(ws.Cell(row, 1).GetValue<string>());
                    int CourseAssignmentId = GetInt(ws.Cell(row, 2).GetValue<string>());
                    int CourseStatusId = GetInt(ws.Cell(row, 3).GetValue<string>());
                    int DeliveryModeId = GetInt(ws.Cell(row, 4).GetValue<string>()); // 0 permitido
                    int ControlNumber = GetInt(ws.Cell(row, 5).GetValue<string>());
                    int Available = GetInt(ws.Cell(row, 6).GetValue<string>());
                    string CreateUser = ws.Cell(row, 7).GetValue<string>()?.Trim();
                    string LastUpdateUser = ws.Cell(row, 8).GetValue<string>()?.Trim();

                    var dr = tvp.NewRow();
                    dr["RowNum"] = RowNum > 0 ? RowNum : row - 1; // fallback: # de fila
                    dr["CourseAssignmentId"] = CourseAssignmentId;
                    dr["CourseStatusId"] = CourseStatusId;
                    dr["DeliveryModeId"] = DeliveryModeId;
                    dr["ControlNumber"] = ControlNumber;
                    dr["Available"] = Available;
                    dr["CreateUser"] = (object?)CreateUser ?? DBNull.Value;
                    dr["LastUpdateUser"] = (object?)LastUpdateUser ?? DBNull.Value;
                    tvp.Rows.Add(dr);

                    row++;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error leyendo el Excel: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

            // Llamar SP
            string userName = User?.Identity?.Name ?? "system";
            string cs = _context.Database.GetDbConnection().ConnectionString;

            var errors = new DataTable();
            var summary = new DataTable();

            try
            {
                using var cn = new SqlConnection(cs);
                await cn.OpenAsync();

                using var cmd = new SqlCommand("dbo.sp_BulkUpsertCourseCompleted", cn);
                cmd.CommandType = CommandType.StoredProcedure;

                var pTvp = cmd.Parameters.AddWithValue("@Rows", tvp);
                pTvp.SqlDbType = SqlDbType.Structured;
                pTvp.TypeName = "dbo.CourseCompletedImportType";
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = userName });
                cmd.Parameters.Add(new SqlParameter("@AllowUpsert", SqlDbType.Bit) { Value = allowUpsert });

                using var da = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables.Count > 0) errors = ds.Tables[0];
                if (ds.Tables.Count > 1) summary = ds.Tables[1];

                TempData["SuccessMessage"] = BuildSummaryMessage(summary, errors);
                TempData["ErrorTable"] = DataTableToHtml(errors); // para mostrar en vista
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error procesando la carga: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private static DataTable BuildTvpSchema()
        {
            var dt = new DataTable();
            dt.Columns.Add("RowNum", typeof(int));
            dt.Columns.Add("CourseAssignmentId", typeof(int));
            dt.Columns.Add("CourseStatusId", typeof(int));
            dt.Columns.Add("DeliveryModeId", typeof(int));
            dt.Columns.Add("ControlNumber", typeof(int));
            dt.Columns.Add("Available", typeof(int));
            dt.Columns.Add("CreateUser", typeof(string));
            dt.Columns.Add("LastUpdateUser", typeof(string));
            return dt;
        }

        private static int GetInt(string s)
        {
            return int.TryParse(s?.Trim(), out var v) ? v : 0;
        }

        private static string BuildSummaryMessage(DataTable summary, DataTable errors)
        {
            int inserted = 0, updated = 0, err = 0;
            if (summary != null && summary.Rows.Count > 0)
            {
                inserted = summary.Columns.Contains("InsertedCount") ? Convert.ToInt32(summary.Rows[0]["InsertedCount"]) : 0;
                updated = summary.Columns.Contains("UpdatedCount") ? Convert.ToInt32(summary.Rows[0]["UpdatedCount"]) : 0;
            }
            if (errors != null) err = errors.Rows.Count;
            return $"Carga completada: Insertados={inserted}, Actualizados={updated}, Errores={err}.";
        }

        private static string DataTableToHtml(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return string.Empty;
            using var sw = new StringWriter();
            sw.Write("<table class='table table-sm table-bordered'><thead><tr>");
            foreach (DataColumn c in dt.Columns) sw.Write($"<th>{c.ColumnName}</th>");
            sw.Write("</tr></thead><tbody>");
            foreach (DataRow r in dt.Rows)
            {
                sw.Write("<tr>");
                foreach (DataColumn c in dt.Columns) sw.Write($"<td>{System.Net.WebUtility.HtmlEncode(r[c]?.ToString())}</td>");
                sw.Write("</tr>");
            }
            sw.Write("</tbody></table>");
            return sw.ToString();
        }


        /// <summary>
        /// PDF READER
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="levelId"></param>
        /// <returns></returns>

        private async Task<bool> ExistsDiagnosticTestAsync(int courseId, int levelId)
        {
            return await _context.CtTests
                .AsNoTracking()
                .AnyAsync(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId);
        }

        private async Task<bool> ExistsMaterialAsync(int courseId, int levelId)
        {
            return await (
                from clm in _context.CtCourseLevelMaterials.AsNoTracking()
                join m in _context.CtCoursematerials.AsNoTracking()
                    on clm.FkCourseMaterial equals m.PkCoursematerial
                where clm.Available == 1
                   && clm.FkCourse == courseId
                   && clm.FkLevelCourse == levelId
                   && (m.Available ?? 0) == 1
                   && ((m.File != null && m.File.Length > 0) || !string.IsNullOrWhiteSpace(m.UrlPath))
                select clm.PkCourseLevelMaterial
            ).AnyAsync();
        }

        [Authorize]
        [HttpGet("/Catalog/StartCourse")]
        public async Task<IActionResult> StartCourse([FromQuery] int courseId, [FromQuery] int levelId)
        {
            if (!await ExistsDiagnosticTestAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = "No diagnostic test found for this course/level. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (!await ExistsMaterialAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = "No course material (PDF/URL) linked to this course/level. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var test = await _context.CtTests.AsNoTracking()
                .Where(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId)
                .OrderByDescending(t => t.PkTest)
                .FirstOrDefaultAsync();

            return RedirectToAction("Diagnostic", "Trainify", new { id = test!.PkTest, courseId, levelId });
        }

        [Authorize]
        public async Task<IActionResult> LearningTrainify()
        {
            var result = new List<LearningCourseToDoViewModel>();
            var userName = User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userName))
            {
                TempData["ErrorMessage"] = "No se pudo obtener el usuario actual.";
                return View(result);
            }

            string connectionString = _context.Database.GetDbConnection().ConnectionString;

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand("sp_GetCoursestoDo_Learning", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = userName });

                using var reader = await command.ExecuteReaderAsync();

                int Ord(string n) => reader.GetOrdinal(n);
                bool IsNull(string n) => reader.IsDBNull(Ord(n));
                bool HasCol(string n)
                {
                    var schema = reader.GetSchemaTable();
                    if (schema == null) return false;
                    foreach (DataRow r in schema.Rows)
                        if (string.Equals(r["ColumnName"]?.ToString(), n, StringComparison.OrdinalIgnoreCase))
                            return true;
                    return false;
                }

                while (await reader.ReadAsync())
                {
                    var vm = new LearningCourseToDoViewModel
                    {
                        PK_CourseAssignment = IsNull("PK_CourseAssignment") ? 0 : reader.GetInt32(Ord("PK_CourseAssignment")),
                        //FK_Position = IsNull("FK_Position") ? 0 : reader.GetInt32(Ord("FK_Position")),
                        NAME_POSITION_ENGLISH = IsNull("NAME_POSITION_ENGLISH") ? "—" : reader.GetString(Ord("NAME_POSITION_ENGLISH")),

                        FK_Course = HasCol("FK_Course") && !IsNull("FK_Course") ? reader.GetInt32(Ord("FK_Course")) : 0,
                        CourseName = IsNull("CourseName") ? "—" : reader.GetString(Ord("CourseName")),
                        CourseLevel = HasCol("CourseLevel") && !IsNull("CourseLevel") ? reader.GetString(Ord("CourseLevel")) : "—",
                        FK_RequiredCourseLevels = HasCol("FK_RequiredCourseLevels") && !IsNull("FK_RequiredCourseLevels") ? reader.GetInt32(Ord("FK_RequiredCourseLevels")) : 0, // 👈 mapeo nuevo

                        DeliveryMode = HasCol("DeliveryMode") && !IsNull("DeliveryMode") ? reader.GetString(Ord("DeliveryMode")) : "—",
                        CourseValidityDays = HasCol("CourseValidityDays") && !IsNull("CourseValidityDays") ? Convert.ToInt32(reader["CourseValidityDays"]) : (int?)null,

                        //UserName = IsNull("UserName") ? userName : reader["UserName"].ToString()!,
                        CONTROL_NUMBER = HasCol("CONTROL_NUMBER") && !IsNull("CONTROL_NUMBER") ? reader["CONTROL_NUMBER"].ToString()! : "—",
                        FullName = HasCol("FullName") && !IsNull("FullName") ? reader["FullName"].ToString()! : "—",

                        LastUpdateDate = HasCol("LastUpdateDate") && !IsNull("LastUpdateDate") ? Convert.ToDateTime(reader["LastUpdateDate"]) : (DateTime?)null,
                        CourseStatus = HasCol("CourseStatus") && !IsNull("CourseStatus") ? reader["CourseStatus"].ToString()! : "—"
                    };

                    result.Add(vm);
                }

                if (result.Count == 0)
                    TempData["SuccessMessage"] = "No hay cursos pendientes requeridos para mostrar.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al cargar cursos: {ex.Message}";
            }

            return View(result);
        }

        // (ÚNICA ruta para abrir PDF en una vista con navegación/iframe)
        [HttpGet("/Catalog/OpenPdfCourseMaterialByCourseLevel")]
        public IActionResult OpenPdfCourseMaterialByCourseLevel([FromQuery] int courseId, [FromQuery] int levelId)
        {
            var streamAbs = Url.Action(
                nameof(StreamPdfCourseMaterialByCourseLevel),
                "Catalog",
                new { courseId, levelId },
                protocol: Request.Scheme // <<--- ABSOLUTO (http/https + host)
            ) ?? string.Empty;

            var vm = new RH_CM.ViewModels.CourseMaterialViewerViewModel
            {
                CourseId = courseId,
                LevelId = levelId,
                StreamUrl = streamAbs
            };
            return View("OpenPdfCourseMaterialByCourseLevel", vm);
        }

        [Authorize]
        [HttpGet("/Catalog/StreamPdfCourseMaterialByCourseLevel")]
        public async Task<IActionResult> StreamPdfCourseMaterialByCourseLevel(int courseId, int levelId)
        {
            var link = await _context.CtCourseLevelMaterials
                .AsNoTracking()
                .Where(x => x.Available == 1 && x.FkCourse == courseId && x.FkLevelCourse == levelId)
                .OrderByDescending(x => x.PkCourseLevelMaterial)
                .FirstOrDefaultAsync();

            if (link == null)
            {
                TempData["ErrorMessage"] = "No course material is linked to this course/level. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var material = await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PkCoursematerial == link.FkCourseMaterial && (m.Available ?? 0) == 1);

            if (material == null)
            {
                TempData["ErrorMessage"] = "Course material not found or unavailable. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (material.File != null && material.File.Length > 0)
                return File(material.File, "application/pdf");

            if (!string.IsNullOrWhiteSpace(material.UrlPath))
                return Redirect(material.UrlPath);

            TempData["ErrorMessage"] = "The material exists but has no file or URL. Please contact HR.";
            return RedirectToAction("LearningTrainify", "Trainify");
        }

        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        [HttpGet]
        public async Task<IActionResult> Diagnostic(int id, int? courseId, int? levelId)
        {
            var currentUser = User?.Identity?.Name ?? "Anon";

            var test = await _context.CtTests.FirstOrDefaultAsync(t => t.PkTest == id && t.Available == 1);
            if (test == null)
            {
                TempData["ErrorMessage"] = "Test not found or not available.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var cId = courseId ?? test.FkCourse;
            var lId = levelId ?? test.FkLevelcourse;

            // ¿ya contestó HOY?
            var answeredToday = await _context.SyUserDiagnostics.AsNoTracking()
                .AnyAsync(d => d.FkTest == test.PkTest
                               && d.Createuser == currentUser
                               && EF.Functions.DateDiffDay(d.Createdate, DateTime.Now) == 0);

            if (answeredToday)
            {
                // Mostrar vista intermedia con botón "Continuar" (abre el PDF)
                ViewBag.CourseId = cId;
                ViewBag.LevelId = lId;

                // Por seguridad, validamos material aquí también
                ViewBag.HasMaterial = await ExistsMaterialAsync(cId, lId);
                return View("DiagnosticAlreadyAnswered");
            }

            // Cargar preguntas
            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .OrderBy(q => q.PkQuestions)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "This test has no questions. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var model = new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                NextCourseId = cId,
                NextLevelId = lId,
                Questions = questions.Select(q =>
                {
                    var opts = _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions && o.Available == 1)
                        .OrderBy(o => o.PkOptions)
                        .ToList();

                    return new SubmitQuestionViewModel
                    {
                        FkQuestion = q.PkQuestions,
                        QuestionText = q.Question,
                        IsMultiple = opts.Count(o => o.Answer == 1) > 1,
                        Options = opts.Select(o => new SubmitOptionViewModel
                        {
                            FkOption = o.PkOptions,
                            OptionText = o.Options,
                            IsSelected = false
                        }).ToList()
                    };
                }).ToList()
            };

            return View("Diagnostic", model);
        }

        // POST: guarda en dbo.SY_USER_DIAGNOSTIC y redirige al visor del PDF si hay material
        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDiagnostic(SubmitTestViewModel model)
        {
            if (model?.Questions == null || !model.Questions.Any())
            {
                TempData["ErrorMessage"] = "There are no answers to submit.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var hasAnySelection = model.Questions
                .SelectMany(q => q.Options ?? new List<SubmitOptionViewModel>())
                .Any(o => o.IsSelected);

            if (!hasAnySelection)
            {
                TempData["ErrorMessage"] = "Please select at least one option before submitting.";
                return View("Diagnostic", model);
            }

            var currentUser = User.Identity?.Name ?? "Anon";
            var now = DateTime.Now;

            // IDs de preguntas del POST
            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            // Mapa de textos de pregunta (seguridad)
            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

            // Mapa de opciones correctas
            var correctMap = await _context.CtOptions
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1 && o.Answer == 1)
                .GroupBy(o => o.FkQuestions)
                .Select(g => new
                {
                    FkQuestion = g.Key,
                    Ids = g.Select(x => x.PkOptions).ToList(),
                    Csv = string.Join(",", g.OrderBy(x => x.PkOptions).Select(x => x.PkOptions))
                })
                .ToDictionaryAsync(x => x.FkQuestion, x => (Ids: x.Ids, Csv: x.Csv));

            var resultVm = new DiagnosticResultViewModel
            {
                FkTest = model.FkTest,
                TestName = model.TestName,
                NextCourseId = model.NextCourseId,
                NextLevelId = model.NextLevelId,
                Questions = new List<DiagnosticQuestionResultViewModel>()
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var q in model.Questions)
                {
                    var selectedIds = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Where(o => o.IsSelected)
                        .Select(o => o.FkOption)
                        .OrderBy(id => id)
                        .ToList();

                    var csvSelected = string.Join(",", selectedIds);
                    var correctIds = correctMap.TryGetValue(q.FkQuestion, out var t1) ? t1.Ids : new List<int>();
                    var csvCorrect = correctMap.TryGetValue(q.FkQuestion, out var t2) ? t2.Csv : string.Empty;

                    _context.SyUserDiagnostics.Add(new SyUserDiagnostic
                    {
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        Createuser = currentUser,
                        Createdate = now,
                        Available = 1
                    });

                    var optionResults = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Select(o => new DiagnosticOptionResultViewModel
                        {
                            FkOption = o.FkOption,
                            OptionText = o.OptionText,
                            IsSelected = selectedIds.Contains(o.FkOption),
                            IsCorrect = correctIds.Contains(o.FkOption)
                        })
                        .OrderBy(o => o.FkOption)
                        .ToList();

                    bool questionCorrect =
                        selectedIds.Count == correctIds.Count &&
                        !selectedIds.Except(correctIds).Any() &&
                        !correctIds.Except(selectedIds).Any();

                    var safeQuestionText =
                        !string.IsNullOrWhiteSpace(q.QuestionText)
                            ? q.QuestionText
                            : (questionTextMap.TryGetValue(q.FkQuestion, out var qt) ? qt : string.Empty);

                    resultVm.Questions.Add(new DiagnosticQuestionResultViewModel
                    {
                        FkQuestion = q.FkQuestion,
                        QuestionText = safeQuestionText,
                        IsMultiple = q.IsMultiple,
                        SelectedOptionIds = selectedIds,
                        CorrectOptionIds = correctIds,
                        IsCorrect = questionCorrect,
                        Options = optionResults
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error saving diagnostic: {ex.Message}";
                return View("Diagnostic", model);
            }

            // Score + validación de material
            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = (int)Math.Round((double)resultVm.CorrectCount * 100.0 / Math.Max(1, resultVm.TotalQuestions), 0);
            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);

            // ✅ Si hay material, redirige al visor de PDF inmediatamente
            if (resultVm.HasMaterial && resultVm.NextCourseId > 0 && resultVm.NextLevelId > 0)
            {
                TempData["SuccessMessage"] = "Diagnostic submitted. Opening course material...";
                return RedirectToAction(
                    "OpenPdfCourseMaterialByCourseLevel",
                    "Catalog",
                    new { courseId = resultVm.NextCourseId, levelId = resultVm.NextLevelId }
                );
            }

            // ❗ Si NO hay material, muestra resultados con aviso
            TempData["ErrorMessage"] = "No course material (PDF/URL) linked to this course/level. Please contact HR.";
            return View("DiagnosticResult", resultVm);
        }

        // GET: render exam (mismo fetch que Diagnostic; puedes ajustar si tienes tipo de test)
        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        [HttpGet]
        public async Task<IActionResult> Exam(int? id, int? courseId, int? levelId)
        {
            CtTest? test = null;

            if (id.HasValue)
            {
                test = await _context.CtTests.FirstOrDefaultAsync(t => t.PkTest == id.Value && t.Available == 1);
            }
            else if (courseId.HasValue && levelId.HasValue)
            {
                test = await _context.CtTests.AsNoTracking()
                    .Where(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId)
                    .OrderByDescending(t => t.PkTest)
                    .FirstOrDefaultAsync();
            }

            if (test == null)
            {
                TempData["ErrorMessage"] = "Exam not found or not available.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .OrderBy(q => q.PkQuestions)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "This exam has no questions. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var model = new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                NextCourseId = courseId ?? test.FkCourse,
                NextLevelId = levelId ?? test.FkLevelcourse,
                Questions = questions.Select(q =>
                {
                    var opts = _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions && o.Available == 1)
                        .OrderBy(o => o.PkOptions)
                        .ToList();

                    return new SubmitQuestionViewModel
                    {
                        FkQuestion = q.PkQuestions,
                        QuestionText = q.Question,
                        IsMultiple = opts.Count(o => o.Answer == 1) > 1,
                        Options = opts.Select(o => new SubmitOptionViewModel
                        {
                            FkOption = o.PkOptions,
                            OptionText = o.Options,
                            IsSelected = false
                        }).ToList()
                    };
                }).ToList()
            };

            // Reutiliza la misma vista que Diagnostic o crea "Exam.cshtml" clonando la de Diagnostic
            return View("Exam", model);
        }

        // POST: guarda en SY_USER_ANSWER y controla el loop de aprobación
        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(SubmitTestViewModel model)
        {
            if (model?.Questions == null || !model.Questions.Any())
            {
                TempData["ErrorMessage"] = "There are no answers to submit.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var hasAnySelection = model.Questions
                .SelectMany(q => q.Options ?? new List<SubmitOptionViewModel>())
                .Any(o => o.IsSelected);

            if (!hasAnySelection)
            {
                TempData["ErrorMessage"] = "Please select at least one option before submitting.";
                return View("Exam", model);
            }

            var currentUser = User.Identity?.Name ?? "Anon";
            var now = DateTime.Now;

            // IDs de preguntas del POST
            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            // Mapa de textos de pregunta (seguridad)
            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

            // Mapa de opciones correctas (Ids + Csv) — MISMO patrón que Diagnostic
            var correctMap = await _context.CtOptions
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1 && o.Answer == 1)
                .GroupBy(o => o.FkQuestions)
                .Select(g => new
                {
                    FkQuestion = g.Key,
                    Ids = g.Select(x => x.PkOptions).ToList(),
                    Csv = string.Join(",", g.OrderBy(x => x.PkOptions).Select(x => x.PkOptions))
                })
                .ToDictionaryAsync(x => x.FkQuestion, x => (Ids: x.Ids, Csv: x.Csv));

            // VM de resultado (puedes reutilizar el mismo DiagnosticResultViewModel)
            var resultVm = new DiagnosticResultViewModel
            {
                FkTest = model.FkTest,
                TestName = model.TestName,
                NextCourseId = model.NextCourseId,
                NextLevelId = model.NextLevelId,
                Questions = new List<DiagnosticQuestionResultViewModel>()
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var q in model.Questions)
                {
                    var selectedIds = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Where(o => o.IsSelected)
                        .Select(o => o.FkOption)
                        .OrderBy(id => id)
                        .ToList();

                    var csvSelected = string.Join(",", selectedIds);
                    var correctIds = correctMap.TryGetValue(q.FkQuestion, out var t1) ? t1.Ids : new List<int>();
                    var csvCorrect = correctMap.TryGetValue(q.FkQuestion, out var t2) ? t2.Csv : string.Empty;

                    // 👇 Guardado tipo DIAGNOSTIC pero en SY_USER_ANSWER (modelo que nos mostraste)
                    _context.SyUserAnswers.Add(new SyUserAnswer
                    {
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        Createuser = currentUser,
                        Createdate = now,
                        Available = 1
                    });

                    // Armar detalle por opción para pintar la vista
                    var optionResults = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Select(o => new DiagnosticOptionResultViewModel
                        {
                            FkOption = o.FkOption,
                            OptionText = o.OptionText,
                            IsSelected = selectedIds.Contains(o.FkOption),
                            IsCorrect = correctIds.Contains(o.FkOption)
                        })
                        .OrderBy(o => o.FkOption)
                        .ToList();

                    bool questionCorrect =
                        selectedIds.Count == correctIds.Count &&
                        !selectedIds.Except(correctIds).Any() &&
                        !correctIds.Except(selectedIds).Any();

                    var safeQuestionText =
                        !string.IsNullOrWhiteSpace(q.QuestionText)
                            ? q.QuestionText
                            : (questionTextMap.TryGetValue(q.FkQuestion, out var qt) ? qt : string.Empty);

                    resultVm.Questions.Add(new DiagnosticQuestionResultViewModel
                    {
                        FkQuestion = q.FkQuestion,
                        QuestionText = safeQuestionText,
                        IsMultiple = q.IsMultiple,
                        SelectedOptionIds = selectedIds,
                        CorrectOptionIds = correctIds,
                        IsCorrect = questionCorrect,
                        Options = optionResults
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error saving exam: {ex.Message}";
                return View("Exam", model);
            }

            // Calcula score igual
            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = (int)Math.Round((double)resultVm.CorrectCount * 100.0 / Math.Max(1, resultVm.TotalQuestions), 0);
            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);

            // 👉 Mostrar SIEMPRE la vista de resultados del EXAMEN (no redirigir directo).
            // Desde esa vista el usuario:
            //   - Ve calificación
            //   - Puede abrir el PDF para repasar
            //   - Puede confirmar "Reintentar" el examen
            return View("ExamResult", resultVm);
        }
    }
}
