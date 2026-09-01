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

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Index()
        {
            var lista = new List<CourseCompletedViewModel>();

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

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexCourseCompleted()
        {
            var rows = new List<CourseCompletedSummaryItemViewModel>();
            var cs = _context.Database.GetDbConnection().ConnectionString;

            using var conn = new SqlConnection(cs);
            using var cmd = new SqlCommand("dbo.sp_IndexCourseCompletedSummary", conn)
            { CommandType = CommandType.StoredProcedure };

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();

            int ordCourse = rdr.GetOrdinal("CourseName");
            int ordLevel = rdr.GetOrdinal("LevelName");
            int ordDeliv = rdr.GetOrdinal("DeliveryModeName");
            int ordStat = rdr.GetOrdinal("Course_Status");
            int ordTotal = rdr.GetOrdinal("TotalCompletions");

            while (await rdr.ReadAsync())
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

        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseCompletedToExcel()
        {
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
                           LevelDescription = lc.DescripctionLevel,
                           s.Avaialble,
                           s.LastUpdateDate
                       }).ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseCompleted");

            ws.Cell(1, 1).Value = "PkCourseCompleted";
            ws.Cell(1, 2).Value = "ControlNumber";
            ws.Cell(1, 3).Value = "HeadcountName";
            ws.Cell(1, 4).Value = "CourseName";
            ws.Cell(1, 5).Value = "LevelDescription";
            ws.Cell(1, 6).Value = "Available";
            ws.Cell(1, 7).Value = "LastUpdateDate";

            var header = ws.Range("A1:G1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            int row = 2;
            foreach (var x in data)
            {
                ws.Cell(row, 1).Value = x.PkCourseCompleted;
                ws.Cell(row, 2).Value = x.ControlNumber;
                ws.Cell(row, 3).Value = x.HeadcountName?.Trim();
                ws.Cell(row, 4).Value = x.CourseName;
                ws.Cell(row, 5).Value = x.LevelDescription;
                ws.Cell(row, 6).Value = (Convert.ToInt32(x.Avaialble) == 1) ? "Yes" : "No";
                ws.Cell(row, 7).Value = x.LastUpdateDate;
                row++;
            }

            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 7);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            ws.Column(7).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

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

        private async Task LoadCourseCompletedBulkViewBagsAsync(int selectedFkCourse, int selectedFkLevel)
        {
            ViewBag.Courses = await _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .Select(c => new { c.PkCourse, c.CourseName })
                .ToListAsync();

            ViewBag.Levels = await _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .Select(l => new { l.PkLevelcourse, l.DescripctionLevel })
                .ToListAsync();

            ViewBag.Headcounts = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(h =>
                    h.Available == 1
                    && _context.CtCourseassignments.Any(ca =>
                           ca.Available == 1
                           && ca.FkCourse == selectedFkCourse
                           && ca.FkRequiredCourseLevels == selectedFkLevel
                           && ca.FkPosition == h.FkPosition)
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
                            .FirstOrDefault() ?? "No position")
                    )
                })
                .OrderBy(x => x.Display)
                .ToListAsync();

            ViewBag.CourseStatus = await _context.CtCoursestatuses
                .AsNoTracking()
                .Where(s => s.Available == 1)
                .OrderBy(s => s.DescriptionCoursestatus)
                .Select(s => new { s.PkCoursestatus, s.DescriptionCoursestatus })
                .ToListAsync();

            ViewBag.DeliveryModes = await _context.CtDeliverymodes
                .AsNoTracking()
                .Where(d => d.Available == 1)
                .OrderBy(d => d.DescriptionDeliverymode)
                .Select(d => new { Id = d.PkDeliverymode, Text = d.DescriptionDeliverymode })
                .ToListAsync();
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            if (!ModelState.IsValid)
            {
                return NotFound();
            }

            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            await LoadCourseCompletedBulkViewBagsAsync(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateCourseCompletedBulk(
            int FkCourse,
            int FkRequiredCourseLevels,
            int[] SelectedHeadcounts,
            bool allowUpsert = true)
        {
            if (!ModelState.IsValid || FkCourse <= 0 || FkRequiredCourseLevels <= 0 || SelectedHeadcounts == null || SelectedHeadcounts.Length == 0)
            {
                TempData["ErrorMessage"] = "Select a course, a level, and at least one person.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
            }

            var alreadyCompletedHc = await (
                from cc in _context.SyCoursecompleteds.AsNoTracking()
                join ca in _context.CtCourseassignments.AsNoTracking()
                    on cc.FkCourseAssignment equals ca.PkCourseAssignment
                where cc.Avaialble == 1
                   && ca.FkCourse == FkCourse
                   && ca.FkRequiredCourseLevels == FkRequiredCourseLevels
                select cc.FkHeadcount
            ).ToListAsync();

            var alreadyCompletedHcSet = alreadyCompletedHc.ToHashSet();

            var filtered = SelectedHeadcounts.Distinct().Where(hc => !alreadyCompletedHcSet.Contains(hc)).ToArray();

            if (filtered.Length == 0)
            {
                TempData["ErrorMessage"] = "All selected people already have the course completed for that level.";
                return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
            }

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

            TempData["SuccessMessage"] = "Records created/updated successfully.";
            return RedirectToAction(nameof(CreateCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkRequiredCourseLevels });
        }

        private async Task LoadDeleteCourseCompletedBulkViewBagsAsync(int selectedFkCourse, int selectedFkLevel)
        {
            var courses = await _context.CtCourses!
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .Select(c => new { c.PkCourse, c.CourseName })
                .ToListAsync();

            ViewBag.Courses = courses
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.PkCourse.ToString(),
                    Text = c.CourseName,
                    Selected = c.PkCourse == selectedFkCourse
                })
                .ToList();

            var levelsQuery =
                from ca in _context.CtCourseassignments.AsNoTracking()
                join l in _context.CtLevelcourses.AsNoTracking()
                    on ca.FkRequiredCourseLevels equals l.PkLevelcourse
                join c in _context.CtCourses.AsNoTracking()
                    on ca.FkCourse equals c.PkCourse
                where
                    ca.Available == 1
                    && l.Available == 1
                    && c.Available == 1
                    && (selectedFkCourse <= 0 || ca.FkCourse == selectedFkCourse)
                select new
                {
                    l.PkLevelcourse,
                    l.DescripctionLevel
                };

            var levels = await levelsQuery
                .Distinct()
                .OrderBy(x => x.PkLevelcourse)
                .ToListAsync();

            if (selectedFkCourse > 0 && selectedFkLevel > 0 && !levels.Any(x => x.PkLevelcourse == selectedFkLevel))
            {
                selectedFkLevel = 0;
            }

            ViewBag.Levels = levels
                .Select(l => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = l.PkLevelcourse.ToString(),
                    Text = l.DescripctionLevel,
                    Selected = l.PkLevelcourse == selectedFkLevel
                })
                .ToList();
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DeleteCourseCompletedBulk(int? fkCourse, int? fkLevel)
        {
            if (!ModelState.IsValid)
            {
                return NotFound();
            }

            int selectedFkCourse = fkCourse ?? 0;
            int selectedFkLevel = fkLevel ?? 0;

            await LoadDeleteCourseCompletedBulkViewBagsAsync(selectedFkCourse, selectedFkLevel);

            ViewBag.SelectedFkCourse = selectedFkCourse;
            ViewBag.SelectedFkLevel = selectedFkLevel;

            bool needFilters = (selectedFkCourse <= 0 || selectedFkLevel <= 0);
            ViewBag.NeedFilters = needFilters;

            var rows = new List<CourseCompletedRowDtoViewModel>();

            if (!needFilters)
            {
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
                        cc.Avaialble == 1
                        && c.Available == 1
                        && ca.Available == 1
                        && lc.Available == 1
                        && cs.Available == 1
                        && dm.Available == 1
                        && h.Available == 1
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

                rows = await query
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
                    .ToListAsync();
            }

            ViewBag.CourseCompletedRows = rows;
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseCompletedBulk(
            [FromForm] int[] SelectedCourseCompletedIds,
            [FromForm] bool SoftDelete,
            [FromForm] int FkCourse,
            [FromForm] int FkLevel
        )
        {
            if (!ModelState.IsValid || FkCourse <= 0 || FkLevel <= 0 || SelectedCourseCompletedIds == null || SelectedCourseCompletedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Select at least one record to delete.";
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

                await conn.OpenAsync();
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    TempData["ErrorMessage"] = "No records were deleted (check your selection and filters).";
                else
                    TempData["SuccessMessage"] = $"Operation completed. Rows affected: {affected}.";
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while bulk-deleting completed courses. Detail: {ex.Message}";
            }

            return RedirectToAction(nameof(DeleteCourseCompletedBulk), new { fkCourse = FkCourse, fkLevel = FkLevel });
        }

        [HttpPost]
        [Route("ToggleCourseCompleted")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseCompleted(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid record.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }

            var item = await _context.SyCoursecompleteds.FindAsync(id);
            if (item != null)
            {
                item.Avaialble = item.Avaialble == 1 ? 0 : 1;
                item.LastUpdateUser = User?.Identity?.Name ?? "Unknown";
                item.LastUpdateDate = DateTime.Now;

                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexCourseCompleted));
        }

        [HttpPost]
        [Route("DeleteCourseCompleted")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseCompleted(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData["ErrorMessage"] = "Invalid record.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }

            var item = await _context.SyCoursecompleteds.FindAsync(id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "The record does not exist or has already been deleted.";
                return RedirectToAction(nameof(IndexCourseCompleted));
            }

            try
            {
                _context.SyCoursecompleteds.Remove(item);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Record deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "The record cannot be deleted because of related database records.";
            }

            return RedirectToAction(nameof(IndexCourseCompleted));
        }
    }
}
