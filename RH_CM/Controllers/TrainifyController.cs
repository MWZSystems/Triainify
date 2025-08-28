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
        // GET: TrainifyController
        public ActionResult TrainifyHome()
        {
            return View();
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

        /// <summary>
        /// AUN FALTA VALIDAR ESTA SEGUNDA PARTE
        /// </summary>
        /// <returns></returns>

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
    }
}
