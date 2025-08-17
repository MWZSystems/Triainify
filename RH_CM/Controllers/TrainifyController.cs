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
        private readonly RH_CHDBContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TrainifyController(RH_CHDBContext context, UserManager<IdentityUser> userManager)
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


    }
}
