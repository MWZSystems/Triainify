using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.ViewModels;
using System.Data;
using RH_CM.Messages.Catalog;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {

        [Authorize(Policy = "ViewAccess")]
        public IActionResult IndexCourseassignments()
        {
            var courseAssignments = _context.CtCourseassignments
                .AsNoTracking()
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

            return View("CourseAssignments/IndexCourseassignments", courseAssignments);
        }

        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> ExportCourseAssignmentsToExcel()
        {
            // 1) Get the data, keeping the model's original types
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
                // LEFT JOIN with DeliveryMode (FkDeliveryMode is nullable)
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
                    // Types as-is from the model
                    x.ca.Requiered,              // bool
                    x.ca.Available,              // int (0/1)
                    DeliveryModeDescription = dm != null ? dm.DescriptionDeliverymode : null, // may be null
                                                                                              // Audit fields
                    x.ca.CreateUser,
                    x.ca.CreateDate,
                    x.ca.LastUpdateUser,
                    x.ca.LastUpdateDate
                })
                .OrderBy(x => x.PositionName)
                .ThenBy(x => x.CourseName)
                .ToListAsync();

            // 2) Build the Excel file
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseAssignments");

            // Headers (includes audit fields)
            ws.Cell(1, 1).Value = "PkCourseAssignment";
            ws.Cell(1, 2).Value = "Position";
            ws.Cell(1, 3).Value = "Course";
            ws.Cell(1, 4).Value = "Required Level";
            ws.Cell(1, 5).Value = "Required";
            ws.Cell(1, 6).Value = "Available";
            ws.Cell(1, 7).Value = "Delivery Mode";
            ws.Cell(1, 8).Value = "CreateUser";
            ws.Cell(1, 9).Value = "CreateDate";
            ws.Cell(1, 10).Value = "LastUpdateUser";
            ws.Cell(1, 11).Value = "LastUpdateDate";

            var header = ws.Range("A1:K1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // 3) Data (mapped directly per type)
            int row = 2;
            foreach (var it in data)
            {
                ws.Cell(row, 1).Value = it.PkCourseAssignment;
                ws.Cell(row, 2).Value = it.PositionName;
                ws.Cell(row, 3).Value = it.CourseName;
                ws.Cell(row, 4).Value = it.RequiredCourseLevelDescription;

                // Requiered is bool -> "Yes/No"
                ws.Cell(row, 5).Value = it.Requiered ? "Yes" : "No";

                // Available is int (0/1) -> "Yes/No"
                ws.Cell(row, 6).Value = it.Available == 1 ? "Yes" : "No";

                ws.Cell(row, 7).Value = it.DeliveryModeDescription ?? ""; // empty if null

                ws.Cell(row, 8).Value = it.CreateUser;
                ws.Cell(row, 9).Value = it.CreateDate;
                ws.Cell(row, 10).Value = it.LastUpdateUser;
                ws.Cell(row, 11).Value = it.LastUpdateDate;

                row++;
            }

            // 4) Styles and formatting
            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 11);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            // Date/time formats
            ws.Column(9).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
            ws.Column(11).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

            // Auto-fit columns and freeze the header row
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // 5) Download
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

        /// <summary>
        /// Raw export of every column in CtCourseassignments, with no joins or translations,
        /// so staff can cross-check the data behind the Course Assignments catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportCourseAssignmentsFullData()
        {
            var data = await _context.CtCourseassignments.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "CourseAssignments");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("CourseAssignments"));
        }

        private void LoadCourseAssignmentViewBags()
        {
            ViewBag.Positions = _context.CtPositions
                .AsNoTracking()
                .Where(p => p.Available == 1)
                .ToList();

            ViewBag.Courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .ToList();

            ViewBag.LevelCourses = _context.CtLevelcourses
                .AsNoTracking()
                .Where(lc => lc.Available == 1)
                .ToList();

            ViewBag.DeliveryModes = _context.CtDeliverymodes
                .AsNoTracking()
                .Where(dm => dm.Available == 1)
                .ToList();
        }

        /// <summary>
        /// Displays the form to create a new course assignment.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourseAssignment()
        {
            LoadCourseAssignmentViewBags();
            return View("CourseAssignments/CreateCourseAssignment");
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignment(CtCourseassignment courseAssignment)
        {
            // Validate required fields
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.PositionIsRequiredPleaseSelectAValid;
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.CourseIsRequiredPleaseSelectAValid;
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.RequiredCourseLevelIsRequiredPleaseSelect;
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Validate duplicates
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels);

            if (exists)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.CombinationOfPositionCourseAndRequiredCourse;
                LoadCourseAssignmentViewBags();
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Save if everything is valid
            courseAssignment.CreateUser = User.Identity?.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;
            courseAssignment.Available = 1;
            _context.CtCourseassignments.Add(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseAssignmentMessages.CourseAssignmentCreatedSuccessfully;
            LoadCourseAssignmentViewBags();
            return RedirectToAction(nameof(CreateCourseAssignment));
        }

        /// <summary>
        /// Displays the form to bulk-create course assignments.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourseAssignmentBulk()
        {
            LoadCourseAssignmentViewBags();
            return View("CourseAssignments/CreateCourseAssignmentBulk");
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignmentBulk(
            [FromForm] int[] SelectedPositions,
            [FromForm] int[] SelectedCourses,
            [FromForm] int[] SelectedLevels,      // <-- NEW
            [FromForm] int FkDeliveryMode,
            //[FromForm] bool Requiered
            [FromForm] bool Requiered)
        {
            bool required = Requiered;  // already correct as-is

            // Basic validations
            if (SelectedPositions == null || SelectedPositions.Length == 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.SelectAtLeastOnePosition;
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (SelectedCourses == null || SelectedCourses.Length == 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.SelectAtLeastOneCourse;
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (SelectedLevels == null || SelectedLevels.Length == 0)     // <-- NEW
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.SelectAtLeastOneCourseLevel;
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }
            if (FkDeliveryMode <= 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.DeliveryModeIsRequired;
                return RedirectToAction(nameof(CreateCourseAssignmentBulk));
            }

            // Helper for the TVP (table-valued parameter)
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
                cmd.Parameters.Add(new SqlParameter("@Requiered", SqlDbType.Bit) { Value = required });
                cmd.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = user });

                conn.Open();
                int affected = cmd.ExecuteNonQuery();

                if (affected == 0)
                    TempData["ErrorMessage"] = CourseAssignmentMessages.NoNewAssignmentsWereGeneratedPossibleDuplicates;
                else
                    TempData["SuccessMessage"] = string.Format(CourseAssignmentMessages.NewAssignmentsWereCreatedFormat, affected);
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.ErrorOccurredWhileCreatingTheBulkAssignments;
            }

            return RedirectToAction(nameof(CreateCourseAssignmentBulk));
        }


        /// <summary>
        /// Displays the form to edit an existing course assignment.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult EditCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment == null) return NotFound();

            LoadCourseAssignmentViewBags();
            return View("CourseAssignments/EditCourseAssignment", courseAssignment);
        }

        /// <summary>
        /// Updates an existing course assignment.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseAssignment(int id, CtCourseassignment form)
        {
            if (id != form.PkCourseAssignment) return NotFound();

            // Validaciones de negocio (tus mismas)
            if (form.FkPosition <= 0) { TempData["ErrorMessage"] = CourseAssignmentMessages.PositionIsRequired; return RedirectToAction(nameof(EditCourseAssignment), new { id }); }
            if (form.FkCourse <= 0) { TempData["ErrorMessage"] = CourseAssignmentMessages.CourseIsRequired; return RedirectToAction(nameof(EditCourseAssignment), new { id }); }
            if (form.FkRequiredCourseLevels <= 0) { TempData["ErrorMessage"] = CourseAssignmentMessages.RequiredCourseLevelIsRequired; return RedirectToAction(nameof(EditCourseAssignment), new { id }); }

            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == form.FkPosition &&
                ca.FkCourse == form.FkCourse &&
                ca.FkRequiredCourseLevels == form.FkRequiredCourseLevels &&
                ca.PkCourseAssignment != id);

            if (exists)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.CombinationAlreadyExists;
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            var entity = _context.CtCourseassignments.Find(id);
            if (entity == null) return NotFound();

            // === Only update the editable fields ===
            entity.FkPosition = form.FkPosition;
            entity.FkCourse = form.FkCourse;
            entity.FkRequiredCourseLevels = form.FkRequiredCourseLevels;
            entity.FkDeliveryMode = form.FkDeliveryMode;
            entity.Requiered = form.Requiered;
            entity.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            entity.LastUpdateDate = DateTime.Now;

            _context.SaveChanges();

            TempData["SuccessMessage"] = CourseAssignmentMessages.CourseAssignmentUpdatedSuccessfully;
            return RedirectToAction(nameof(EditCourseAssignment), new { id });  // pasa el id!
        }

        /// <summary>
        /// Deletes a course assignment.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseAssignment(int id)
        {
            var dependency = await _catalogIntegrityService.CourseAssignmentDependencyAsync(new[] { id });
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexCourseassignments));
            }

            var courseAssignment = await _context.CtCourseassignments.FindAsync(id);
            if (courseAssignment != null)
            {
                _context.CtCourseassignments.Remove(courseAssignment);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = CourseAssignmentMessages.CourseAssignmentDeletedSuccessfully;
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "The assignment could not be deleted because it is now referenced by training history. Disable it instead.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.CourseAssignmentNotFound;
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        /// <summary>
        /// Toggles a course assignment's availability.
        /// </summary>
        [HttpPost]
        [Route("ToggleCourseAssignment")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseAssignment(int id)
        {
            var courseAssignment = await _context.CtCourseassignments.FindAsync(id);
            if (courseAssignment != null)
            {
                // Alternar el valor de Available
                courseAssignment.Available = courseAssignment.Available == 1 ? 0 : 1;
                courseAssignment.LastUpdateUser = User.Identity?.Name ?? "Unknown";
                courseAssignment.LastUpdateDate = DateTime.Now;

                _context.CtCourseassignments.Update(courseAssignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = CourseAssignmentMessages.CourseAssignmentAvailabilityStatusWasSuccessfullyUpdated;
            }
            else
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.CourseAssignmentNotFound;
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        /// <summary>
        /// Displays course assignments so the user can select which ones to delete.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult IndexDeleteCourseassignments()
        {
            var model = _context.CtCourseassignments
                .AsNoTracking()
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

            return View("CourseAssignments/IndexDeleteCourseassignments", model);
        }


        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseAssignmentsBulk([FromForm] int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.SelectAtLeastOneRecord;
                return RedirectToAction(nameof(IndexCourseassignments));
            }

            var dependency = await _catalogIntegrityService.CourseAssignmentDependencyAsync(selectedIds);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexCourseassignments));
            }

            // TVP helper (same as used in CreateBulk)
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
                    TempData["ErrorMessage"] = CourseAssignmentMessages.NoRecordsWereDeletedNonexistentIds;
                else
                    TempData["SuccessMessage"] = string.Format(CourseAssignmentMessages.AssignmentsWereDeletedFormat, deleted);
            }
            catch (SqlException)
            {
                TempData["ErrorMessage"] = CourseAssignmentMessages.ErrorOccurredWhileDeletingTheAssignments;
            }

            return RedirectToAction(nameof(IndexCourseassignments));
        }


    }
}
