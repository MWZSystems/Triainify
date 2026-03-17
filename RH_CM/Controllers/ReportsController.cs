using ClosedXML;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;
using RH_CM.ViewModels;
using System.Data;
using System.Globalization;

namespace RH_CM.Controllers
{
    public class ReportsController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UnitOfWork _unitOfWork;

        public ReportsController(db_abcd61_rhchdbContext context, 
                                UserManager<IdentityUser> userManager,
                                UnitOfWork unitOfWork)
        {
            _context = context;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        // ================= Helper: combo de supervisores =================
        private async Task<List<SelectListItem>> GetSupervisorsAsync()
        {
            var rows = await (
                from s in _context.CtSupervisors.AsNoTracking()
                join h in _context.SyHeadcounts.AsNoTracking()
                    on s.FkHeadcount equals h.PkHeadcount
                where s.Available == 1 && h.Available == 1
                orderby h.ControlNumber, h.Names, h.LastName, h.SecondName
                select new
                {
                    s.PkSupervisorId,
                    h.ControlNumber,
                    h.Names,
                    h.LastName,
                    h.SecondName
                }
            ).ToListAsync();

            return rows
                .Select(x =>
                {
                    var fullName = string.Join(" ",
                        new[] { x.Names, x.LastName, x.SecondName }
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .Select(p => p!.Trim())
                    );

                    return new SelectListItem
                    {
                        Value = x.PkSupervisorId.ToString(),
                        Text = $"[{x.ControlNumber}] {fullName} — Supervisor #{x.PkSupervisorId}"
                    };
                })
                .ToList();
        }

        // ------------------- Window A: Selector -------------------
        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> MatrizbySupervisorSelect(string? managementSystem)
        {
            var vm = new SelectSupervisorViewModel
            {
                Supervisors = await GetSupervisorsAsync()
            };

            ViewBag.ManagementSystem = managementSystem;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> MatrizbySupervisorSelect(SelectSupervisorViewModel vm, string? managementSystem)
        {
            if (!vm.SelectedSupervisorId.HasValue || vm.SelectedSupervisorId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Select a valid supervisor";
                vm.Supervisors = await GetSupervisorsAsync();
                ViewBag.ManagementSystem = managementSystem;
                return View(vm);
            }

            return RedirectToAction(nameof(MatrizbySupervisor), new
            {
                supervisorId = vm.SelectedSupervisorId.Value,
                managementSystem = managementSystem
            });
        }

        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> MatrizbySupervisor(int supervisorId, string? managementSystem)
        {
            var result = new List<MatrizBySupervisorViewModel>();

            if (supervisorId <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid supervisor first.";
                return RedirectToAction(nameof(MatrizbySupervisorSelect));
            }

            try
            {
                string connectionString = _context.Database.GetDbConnection().ConnectionString;

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("dbo.sp_GetMatrizbySupervisorCourseAssignments", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.Add(new SqlParameter("@SupervisorId", SqlDbType.Int)
                {
                    Value = supervisorId
                });

                command.Parameters.Add(new SqlParameter("@ManagementSystem", SqlDbType.NVarChar, 50)
                {
                    Value = string.IsNullOrWhiteSpace(managementSystem)
                        ? (object)DBNull.Value
                        : managementSystem
                });

                await using var reader = await command.ExecuteReaderAsync();

                int Ord(string n) => reader.GetOrdinal(n);
                bool IsNull(string n) => reader.IsDBNull(Ord(n));

                while (await reader.ReadAsync())
                {
                    result.Add(new MatrizBySupervisorViewModel
                    {
                        PK_HEADCOUNT = IsNull("PK_HEADCOUNT") ? 0 : reader.GetInt32(Ord("PK_HEADCOUNT")),
                        UserName = IsNull("UserName") ? null : reader.GetString(Ord("UserName")),
                        CONTROL_NUMBER = IsNull("CONTROL_NUMBER") ? null : reader["CONTROL_NUMBER"]?.ToString(),
                        NAMES = IsNull("NAMES") ? null : reader["NAMES"]?.ToString(),
                        LAST_NAME = IsNull("LAST_NAME") ? null : reader["LAST_NAME"]?.ToString(),
                        SECOND_NAME = IsNull("SECOND_NAME") ? null : reader["SECOND_NAME"]?.ToString(),
                        FK_Position = IsNull("FK_Position") ? 0 : Convert.ToInt32(reader["FK_Position"]),
                        NAME_POSITION_ENGLISH = IsNull("NAME_POSITION_ENGLISH") ? null : reader["NAME_POSITION_ENGLISH"]?.ToString(),
                        Completed = IsNull("Completed") ? 0 : Convert.ToInt32(reader["Completed"]),
                        Pending = IsNull("Pending") ? 0 : Convert.ToInt32(reader["Pending"]),
                        ExpiringSoon = IsNull("Expiring Soon") ? 0 : Convert.ToInt32(reader["Expiring Soon"]),
                        Permanent = IsNull("Permanent") ? 0 : Convert.ToInt32(reader["Permanent"]),
                        Scheduled = IsNull("Scheduled") ? 0 : Convert.ToInt32(reader["Scheduled"])
                    });
                }

                if (result.Count == 0)
                    TempData["ErrorMessage"] = $"No employees were found for supervisor {supervisorId}.";
                else
                    TempData["SuccessMessage"] = $"{result.Count} employees were found for supervisor {supervisorId}.";
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"SQL Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            ViewBag.SupervisorId = supervisorId;
            ViewBag.ManagementSystem = managementSystem;

            return View(result);
        }
        public async Task <ActionResult> MissingMaterialExamReport()
        {
            List<MaterialExamDTOs> result = await _unitOfWork.ExecuteStoredProcedureToListAsync<MaterialExamDTOs>("sp_MissingMaterialExam");

            return View(result);
        }

        // =============================
        // EXPORTAR A EXCEL
        // =============================
        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> ExportMissingMaterialExamReport()
        {
            //Aqui lo tuve que modificar porque usaba los constrains del sql server, al borrar las ligas entre las tablas tuve que armar el query
            List<MaterialExamDTOs> data = await _unitOfWork.ExecuteStoredProcedureToListAsync<MaterialExamDTOs>("sp_MissingMaterialExam");

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CourseMissing");

            ws.Cell(1, 1).Value = "Course ID";
            ws.Cell(1, 2).Value = "Course Name";
            ws.Cell(1, 3).Value = "Level";
            ws.Cell(1, 4).Value = "Material";
            ws.Cell(1, 5).Value = "Exam";

            var header = ws.Range("A1:E1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var it in data)
            {
                ws.Cell(row, 1).Value = it.CourseID;
                ws.Cell(row, 2).Value = it.CourseName;
                ws.Cell(row, 3).Value = it.Level;
                ws.Cell(row, 4).Value = it.Material;
                ws.Cell(row, 5).Value = it.Exam;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(
                ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CourseWOMaterialExam_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }

        // GET: Reports/MatrizByDeparmentGeneral_RH
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> MatrizByDeparmentGeneral_RH(string? managementSystem)
        {
            var page = new CompletedByDepartmentPageViewModel();
            string connectionString = _context.Database.GetDbConnection().ConnectionString;

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand("dbo.sp_Reports_CompletedbyDepartment_get", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Parámetro opcional: si viene null/empty, se manda DBNull.Value
            command.Parameters.Add(new SqlParameter("@ManagementSystem", SqlDbType.NVarChar, 50)
            {
                Value = string.IsNullOrWhiteSpace(managementSystem) ? (object)DBNull.Value : managementSystem
            });

            await using var reader = await command.ExecuteReaderAsync();

            int Ord(string n) => reader.GetOrdinal(n);
            bool IsNull(string n) => reader.IsDBNull(Ord(n));

            while (await reader.ReadAsync())
            {
                var row = new CompletedbyDepartmentViewModel
                {
                    Department = IsNull("NAME_DEPARMENT") ? "—" : reader.GetString(Ord("NAME_DEPARMENT")),
                    TotalAssigned = IsNull("Total_Asignados") ? 0 : Convert.ToInt32(reader["Total_Asignados"]),
                    TotalCompleted = IsNull("Total_Completados") ? 0 : Convert.ToInt32(reader["Total_Completados"]),
                    // TotalPending es calculado en el VM, pero si prefieres asignar:
                    // TotalPending = IsNull("Total_Pendientes") ? 0 : Convert.ToInt32(reader["Total_Pendientes"]),
                    PercentCompleted = IsNull("Porcentaje_Completo_Value")
                        ? 0m
                        : Convert.ToDecimal(reader["Porcentaje_Completo_Value"], CultureInfo.InvariantCulture)
                };

                page.Rows.Add(row);
            }

            // Totales para el donut
            page.OverallAssigned = page.Rows.Sum(x => x.TotalAssigned);
            page.OverallCompleted = page.Rows.Sum(x => x.TotalCompleted);
            // OverallPending se calcula en el VM (propiedad derivada)
            if (page.OverallAssigned > 0)
            {
                page.OverallCompletedPct = Math.Round(100m * page.OverallCompleted / page.OverallAssigned, 2);
                // OverallPendingPct se deriva del VM (100 - OverallCompletedPct)
            }
            else
            {
                page.OverallCompletedPct = 0;
            }

            // Pasar el filtro actual a la vista (para remarcar UI)
            ViewBag.ManagementSystem = managementSystem;

            return View(page);
        }

    }
}
