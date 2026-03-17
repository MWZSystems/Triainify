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
        private readonly string _connString;

        public TrainifyController(
            db_abcd61_rhchdbContext context,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _connString = configuration.GetConnectionString("ConexionSQL");
        }

        // Helper: positions dropdown
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MatrixByEmployee(string mode = "EMPLOYEE", int? fkPosition = null, string? userName = null)
        {
            var result = new List<MatrizByEmployeeViewModel>();
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "EMPLOYEE" : mode.Trim().ToUpper();

            string? effectiveUserName = null;
            int? selectedPosition = null;

            if (normalizedMode == "POSITION")
            {
                if (!fkPosition.HasValue || fkPosition.Value <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a valid position.";
                    ViewBag.Mode = "POSITION";
                    ViewBag.Positions = await GetPositionsAsync();
                    ViewBag.SelectedPositionId = null;
                    ViewBag.TargetUserName = null;
                    return View(result);
                }

                selectedPosition = fkPosition.Value;
            }
            else
            {
                effectiveUserName = string.IsNullOrWhiteSpace(userName)
                    ? User?.Identity?.Name
                    : userName;

                if (string.IsNullOrWhiteSpace(effectiveUserName))
                {
                    TempData["ErrorMessage"] = "Unable to retrieve the current user.";
                    ViewBag.Mode = "EMPLOYEE";
                    ViewBag.Positions = await GetPositionsAsync();
                    ViewBag.SelectedPositionId = null;
                    ViewBag.TargetUserName = null;
                    return View(result);
                }
            }

            string connectionString = _context.Database.GetDbConnection().ConnectionString;

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("sp_GetMatrizbyEmployeeCourseAssignments", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256)
                {
                    Value = string.IsNullOrWhiteSpace(effectiveUserName)
                        ? (object)DBNull.Value
                        : effectiveUserName
                });

                command.Parameters.Add(new SqlParameter("@FkPosition", SqlDbType.Int)
                {
                    Value = selectedPosition.HasValue
                        ? (object)selectedPosition.Value
                        : DBNull.Value
                });

                await using var reader = await command.ExecuteReaderAsync();

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
                        CourseValidityDays = IsNull("CourseValidityDays") ? (int?)null : Convert.ToInt32(reader["CourseValidityDays"]),
                        UserName = IsNull("UserName") ? "—" : reader["UserName"]?.ToString() ?? "—",
                        CONTROL_NUMBER = IsNull("CONTROL_NUMBER") ? "—" : reader["CONTROL_NUMBER"]?.ToString() ?? "—",
                        NAMES = IsNull("NAMES") ? "—" : reader["NAMES"]?.ToString() ?? "—",
                        LAST_NAME = IsNull("LAST_NAME") ? "—" : reader["LAST_NAME"]?.ToString() ?? "—",
                        SECOND_NAME = IsNull("SECOND_NAME") ? "—" : reader["SECOND_NAME"]?.ToString() ?? "—",
                        LastUpdateDate = IsNull("LastUpdateDate") ? (DateTime?)null : Convert.ToDateTime(reader["LastUpdateDate"]),
                        CourseStatus = IsNull("CourseStatus") ? "—" : reader["CourseStatus"]?.ToString() ?? "—"
                    });
                }
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"SQL error while querying data: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            ViewBag.Mode = normalizedMode;
            ViewBag.Positions = await GetPositionsAsync();
            ViewBag.SelectedPositionId = selectedPosition;
            ViewBag.TargetUserName = effectiveUserName;

            return View(result);
        }

        [Authorize(Policy = "ViewAccess")]
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

        public async Task<IActionResult> CreateCourseCompleted()
        {
            var vm = new RH_CM.ViewModels.SyCourseCompletedVM();
            await PopulateSelects(vm);
            return View(vm);
        }

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
                FkDeliveryMode = vm.FkDeliveryMode,
                FkHeadcount = vm.FkHeadcount,
                CreateUser = user,
                CreateDate = now,
                LastUpdateUser = user,
                LastUpdateDate = now,
                Avaialble = 1
            };

            _context.SyCoursecompleteds.Add(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Record created successfully.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        public async Task<IActionResult> EditCourseCompleted(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = await ToViewModel(entity);
            await PopulateSelects(vm);
            return View(vm);
        }

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

            entity.FkCourseAssignment = vm.FkCourseAssignment;
            entity.FkCourseStatus = vm.FkCourseStatus;
            entity.FkDeliveryMode = vm.FkDeliveryMode;
            entity.FkHeadcount = vm.FkHeadcount;
            entity.LastUpdateUser = User?.Identity?.Name ?? "system";
            entity.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Record updated successfully.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        public async Task<IActionResult> DeleteCourseCompleted(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = await ToViewModel(entity);
            return View(vm);
        }

        [HttpPost, ActionName("DeleteCourseCompleted"), ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DeleteCourseCompletedConfirmed(int id)
        {
            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            _context.SyCoursecompleteds.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Record deleted successfully.";
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

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

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("CourseCompleted");

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

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, bool allowUpsert = true)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an Excel file.";
                return RedirectToAction(nameof(Index));
            }

            DataTable tvp = BuildTvpSchema();
            try
            {
                using var stream = file.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.FirstOrDefault(x => x.Name.Equals("CourseCompleted", StringComparison.OrdinalIgnoreCase))
                         ?? wb.Worksheet(1);

                int row = 2;
                while (!ws.Row(row).IsEmpty())
                {
                    int RowNum = GetInt(ws.Cell(row, 1).GetValue<string>());
                    int CourseAssignmentId = GetInt(ws.Cell(row, 2).GetValue<string>());
                    int CourseStatusId = GetInt(ws.Cell(row, 3).GetValue<string>());
                    int DeliveryModeId = GetInt(ws.Cell(row, 4).GetValue<string>());
                    int ControlNumber = GetInt(ws.Cell(row, 5).GetValue<string>());
                    int Available = GetInt(ws.Cell(row, 6).GetValue<string>());
                    string CreateUser = ws.Cell(row, 7).GetValue<string>()?.Trim();
                    string LastUpdateUser = ws.Cell(row, 8).GetValue<string>()?.Trim();

                    var dr = tvp.NewRow();
                    dr["RowNum"] = RowNum > 0 ? RowNum : row - 1;
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
                TempData["ErrorMessage"] = $"Error reading the Excel file: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }

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
                TempData["ErrorTable"] = DataTableToHtml(errors);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error processing the upload: {ex.Message}";
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
            return $"Upload completed: Inserted={inserted}, Updated={updated}, Errors={err}.";
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

        private async Task<int?> GetDiagnosticTestIdAsync(int courseId, int levelId)
        {
            return await _context.CtTests
                .AsNoTracking()
                .Where(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId)
                .OrderByDescending(t => t.PkTest)
                .Select(t => (int?)t.PkTest)
                .FirstOrDefaultAsync();
        }

        private async Task<(int HeadcountId, int ControlNumber, string UserName)?> GetCurrentHeadcountInfoAsync()
        {
            var currentUser = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(currentUser))
                return null;

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == currentUser);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
                return null;

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
                return null;

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
                return null;

            return (hc.PkHeadcount, controlNumber, currentUser);
        }

        private async Task<bool> HasDiagnosticAttemptAsync(int courseId, int levelId)
        {
            var current = await GetCurrentHeadcountInfoAsync();
            if (current == null)
                return false;

            var testId = await GetDiagnosticTestIdAsync(courseId, levelId);
            if (!testId.HasValue)
                return false;

            return await _context.SyUserDiagnostics
                .AsNoTracking()
                .AnyAsync(d =>
                    d.FkHeadcount == current.Value.HeadcountId &&
                    d.FkTest == testId.Value &&
                    d.Available == 1);
        }

        [Authorize]
        [HttpGet("/Catalog/StartCourse")]
        public async Task<IActionResult> StartCourse([FromQuery] int courseId, [FromQuery] int levelId, [FromQuery] int? courseAssignmentId)
        {
            if (!await ExistsDiagnosticTestAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = "No diagnostic test was found for this course/level. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (!await ExistsMaterialAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = "No course material (PDF/URL) is linked to this course/level. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var testId = await GetDiagnosticTestIdAsync(courseId, levelId);
            if (!testId.HasValue)
            {
                TempData["ErrorMessage"] = "Diagnostic test not found.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            return RedirectToAction("Diagnostic", "Trainify",
                new { id = testId.Value, courseId, levelId, courseAssignmentId });
        }

        [Authorize]
        public async Task<IActionResult> LearningTrainify(string requiredFilter = "REQUIRED")
        {
            var result = new List<LearningCourseToDoViewModel>();
            var userName = User?.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userName))
            {
                TempData["ErrorMessage"] = "Unable to retrieve the current user.";
                ViewBag.RequiredFilter = requiredFilter;
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

                command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256)
                {
                    Value = userName
                });

                command.Parameters.Add(new SqlParameter("@RequiredFilter", SqlDbType.NVarChar, 20)
                {
                    Value = string.IsNullOrWhiteSpace(requiredFilter)
                        ? "REQUIRED"
                        : requiredFilter.Trim().ToUpper()
                });

                using var reader = await command.ExecuteReaderAsync();

                int Ord(string n) => reader.GetOrdinal(n);
                bool IsNull(string n) => reader.IsDBNull(Ord(n));
                bool HasCol(string n)
                {
                    var schema = reader.GetSchemaTable();
                    if (schema == null) return false;

                    foreach (DataRow r in schema.Rows)
                    {
                        if (string.Equals(r["ColumnName"]?.ToString(), n, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

                    return false;
                }

                while (await reader.ReadAsync())
                {
                    var vm = new LearningCourseToDoViewModel
                    {
                        PK_CourseAssignment = IsNull("PK_CourseAssignment") ? 0 : reader.GetInt32(Ord("PK_CourseAssignment")),
                        NAME_POSITION_ENGLISH = IsNull("NAME_POSITION_ENGLISH") ? "—" : reader.GetString(Ord("NAME_POSITION_ENGLISH")),

                        FK_Course = HasCol("FK_Course") && !IsNull("FK_Course") ? reader.GetInt32(Ord("FK_Course")) : 0,
                        CourseName = IsNull("CourseName") ? "—" : reader.GetString(Ord("CourseName")),
                        CourseLevel = HasCol("CourseLevel") && !IsNull("CourseLevel") ? reader.GetString(Ord("CourseLevel")) : "—",
                        FK_RequiredCourseLevels = HasCol("FK_RequiredCourseLevels") && !IsNull("FK_RequiredCourseLevels") ? reader.GetInt32(Ord("FK_RequiredCourseLevels")) : 0,

                        DeliveryMode = HasCol("DeliveryMode") && !IsNull("DeliveryMode") ? reader.GetString(Ord("DeliveryMode")) : "—",
                        CourseValidityDays = HasCol("CourseValidityDays") && !IsNull("CourseValidityDays") ? Convert.ToInt32(reader["CourseValidityDays"]) : (int?)null,

                        CONTROL_NUMBER = HasCol("CONTROL_NUMBER") && !IsNull("CONTROL_NUMBER") ? reader["CONTROL_NUMBER"].ToString()! : "—",
                        FullName = HasCol("FullName") && !IsNull("FullName") ? reader["FullName"].ToString()! : "—",

                        LastUpdateDate = HasCol("LastUpdateDate") && !IsNull("LastUpdateDate") ? Convert.ToDateTime(reader["LastUpdateDate"]) : (DateTime?)null,
                        CourseStatus = HasCol("CourseStatus") && !IsNull("CourseStatus") ? reader["CourseStatus"].ToString()! : "—"
                    };

                    result.Add(vm);
                }

                var normalizedFilter = (requiredFilter ?? "REQUIRED").Trim().ToUpper();

                if (result.Count == 0)
                {
                    TempData["SuccessMessage"] = normalizedFilter switch
                    {
                        "NOT_REQUIRED" => "There are no non-required courses to display.",
                        "ALL" => "There are no courses to display.",
                        _ => "There are no required pending courses to display."
                    };
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading courses: {ex.Message}";
            }

            ViewBag.RequiredFilter = string.IsNullOrWhiteSpace(requiredFilter)
                ? "REQUIRED"
                : requiredFilter.Trim().ToUpper();

            return View(result);
        }

        [HttpGet("/Catalog/OpenPdfCourseMaterialByCourseLevel")]
        public async Task<IActionResult> OpenPdfCourseMaterialByCourseLevel(
            [FromQuery] int courseId, [FromQuery] int levelId, [FromQuery] int? courseAssignmentId)
        {
            var hasDiagnosticAttempt = await HasDiagnosticAttemptAsync(courseId, levelId);
            if (!hasDiagnosticAttempt)
            {
                var diagnosticTestId = await GetDiagnosticTestIdAsync(courseId, levelId);
                if (!diagnosticTestId.HasValue)
                {
                    TempData["ErrorMessage"] = "No diagnostic test was found for this course/level. Please contact HR.";
                    return RedirectToAction("LearningTrainify", "Trainify");
                }

                TempData["ErrorMessage"] = "You must complete the diagnostic test before opening the material.";
                return RedirectToAction("Diagnostic", "Trainify", new
                {
                    id = diagnosticTestId.Value,
                    courseId,
                    levelId,
                    courseAssignmentId
                });
            }

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

            var type = (material.MaterialType ?? "").Trim().ToUpperInvariant();

            var vm = new RH_CM.ViewModels.CourseMaterialViewerViewModel
            {
                CourseId = courseId,
                LevelId = levelId,
                CourseAssignmentId = courseAssignmentId ?? 0,
                MaterialType = material.MaterialType,
                UrlPath = material.UrlPath
            };

            if (string.Equals(type, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                vm.StreamUrl = Url.Action(
                    nameof(StreamPdfCourseMaterialByCourseLevel),
                    "Catalog",
                    new { courseId, levelId },
                    protocol: Request.Scheme
                ) ?? string.Empty;
            }

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

        [HttpGet]
        public async Task<IActionResult> Diagnostic(int id, int? courseId, int? levelId, int? courseAssignmentId)
        {
            var test = await _context.CtTests
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PkTest == id && t.Available == 1);

            if (test == null)
            {
                TempData["ErrorMessage"] = "Test not found or not available.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var cId = courseId ?? test.FkCourse;
            var lId = levelId ?? test.FkLevelcourse;

            var questions = await _context.CtQuestions
                .AsNoTracking()
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .OrderBy(q => q.PkQuestions)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "This test has no questions. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var questionIds = questions.Select(q => q.PkQuestions).ToList();

            var optionList = await _context.CtOptions
                .AsNoTracking()
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1)
                .OrderBy(o => o.PkOptions)
                .ToListAsync();

            var model = new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                NextCourseId = cId,
                NextLevelId = lId,
                CourseAssignmentId = courseAssignmentId,
                Questions = questions.Select(q =>
                {
                    var opts = optionList
                        .Where(o => o.FkQuestions == q.PkQuestions)
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

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == currentUser);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
            {
                TempData["ErrorMessage"] = "Unable to resolve the current user's employee (missing EmployeeNumber).";
                return View("Diagnostic", model);
            }

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
            {
                TempData["ErrorMessage"] = "EmployeeNumber is not a valid number.";
                return View("Diagnostic", model);
            }

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
            {
                TempData["ErrorMessage"] = "The employee (Headcount) associated with the current user does not exist or is not available.";
                return View("Diagnostic", model);
            }

            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

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
                Questions = new List<DiagnosticQuestionResultViewModel>(),
                CourseAssignmentId = model.CourseAssignmentId ?? 0
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var codeExam = await GetNextDiagnosticCodeExamAsync();

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
                        CodeExam = codeExam,
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        FkHeadcount = hc.PkHeadcount,
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

            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = (int)Math.Round((double)resultVm.CorrectCount * 100.0 / Math.Max(1, resultVm.TotalQuestions), 0);
            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);

            ViewBag.CourseAssignmentId = model.CourseAssignmentId;
            TempData["SuccessMessage"] = "Diagnostic submitted. Review your results below.";
            return View("DiagnosticResult", resultVm);
        }

        private async Task<int> GetNextDiagnosticCodeExamAsync()
        {
            await using var conn = new SqlConnection(_connString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CAST(NEXT VALUE FOR dbo.Seq_UserDiagnostic_CodeExam AS INT)";
            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        [HttpGet]
        public async Task<IActionResult> Exam(int? id, int? courseId, int? levelId, int? courseAssignmentId)
        {
            if (!courseId.HasValue || !levelId.HasValue)
            {
                TempData["ErrorMessage"] = "Course and level are required to open the final exam.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var hasDiagnosticAttempt = await HasDiagnosticAttemptAsync(courseId.Value, levelId.Value);
            if (!hasDiagnosticAttempt)
            {
                var diagnosticTestId = await GetDiagnosticTestIdAsync(courseId.Value, levelId.Value);
                if (!diagnosticTestId.HasValue)
                {
                    TempData["ErrorMessage"] = "No diagnostic test was found for this course/level. Please contact HR.";
                    return RedirectToAction("LearningTrainify", "Trainify");
                }

                TempData["ErrorMessage"] = "You must complete the diagnostic test before taking the final exam.";
                return RedirectToAction("Diagnostic", "Trainify", new
                {
                    id = diagnosticTestId.Value,
                    courseId = courseId.Value,
                    levelId = levelId.Value,
                    courseAssignmentId
                });
            }

            CtTest? test = null;

            if (id.HasValue)
            {
                test = await _context.CtTests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.PkTest == id.Value && t.Available == 1);
            }
            else
            {
                test = await _context.CtTests
                    .AsNoTracking()
                    .Where(t => t.Available == 1 && t.FkCourse == courseId.Value && t.FkLevelcourse == levelId.Value)
                    .OrderByDescending(t => t.PkTest)
                    .FirstOrDefaultAsync();
            }

            if (test == null)
            {
                TempData["ErrorMessage"] = "Exam not found or not available.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var questions = await _context.CtQuestions
                .AsNoTracking()
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .OrderBy(q => q.PkQuestions)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] = "This exam has no questions. Please contact HR.";
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var questionIds = questions.Select(q => q.PkQuestions).ToList();

            var optionList = await _context.CtOptions
                .AsNoTracking()
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1)
                .OrderBy(o => o.PkOptions)
                .ToListAsync();

            var model = new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                NextCourseId = courseId.Value,
                NextLevelId = levelId.Value,
                CourseAssignmentId = courseAssignmentId ?? 0,
                Questions = questions.Select(q =>
                {
                    var opts = optionList
                        .Where(o => o.FkQuestions == q.PkQuestions)
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

            return View("Exam", model);
        }

        private async Task<int> GetLastDiagnosticCodeExamAsync(int fkHeadcount, int fkTest)
        {
            return await _context.SyUserDiagnostics
                .AsNoTracking()
                .Where(d => d.FkHeadcount == fkHeadcount && d.FkTest == fkTest && d.Available == 1)
                .OrderByDescending(d => d.Createdate)
                .Select(d => d.CodeExam)
                .FirstOrDefaultAsync();
        }

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

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == currentUser);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
            {
                TempData["ErrorMessage"] = "Unable to resolve the current user's employee (missing EmployeeNumber).";
                return View("Exam", model);
            }

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
            {
                TempData["ErrorMessage"] = "EmployeeNumber is not a valid number.";
                return View("Exam", model);
            }

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
            {
                TempData["ErrorMessage"] = "The employee (Headcount) associated with the current user does not exist or is not available.";
                return View("Exam", model);
            }

            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

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

            foreach (var q in model.Questions)
            {
                var selectedIds = (q.Options ?? new List<SubmitOptionViewModel>())
                    .Where(o => o.IsSelected)
                    .Select(o => o.FkOption)
                    .OrderBy(id => id)
                    .ToList();

                var correctIds = correctMap.TryGetValue(q.FkQuestion, out var t1) ? t1.Ids : new List<int>();

                bool questionCorrect =
                    selectedIds.Count == correctIds.Count &&
                    !selectedIds.Except(correctIds).Any() &&
                    !correctIds.Except(selectedIds).Any();

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

            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = (int)Math.Round((double)resultVm.CorrectCount * 100.0 / Math.Max(1, resultVm.TotalQuestions), 0);

            if (resultVm.Score < 80)
            {
                resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);
                ViewBag.CourseAssignmentId = model.CourseAssignmentId;
                TempData["ErrorMessage"] = "Minimum score is 80. Results were recorded locally only; no course completion was stored.";
                return View("ExamResult", resultVm);
            }

            if (!model.CourseAssignmentId.HasValue || model.CourseAssignmentId.Value <= 0)
            {
                resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);
                ViewBag.CourseAssignmentId = model.CourseAssignmentId;
                TempData["ErrorMessage"] = "Missing course assignment. Please contact your provider or IT support.";
                return View("ExamResult", resultVm);
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var codeExam = await GetLastDiagnosticCodeExamAsync(hc.PkHeadcount, model.FkTest);

                foreach (var q in resultVm.Questions)
                {
                    var csvSelected = string.Join(",", q.SelectedOptionIds ?? new List<int>());
                    var csvCorrect = string.Join(",", q.CorrectOptionIds ?? new List<int>());

                    _context.SyUserAnswers.Add(new SyUserAnswer
                    {
                        CodeExam = codeExam,
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        FkHeadcount = hc.PkHeadcount,
                        Createuser = currentUser,
                        Createdate = now,
                        Available = 1
                    });
                }

                _context.SyCoursemovements.Add(new SyCoursemovement
                {
                    CodeExam = codeExam,
                    FkCourseAssignment = model.CourseAssignmentId.Value,
                    FkCourseStatus = 1,
                    FkDeliveryMode = 1,
                    FkHeadcount = hc.PkHeadcount,
                    Score = resultVm.Score,
                    CreateUser = currentUser,
                    CreateDate = now,
                    LastUpdateUser = currentUser,
                    LastUpdateDate = now,
                    Avaialble = 1
                });

                var existingCompleted = await _context.SyCoursecompleteds
                    .FirstOrDefaultAsync(c =>
                        c.FkCourseAssignment == model.CourseAssignmentId.Value &&
                        c.FkHeadcount == hc.PkHeadcount &&
                        c.Avaialble == 1);

                if (existingCompleted != null)
                {
                    existingCompleted.FkCourseStatus = 1;
                    existingCompleted.FkDeliveryMode = 1;
                    existingCompleted.Score = resultVm.Score;
                    existingCompleted.LastUpdateUser = currentUser;
                    existingCompleted.LastUpdateDate = now;

                    _context.SyCoursecompleteds.Update(existingCompleted);
                }
                else
                {
                    _context.SyCoursecompleteds.Add(new SyCoursecompleted
                    {
                        FkCourseAssignment = model.CourseAssignmentId.Value,
                        FkCourseStatus = 1,
                        FkDeliveryMode = 1,
                        FkHeadcount = hc.PkHeadcount,
                        Score = resultVm.Score,
                        CreateUser = currentUser,
                        CreateDate = now,
                        LastUpdateUser = currentUser,
                        LastUpdateDate = now,
                        Avaialble = 1
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

            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);
            ViewBag.CourseAssignmentId = model.CourseAssignmentId;

            return View("ExamResult", resultVm);
        }
    }
}