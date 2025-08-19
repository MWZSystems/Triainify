using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
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

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand("sp_GetMatrizbyEmployeeCourseAssignments", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@UserName", SqlDbType.NVarChar, 256) { Value = userName });

                    using (var reader = await command.ExecuteReaderAsync())
                    {
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
                                Requiered = !IsNull("Requiered") && Convert.ToBoolean(reader["Requiered"]),
                                FK_DeliveryMode = IsNull("FK_DeliveryMode") ? 0 : Convert.ToInt32(reader["FK_DeliveryMode"]),
                                // DESCRIPTION_DELIVERYMODE   <-- NO existe en tu SP actual
                                CourseValidityDays = IsNull("CourseValidityDays") ? (int?)null : Convert.ToInt32(reader["CourseValidityDays"]),
                                UserName = IsNull("UserName") ? userName : reader["UserName"].ToString(),
                                CONTROL_NUMBER = IsNull("CONTROL_NUMBER") ? "—" : reader["CONTROL_NUMBER"].ToString(),
                                NAMES = IsNull("NAMES") ? "—" : reader["NAMES"].ToString(),
                                LAST_NAME = IsNull("LAST_NAME") ? "—" : reader["LAST_NAME"].ToString(),
                                SECOND_NAME = IsNull("SECOND_NAME") ? "—" : reader["SECOND_NAME"].ToString(),
                                LastUpdateDate = IsNull("LastUpdateDate") ? (DateTime?)null : Convert.ToDateTime(reader["LastUpdateDate"]),
                                CourseStatus = IsNull("CourseStatus") ? "—" : reader["CourseStatus"].ToString()
                            });
                        }
                    }
                }
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

    }
}
