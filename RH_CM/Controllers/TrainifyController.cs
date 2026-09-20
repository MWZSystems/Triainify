using ClosedXML;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.Service.SQLSMS;
using RH_CM.Service.Trainify;
using RH_CM.ViewModels;
using System.Data;
using static RH_CM.ViewModels.ViewModels;
using RH_CM.Messages.Trainify;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public class TrainifyController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly DiagnosticExamService _diagnosticExamService;

        public TrainifyController(
            db_abcd61_rhchdbContext context,
            DiagnosticExamService diagnosticExamService)
        {
            _context = context;
            _diagnosticExamService = diagnosticExamService;
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
        public async Task<IActionResult> MatrixByEmployee(string? mode = "EMPLOYEE", int? fkPosition = null, string? userName = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = new List<MatrizByEmployeeViewModel>();
            var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "EMPLOYEE" : mode.Trim().ToUpper();

            var (targetResolved, effectiveUserName, selectedPosition, targetError) =
                ResolveMatrixByEmployeeTarget(normalizedMode, fkPosition, userName);

            if (!targetResolved)
            {
                TempData["ErrorMessage"] = targetError;
                ViewBag.Mode = normalizedMode;
                ViewBag.Positions = await GetPositionsAsync();
                ViewBag.SelectedPositionId = null;
                ViewBag.TargetUserName = null;
                return View(result);
            }

            string connectionString = _context.Database.GetDbConnection().ConnectionString;

            try
            {
                if (normalizedMode == "POSITION")
                {
                    result = await LoadPositionMatrixAsync(selectedPosition!.Value);
                }
                else
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    await using var command = new SqlCommand("sp_GetMatrizbyEmployeeCourseAssignments", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256)
                    {
                        Value = effectiveUserName ?? (object)DBNull.Value
                    });

                    command.Parameters.Add(new SqlParameter("@FkPosition", SqlDbType.Int)
                    {
                        Value = DBNull.Value
                    });

                    await using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        result.Add(MapEmployeeRow(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = string.Format(TrainifyMessages.SqlErrorWhileQueryingDataFormat, ex.Message);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(TrainifyMessages.ErrorOccurredFormat, ex.Message);
            }

            ViewBag.Mode = normalizedMode;
            ViewBag.Positions = await GetPositionsAsync();
            ViewBag.SelectedPositionId = selectedPosition;
            ViewBag.TargetUserName = effectiveUserName;

            return View(result);
        }

        /// <summary>
        /// Builds the configured matrix for a position without reading employee progress,
        /// completed courses, exam attempts, or the currently signed-in user's history.
        /// </summary>
        private async Task<List<MatrizByEmployeeViewModel>> LoadPositionMatrixAsync(int positionId)
        {
            var assignments = await (
                from assignment in _context.CtCourseassignments.AsNoTracking()
                join position in _context.CtPositions.AsNoTracking()
                    on assignment.FkPosition equals position.PkPosition
                join course in _context.CtCourses.AsNoTracking()
                    on assignment.FkCourse equals course.PkCourse
                join level in _context.CtLevelcourses.AsNoTracking()
                    on assignment.FkRequiredCourseLevels equals level.PkLevelcourse
                where assignment.FkPosition == positionId
                   && assignment.Available == 1
                   && position.Available == 1
                   && course.Available == 1
                   && level.Available == 1
                orderby course.CourseName, level.DescripctionLevel
                select new
                {
                    assignment.FkPosition,
                    PositionName = position.NamePositionEnglish,
                    assignment.FkCourse,
                    course.CourseName,
                    assignment.FkRequiredCourseLevels,
                    LevelName = level.DescripctionLevel,
                    assignment.Requiered,
                    assignment.FkDeliveryMode,
                    course.CourseValidityDays,
                    assignment.LastUpdateDate
                }).ToListAsync();

            return assignments.Select(x => new MatrizByEmployeeViewModel
            {
                FK_Position = x.FkPosition,
                NAME_POSITION_ENGLISH = x.PositionName,
                FK_Course = x.FkCourse,
                CourseName = x.CourseName,
                FK_RequiredCourseLevels = x.FkRequiredCourseLevels,
                DESCRIPCTION_LEVEL = x.LevelName,
                Requiered = x.Requiered,
                FK_DeliveryMode = x.FkDeliveryMode ?? 0,
                CourseValidityDays = x.CourseValidityDays,
                LastUpdateDate = x.LastUpdateDate,
                CourseStatus = x.Requiered ? "REQUIRED" : "OPTIONAL"
            }).ToList();
        }

        /// <summary>
        /// Resolves the (userName, position) target for MatrixByEmployee based on the requested mode,
        /// or an error message when the request doesn't have what it needs (e.g. no position selected,
        /// or the current user's name couldn't be determined).
        /// </summary>
        private (bool Resolved, string? EffectiveUserName, int? SelectedPosition, string? Error) ResolveMatrixByEmployeeTarget(
            string normalizedMode, int? fkPosition, string? userName)
        {
            if (normalizedMode == "POSITION")
            {
                if (!fkPosition.HasValue || fkPosition.Value <= 0)
                {
                    return (false, null, null, TrainifyMessages.SelectAValidPosition);
                }

                return (true, null, fkPosition.Value, null);
            }

            var effectiveUserName = string.IsNullOrWhiteSpace(userName)
                ? User?.Identity?.Name
                : userName;

            if (string.IsNullOrWhiteSpace(effectiveUserName))
            {
                return (false, null, null, TrainifyMessages.UnableToRetrieveTheCurrentUser);
            }

            return (true, effectiveUserName, null, null);
        }

        /// <summary>
        /// Maps the current row of a sp_GetMatrizbyEmployeeCourseAssignments reader to a view model.
        /// </summary>
        private static MatrizByEmployeeViewModel MapEmployeeRow(SqlDataReader reader)
        {
            return new MatrizByEmployeeViewModel
            {
                FK_Position = ReadInt32(reader, "FK_Position"),
                NAME_POSITION_ENGLISH = ReadString(reader, "NAME_POSITION_ENGLISH"),
                FK_Course = ReadInt32(reader, "FK_Course"),
                CourseName = ReadString(reader, "CourseName"),
                FK_RequiredCourseLevels = ReadInt32(reader, "FK_RequiredCourseLevels"),
                DESCRIPCTION_LEVEL = ReadNullableString(reader, "DESCRIPCTION_LEVEL"),
                Requiered = ReadBoolean(reader, "Requiered"),
                FK_DeliveryMode = ReadInt32(reader, "FK_DeliveryMode"),
                CourseValidityDays = ReadNullableInt32(reader, "CourseValidityDays"),
                UserName = ReadString(reader, "UserName"),
                CONTROL_NUMBER = ReadString(reader, "CONTROL_NUMBER"),
                NAMES = ReadString(reader, "NAMES"),
                LAST_NAME = ReadString(reader, "LAST_NAME"),
                SECOND_NAME = ReadString(reader, "SECOND_NAME"),
                LastUpdateDate = ReadNullableDateTime(reader, "LastUpdateDate"),
                CourseStatus = ReadString(reader, "CourseStatus")
            };
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

        /// <summary>
        /// Raw export of every column in SyCoursecompleteds, with no joins or translations,
        /// so staff can cross-check the data behind the Course Completed list.
        /// </summary>
        public async Task<IActionResult> ExportCourseCompletedFullData()
        {
            var data = await _context.SyCoursecompleteds.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "CourseCompleted");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("CourseCompleted"));
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

            TempData["SuccessMessage"] = TrainifyMessages.RecordCreatedSuccessfully;
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        public async Task<IActionResult> EditCourseCompleted(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            TempData["SuccessMessage"] = TrainifyMessages.RecordUpdatedSuccessfully;
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        public async Task<IActionResult> DeleteCourseCompleted(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = await ToViewModel(entity);
            return View(vm);
        }

        [HttpPost, ActionName("DeleteCourseCompleted"), ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DeleteCourseCompletedConfirmed(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await _context.SyCoursecompleteds.FindAsync(id);
            if (entity == null) return NotFound();

            _context.SyCoursecompleteds.Remove(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = TrainifyMessages.RecordDeletedSuccessfully;
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
                .AsNoTracking()
                .OrderBy(x => x.PkCourseAssignment)
                .Select(x => new
                {
                    x.PkCourseAssignment,
                    Text = $"#{x.PkCourseAssignment} - Course:{x.FkCourse} Level:{x.FkRequiredCourseLevels}"
                })
                .ToListAsync();

            var csList = await _context.CtCoursestatuses
                .AsNoTracking()
                .Where(x => x.Available == 1)
                .OrderBy(x => x.DescriptionCoursestatus)
                .ToListAsync();

            var dmList = await _context.CtDeliverymodes
                .AsNoTracking()
                .Where(x => x.Available == 1)
                .OrderBy(x => x.DescriptionDeliverymode)
                .ToListAsync();
            dmList.Insert(0, new CtDeliverymode { PkDeliverymode = 0, DescriptionDeliverymode = "Not Assigned" });

            var hcList = await _context.SyHeadcounts
                .AsNoTracking()
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = TrainifyMessages.SelectAnExcelFile;
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
                    string? CreateUser = ws.Cell(row, 7).GetValue<string>()?.Trim();
                    string? LastUpdateUser = ws.Cell(row, 8).GetValue<string>()?.Trim();

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
                TempData["ErrorMessage"] = string.Format(TrainifyMessages.ErrorReadingTheExcelFileFormat, ex.Message);
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

                using var reader = await cmd.ExecuteReaderAsync();

                if (reader.FieldCount > 0)
                {
                    errors = new DataTable();
                    errors.Load(reader);
                }

                if (await reader.NextResultAsync() && reader.FieldCount > 0)
                {
                    summary = new DataTable();
                    summary.Load(reader);
                }

                TempData["SuccessMessage"] = BuildSummaryMessage(summary, errors);
                TempData["ErrorTable"] = DataTableToHtml(errors);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(TrainifyMessages.ErrorProcessingTheUploadFormat, ex.Message);
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

        [Authorize]
        [HttpGet("/Catalog/StartCourse")]
        public async Task<IActionResult> StartCourse([FromQuery] int courseId, [FromQuery] int levelId, [FromQuery] int? courseAssignmentId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _diagnosticExamService.ExistsDiagnosticTestAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = TrainifyMessages.NoDiagnosticTestWasFoundForThis;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (!await _diagnosticExamService.ExistsMaterialAsync(courseId, levelId))
            {
                TempData["ErrorMessage"] = TrainifyMessages.NoCourseMaterialPdfUrlIsLinked;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var testId = await _diagnosticExamService.GetDiagnosticTestIdAsync(courseId, levelId);
            if (!testId.HasValue)
            {
                TempData["ErrorMessage"] = TrainifyMessages.DiagnosticTestNotFound;
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
                TempData["ErrorMessage"] = TrainifyMessages.UnableToRetrieveTheCurrentUser;
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

                while (await reader.ReadAsync())
                {
                    result.Add(MapLearningRow(reader));
                }

                if (result.Count == 0)
                {
                    TempData["SuccessMessage"] = BuildNoCoursesMessage(requiredFilter);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(TrainifyMessages.ErrorLoadingCoursesFormat, ex.Message);
            }

            ViewBag.RequiredFilter = string.IsNullOrWhiteSpace(requiredFilter)
                ? "REQUIRED"
                : requiredFilter.Trim().ToUpper();

            return View(result);
        }

        /// <summary>
        /// Maps the current row of a sp_GetCoursestoDo_Learning reader to a view model. Some columns
        /// are only present for certain filter modes, hence the HasColumn checks.
        /// </summary>
        private static LearningCourseToDoViewModel MapLearningRow(SqlDataReader reader)
        {
            return new LearningCourseToDoViewModel
            {
                PK_CourseAssignment = ReadInt32(reader, "PK_CourseAssignment"),
                NAME_POSITION_ENGLISH = ReadString(reader, "NAME_POSITION_ENGLISH"),
                FK_Course = ReadInt32(reader, "FK_Course"),
                CourseName = ReadString(reader, "CourseName"),
                CourseLevel = ReadString(reader, "CourseLevel"),
                FK_RequiredCourseLevels = ReadInt32(reader, "FK_RequiredCourseLevels"),
                DeliveryMode = ReadString(reader, "DeliveryMode"),
                CourseValidityDays = ReadNullableInt32(reader, "CourseValidityDays"),
                CONTROL_NUMBER = ReadString(reader, "CONTROL_NUMBER"),
                FullName = ReadString(reader, "FullName"),
                LastUpdateDate = ReadNullableDateTime(reader, "LastUpdateDate"),
                CourseStatus = ReadString(reader, "CourseStatus")
            };
        }

        private static bool HasReaderValue(SqlDataReader reader, string columnName)
        {
            return reader.HasColumn(columnName) && !reader.IsDBNull(reader.GetOrdinal(columnName));
        }

        private static string ReadString(SqlDataReader reader, string columnName, string fallback = "—")
        {
            return HasReaderValue(reader, columnName) ? reader[columnName]?.ToString() ?? fallback : fallback;
        }

        private static string? ReadNullableString(SqlDataReader reader, string columnName)
        {
            return HasReaderValue(reader, columnName) ? reader[columnName]?.ToString() : null;
        }

        private static int ReadInt32(SqlDataReader reader, string columnName)
        {
            return HasReaderValue(reader, columnName) ? Convert.ToInt32(reader[columnName]) : 0;
        }

        private static int? ReadNullableInt32(SqlDataReader reader, string columnName)
        {
            return HasReaderValue(reader, columnName) ? Convert.ToInt32(reader[columnName]) : null;
        }

        private static bool ReadBoolean(SqlDataReader reader, string columnName)
        {
            return HasReaderValue(reader, columnName) && Convert.ToBoolean(reader[columnName]);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            return HasReaderValue(reader, columnName) ? Convert.ToDateTime(reader[columnName]) : null;
        }

        /// <summary>
        /// Builds the "no courses to display" message for the current filter, when the query returns nothing.
        /// </summary>
        private static string BuildNoCoursesMessage(string requiredFilter)
        {
            var normalizedFilter = (requiredFilter ?? "REQUIRED").Trim().ToUpper();

            return normalizedFilter switch
            {
                "NOT_REQUIRED" => "There are no non-required courses to display.",
                "ALL" => "There are no courses to display.",
                _ => "There are no required pending courses to display."
            };
        }

        [HttpGet("/Catalog/OpenPdfCourseMaterialByCourseLevel")]
        public async Task<IActionResult> OpenPdfCourseMaterialByCourseLevel(
            [FromQuery] int courseId, [FromQuery] int levelId, [FromQuery] int? courseAssignmentId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User?.Identity?.Name ?? string.Empty;
            var hasDiagnosticAttempt = await _diagnosticExamService.HasDiagnosticAttemptAsync(currentUser, courseId, levelId);
            if (!hasDiagnosticAttempt)
            {
                var diagnosticTestId = await _diagnosticExamService.GetDiagnosticTestIdAsync(courseId, levelId);
                if (!diagnosticTestId.HasValue)
                {
                    TempData["ErrorMessage"] = TrainifyMessages.NoDiagnosticTestWasFoundForThis;
                    return RedirectToAction("LearningTrainify", "Trainify");
                }

                TempData["ErrorMessage"] = TrainifyMessages.YouMustCompleteTheDiagnosticTestBefore;
                return RedirectToAction("Diagnostic", "Trainify", new
                {
                    id = diagnosticTestId.Value,
                    courseId,
                    levelId,
                    courseAssignmentId
                });
            }

            var material = await _diagnosticExamService.GetActiveMaterialAsync(courseId, levelId);

            if (material == null)
            {
                TempData["ErrorMessage"] = TrainifyMessages.NoCourseMaterialIsLinkedToThis;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var material = await _diagnosticExamService.GetActiveMaterialAsync(courseId, levelId);

            if (material == null)
            {
                TempData["ErrorMessage"] = TrainifyMessages.NoCourseMaterialIsLinkedToThis;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (material.File != null && material.File.Length > 0)
                return File(material.File, "application/pdf");

            if (!string.IsNullOrWhiteSpace(material.UrlPath))
                return Redirect(material.UrlPath);

            TempData["ErrorMessage"] = TrainifyMessages.MaterialExistsButHasNoFileOr;
            return RedirectToAction("LearningTrainify", "Trainify");
        }

        [HttpGet]
        public async Task<IActionResult> Diagnostic(int id, int? courseId, int? levelId, int? courseAssignmentId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var loadResult = await _diagnosticExamService.GetDiagnosticViewModelAsync(id, courseId, levelId, courseAssignmentId);

            if (loadResult.Model == null)
            {
                TempData["ErrorMessage"] = loadResult.ErrorMessage;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            return View("Diagnostic", loadResult.Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDiagnostic(SubmitTestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Anon";
            var outcome = await _diagnosticExamService.SubmitDiagnosticAsync(model, currentUser);

            if (outcome.RedirectToLearning)
            {
                TempData["ErrorMessage"] = outcome.ErrorMessage;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (!outcome.Success)
            {
                TempData["ErrorMessage"] = outcome.ErrorMessage;
                return View("Diagnostic", model);
            }

            ViewBag.CourseAssignmentId = model.CourseAssignmentId;
            TempData["SuccessMessage"] = TrainifyMessages.DiagnosticSubmittedReviewYourResultsBelow;
            return View("DiagnosticResult", outcome.Result);
        }

        [HttpGet]
        public async Task<IActionResult> Exam(int? id, int? courseId, int? levelId, int? courseAssignmentId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!courseId.HasValue || !levelId.HasValue)
            {
                TempData["ErrorMessage"] = TrainifyMessages.CourseAndLevelAreRequiredToOpen;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            var currentUser = User?.Identity?.Name ?? string.Empty;
            var hasDiagnosticAttempt = await _diagnosticExamService.HasDiagnosticAttemptAsync(currentUser, courseId.Value, levelId.Value);
            if (!hasDiagnosticAttempt)
            {
                var diagnosticTestId = await _diagnosticExamService.GetDiagnosticTestIdAsync(courseId.Value, levelId.Value);
                if (!diagnosticTestId.HasValue)
                {
                    TempData["ErrorMessage"] = TrainifyMessages.NoDiagnosticTestWasFoundForThis;
                    return RedirectToAction("LearningTrainify", "Trainify");
                }

                TempData["ErrorMessage"] = TrainifyMessages.YouMustCompleteTheDiagnosticTestBeforeTaking;
                return RedirectToAction("Diagnostic", "Trainify", new
                {
                    id = diagnosticTestId.Value,
                    courseId = courseId.Value,
                    levelId = levelId.Value,
                    courseAssignmentId
                });
            }

            var loadResult = await _diagnosticExamService.GetExamViewModelAsync(id, courseId.Value, levelId.Value, courseAssignmentId ?? 0);

            if (loadResult.Model == null)
            {
                TempData["ErrorMessage"] = loadResult.ErrorMessage;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            return View("Exam", loadResult.Model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitExam(SubmitTestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = User.Identity?.Name ?? "Anon";
            var outcome = await _diagnosticExamService.SubmitExamAsync(model, currentUser);

            if (outcome.RedirectToLearning)
            {
                TempData["ErrorMessage"] = outcome.RedirectMessage;
                return RedirectToAction("LearningTrainify", "Trainify");
            }

            if (outcome.ReshowExamForm)
            {
                TempData["ErrorMessage"] = outcome.ReshowMessage;
                return View("Exam", model);
            }

            if (outcome.ResultErrorMessage != null)
            {
                TempData["ErrorMessage"] = outcome.ResultErrorMessage;
            }
            else if (outcome.Result != null)
            {
                TempData["SuccessMessage"] = "Exam recorded successfully. The course completion was confirmed.";
            }

            ViewBag.CourseAssignmentId = model.CourseAssignmentId;
            return View("ExamResult", outcome.Result);
        }
    }
}
