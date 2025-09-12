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
using RH_CM.ViewModels;
using System.Data;

namespace RH_CM.Controllers
{
    public class ReportsController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportsController(db_abcd61_rhchdbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private void LoadPositions(int selectedFkPosition = 0)
        {
            var positions = _context.CtPositions
                .AsNoTracking()
                .Where(p => p.Available == 1)
                .OrderBy(p => p.NamePosition)
                .Select(p => new
                {
                    p.PkPosition,
                    Name = p.NamePosition
                })
                .ToList();

            ViewBag.Positions = positions;
            ViewBag.SelectedFkPosition = selectedFkPosition;
        }

        // === CARGA DESDE SP (misma forma que MatrizbyEmployee) ===
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CoursesByPosition(int? fkPosition)
        {
            // Cargar siempre el combo (NamePosition)
            LoadPositions(fkPosition ?? 0);

            var result = new List<CoursesByPositionViewModel>();

            if (fkPosition is null || fkPosition <= 0)
            {
                TempData["ErrorMessage"] = TempData["ErrorMessage"] ?? "Selecciona una posición para consultar sus cursos.";
                return View(result);
            }

            try
            {
                string connectionString = _context.Database.GetDbConnection().ConnectionString;

                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new SqlCommand("sp_GetCoursesByPosition", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.Add(new SqlParameter("@FkPosition", SqlDbType.Int) { Value = fkPosition.Value });

                await using var reader = await command.ExecuteReaderAsync();

                // Helpers idénticos a MatrizbyEmployee
                int Ord(string n) => reader.GetOrdinal(n);
                bool IsNull(string n) => reader.IsDBNull(Ord(n));

                while (await reader.ReadAsync())
                {
                    result.Add(new CoursesByPositionViewModel
                    {
                        // columnas según tu muestra del SP
                        FK_Position = IsNull("FK_Position") ? 0 : reader.GetInt32(Ord("FK_Position")),
                        PositionName = IsNull("PositionName") ? "" : reader.GetString(Ord("PositionName")),

                        PK_CourseAssignment = IsNull("PK_CourseAssignment") ? 0 : reader.GetInt32(Ord("PK_CourseAssignment")),
                        FK_Course = IsNull("FK_Course") ? 0 : reader.GetInt32(Ord("FK_Course")),
                        CourseName = IsNull("CourseName") ? "" : reader.GetString(Ord("CourseName")),

                        // si LevelId es INT en SQL, GetInt32 es correcto; si fuera smallint/tinyint, usa Convert.ToInt32
                        LevelId = IsNull("LevelId") ? (int?)null : reader.GetInt32(Ord("LevelId")),
                        LevelName = IsNull("LevelName") ? null : reader.GetString(Ord("LevelName")),

                        // Requiered en DB puede ser bit o int -> Convert.ToBoolean es seguro
                        Requiered = !IsNull("Requiered") && Convert.ToBoolean(reader["Requiered"]),

                        // DeliveryModeId puede venir como int; Convert.ToInt32 es seguro
                        DeliveryModeId = IsNull("DeliveryModeId") ? (int?)null : Convert.ToInt32(reader["DeliveryModeId"]),
                        DeliveryMode = IsNull("DeliveryMode") ? null : reader["DeliveryMode"]?.ToString(),

                        CourseValidityDays = IsNull("CourseValidityDays") ? (int?)null : Convert.ToInt32(reader["CourseValidityDays"])
                    });
                }

                if (!result.Any())
                {
                    TempData["ErrorMessage"] = "No hay cursos asignados para la posición seleccionada.";
                }
                else
                {
                    // El header de la vista prioriza NamePosition del combo; este mensaje usa lo que vino del SP
                    TempData["SuccessMessage"] = $"Se encontraron {result.Count} asignaciones para la posición {result.First().PositionName}.";
                }
            }
            catch (SqlException ex)
            {
                TempData["ErrorMessage"] = $"Error SQL al consultar los cursos: {ex.Message}";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al consultar los cursos por posición.";
            }

            return View(result);
        }

        // GET: ReportsController
        public ActionResult MatrizByEmployeesCheck()
        {
            return View();
        }

        // GET: ReportsController
        public ActionResult MatrizByEmployeesGeneral()
        {
            return View();
        }

        // GET: ReportsController
        public ActionResult MatrizByDeparmentGeneral_RH()
        {
            return View();
        }

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> HeadCountbySupervisor()
        {
            return View();
        }
    }
}
