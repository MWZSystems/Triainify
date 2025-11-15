using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    
    public class HeadCountController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public HeadCountController(db_abcd61_rhchdbContext context,
                                    UserManager<IdentityUser> userManager,
                                SignInManager<IdentityUser> signInManager )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Index()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            var positions = await _context.CtPositions.ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .Where(hc => hc.Available == 1) // Filtrar por Available = 1
                .ToListAsync();

            // Calcular edades
            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts,
                HeadCountAges = headCountAges // Agregar edades al modelo
            };

            return View(model);
        }

        // Método para calcular la edad a partir de la fecha de nacimiento
        private int CalculateAge(DateTime birthdate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthdate.Year;
            if (birthdate.Date > today.AddYears(-age)) age--;
            return age;
        }
        public async Task<IActionResult> ExportHeadCountToExcel()
        {
            // Obtener los datos necesarios para el archivo Excel
            var departments = await _context.CtDepartments.ToListAsync();
            var positions = await _context.CtPositions.ToListAsync();
            //var syHeadCounts = await _context.SyHeadcounts.ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                                .Where(h => h.Available == 1)
                                .ToListAsync();
            var supervisors = await _context.CtSupervisors.ToListAsync();

            // Calcular edades
            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            // Crear el archivo Excel
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("HeadCountList");

                // Encabezados
                var headerRow = worksheet.Row(1);
                headerRow.Cell(1).Value = "Control Number";
                headerRow.Cell(2).Value = "Names";
                headerRow.Cell(3).Value = "Last Name";
                headerRow.Cell(4).Value = "Second Name";
                headerRow.Cell(5).Value = "Level";
                headerRow.Cell(6).Value = "Shift";
                headerRow.Cell(7).Value = "Start Date";
                headerRow.Cell(8).Value = "Department";
                headerRow.Cell(9).Value = "Position";
                headerRow.Cell(10).Value = "Supervisor";
                headerRow.Cell(11).Value = "Birthdate";
                headerRow.Cell(12).Value = "Age"; 
                headerRow.Cell(13).Value = "Curp";
                headerRow.Cell(14).Value = "RFC";
                headerRow.Cell(15).Value = "Social Security";
                headerRow.Cell(16).Value = "Sex";
                headerRow.Cell(17).Value = "Marital Status";
                headerRow.Cell(18).Value = "Phone 1";
                headerRow.Cell(19).Value = "Phone 2";
                headerRow.Cell(20).Value = "Email";
                headerRow.Cell(21).Value = "Education Level";
                headerRow.Cell(22).Value = "Specialization";
                headerRow.Cell(23).Value = "Street";
                headerRow.Cell(24).Value = "Neighborhood";
                headerRow.Cell(25).Value = "City";
                headerRow.Cell(26).Value = "Zip Code";

                // Aplicar estilo al encabezado
                var headerRange = worksheet.Range("A1:Z1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

                // Llenar los datos
                int row = 2;
                foreach (var item in syHeadCounts)
                {
                    worksheet.Cell(row, 1).Value = item.ControlNumber;
                    worksheet.Cell(row, 2).Value = item.Names;
                    worksheet.Cell(row, 3).Value = item.LastName;
                    worksheet.Cell(row, 4).Value = item.SecondName;
                    worksheet.Cell(row, 5).Value = item.LevelEmployee;
                    worksheet.Cell(row, 6).Value = item.ShiftWork;
                    worksheet.Cell(row, 7).Value = item.StarDate.ToShortDateString();
                    worksheet.Cell(row, 8).Value = departments.FirstOrDefault(d => d.PkDepartment == item.FkDepartment)?.NameDeparment;
                    worksheet.Cell(row, 9).Value = positions.FirstOrDefault(p => p.PkPosition == item.FkPosition)?.NamePosition;
                    //worksheet.Cell(row, 10).Value = item.FkSupervisorId != 0
                    //    ? $"{syHeadCounts.FirstOrDefault(p => p.FkSupervisorId == item.FkSupervisorId)?.Names} {syHeadCounts.FirstOrDefault(p => p.PkHeadcount == item)?.LastName} {syHeadCounts.FirstOrDefault(p => p.PkHeadcount == item.Supervisor)?.SecondName}"
                    //    : "Not Aissgned";
                    worksheet.Cell(row, 10).Value = item.FkSupervisorId != null
                                                        ? syHeadCounts
                                                            .FirstOrDefault(h =>
                                                                h.PkHeadcount ==
                                                                supervisors
                                                                    .FirstOrDefault(s => s.PkSupervisorId == item.FkSupervisorId)
                                                                    ?.FkHeadcount
                                                            ) is var sup && sup != null
                                                                ? $"{sup.Names} {sup.LastName} {sup.SecondName}"
                                                                : ""
                                                        : "";
                    worksheet.Cell(row, 11).Value = item.Birthdate.ToShortDateString();
                    worksheet.Cell(row, 12).Value = headCountAges.GetValueOrDefault(item.PkHeadcount, 0);
                    worksheet.Cell(row, 13).Value = item.Curp;
                    worksheet.Cell(row, 14).Value = item.Rfc;
                    worksheet.Cell(row, 15).Value = item.SocialSecurity;
                    worksheet.Cell(row, 16).Value = item.Sex;
                    worksheet.Cell(row, 17).Value = item.MaritalStatus;
                    worksheet.Cell(row, 18).Value = item.Phone1;
                    worksheet.Cell(row, 19).Value = item.Phone2;
                    worksheet.Cell(row, 20).Value = item.Email;
                    worksheet.Cell(row, 21).Value = item.EducationLevel;
                    worksheet.Cell(row, 22).Value = item.Specialization;
                    worksheet.Cell(row, 23).Value = item.Street;
                    worksheet.Cell(row, 24).Value = item.Neighborhood;
                    worksheet.Cell(row, 25).Value = item.City;
                    worksheet.Cell(row, 26).Value = item.ZipCode;
                    row++;
                }

                // Ajustar el ancho de las columnas
                worksheet.Columns().AdjustToContents();

                // Agregar bordes al rango de datos
                var dataRange = worksheet.Range(1, 1, row - 1, 26);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Aplicar alineación de texto
                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // Guardar y devolver el archivo Excel
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HeadCountList.xlsx");
                }
            }
        }

        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> HeadCountUnavailable()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            var positions = await _context.CtPositions.ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .Where(hc => hc.Available == 0) // Filtrar por Available = 1
                .ToListAsync();

            // Calcular edades
            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts,
                HeadCountAges = headCountAges // Agregar edades al modelo
            };

            return View(model);
        }

        // GET: SyHeadCounts
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")] //OnBoardingView
        public async Task<IActionResult> IndexHeadCount()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            var positions = await _context.CtPositions.ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
            .Where(h => h.Available == 1)
            .ToListAsync();


            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts
            };

            return View(model);
        }

        // GET: SyHeadCounts
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")] //OnBoardingView
        public async Task<IActionResult> IndexOFFBoarding()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            var positions = await _context.CtPositions.ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
            .Where(h => h.Available == 0)
            .ToListAsync();


            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts
            };

            // Usa el nombre real del .cshtml que ya tienes
            return View(model);
        }

        // Método de inicialización simplificado
        private CreateHeadCountViewModel InitializeCreateHeadCount()
        {
            var departments = _context.CtDepartments.ToList();
            var positions = _context.CtPositions.ToList();

            var supervisors = (
                from s in _context.CtSupervisors
                join h in _context.SyHeadcounts
                    on s.FkHeadcount equals h.PkHeadcount
                where s.Available == 1 && h.Available == 1
                select new SupervisorDisplayViewModel
                {
                    PkSupervisorId = s.PkSupervisorId,
                    FkHeadcount = s.FkHeadcount,
                    ControlNumber = h.ControlNumber,
                    Names = h.Names,
                    SecondName = h.SecondName,
                    LastName = h.LastName
                }
            ).ToList();

            var model = new CreateHeadCountViewModel
            {
                Departments = departments,
                Position = positions,
                Supervisors = supervisors
            };

            return model;
        }

        // GET: HeadCount/Create
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateHeadCount()
        {
            var model = InitializeCreateHeadCount();
            return View(model);
        }

        [HttpPost]
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateHeadCount(CreateHeadCountViewModel model)
        {
            // Helper method to reload view data
            void ReloadViewData()
            {
                model.Departments = _context.CtDepartments.ToList();
                model.Position = _context.CtPositions.ToList();
                model.Supervisors = (
                    from s in _context.CtSupervisors
                    join h in _context.SyHeadcounts
                        on s.FkHeadcount equals h.PkHeadcount
                    where s.Available == 1 && h.Available == 1
                    select new SupervisorDisplayViewModel
                    {
                        PkSupervisorId = s.PkSupervisorId,
                        FkHeadcount = s.FkHeadcount,
                        ControlNumber = h.ControlNumber,
                        Names = h.Names,
                        SecondName = h.SecondName,
                        LastName = h.LastName
                    }
                ).ToList();
            }

            // Helper method to return with error
            IActionResult ReturnWithError(string errorMessage)
            {
                TempData["ErrorMessage"] = errorMessage;
                ReloadViewData();
                return View(model);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    ReloadViewData();
                    return View(model);
                }

                // Validate LastName or SecondName
                if (string.IsNullOrWhiteSpace(model.LastName) && string.IsNullOrWhiteSpace(model.SecondName))
                    return ReturnWithError("Please provide either a Last Name or a Second Name.");

                // Validate duplicate ControlNumber
                if (_context.SyHeadcounts.Any(h => h.ControlNumber == model.ControlNumber))
                    return ReturnWithError("The Control Number already exists.");
                
                // Validate duplicate username
                if (_context.AspNetUsers.Any(u => u.Ntuser == model.Ntuser))
                    return ReturnWithError("The UserName "+ model.Ntuser  + " already exists.");

                // Create entity
                var headCount = new SyHeadcount
                {
                    ControlNumber = model.ControlNumber,
                    Photo = model.Photo ?? new byte[0],
                    Names = model.Names,
                    LastName = model.LastName ?? "",
                    SecondName = model.SecondName ?? "",
                    LevelEmployee = model.LevelEmployee,
                    ShiftWork = model.ShiftWork,
                    StarDate = model.StarDate,
                    Curp = model.Curp,
                    Rfc = model.Rfc,
                    SocialSecurity = model.SocialSecurity,
                    Birthdate = model.Birthdate,
                    Sex = model.Sex,
                    MaritalStatus = model.MaritalStatus ?? "",
                    Street = model.Street,
                    Neighborhood = model.Neighborhood,
                    City = model.City ?? "CHIHUAHUA",
                    ZipCode = model.ZipCode,
                    Phone1 = model.Phone1,
                    Phone2 = model.Phone2 ?? "0",
                    Email = model.Email ?? "",
                    EducationLevel = model.EducationLevel ?? "",
                    Specialization = model.Specialization ?? "",
                    FkDepartment = model.FkDepartment,
                    FkPosition = model.FkPosition,
                    ZipCodesat = model.ZipCodesat,
                    FkSupervisorId = model.FkSupervisorId,
                    Createuser = "ADMIN",
                    Lastuser = "ADMIN",
                    Createdate = DateTime.Now,
                    Lastupdate = DateTime.Now,
                    Available = 1
                };

                //save the heacount creation
                _context.SyHeadcounts.Add(headCount);
                _context.SaveChanges();

                //If everythig sucess when creating an inboud, create his user aswell
                var usuario = new AppUsuario
                {
                    UserName = model.Ntuser,
                    EmployeeNumber = model.ControlNumber.ToString(),
                    Email = model.Email,
                    Names = model.Names,
                    LastName = model.LastName,
                    Available = 1,
                    CreateDate = DateTime.Today,
                    Ntuser = model.Ntuser
                };

                var resultado = await _userManager.CreateAsync(usuario, "Temp12345!"); //Generic Password for new users.

                if (resultado.Succeeded)
                {
                    // Asignar el rol al usuario
                    await _userManager.AddToRoleAsync(usuario, "Empleado");

                    TempData["SuccessMessage"] = "Head count created successfully.";
                    return RedirectToAction(nameof(IndexHeadCount));
                }

                TempData["ErrorMessage"] = "HeadCount Added, but user could not be created.";
                return RedirectToAction(nameof(IndexHeadCount));


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the head count: {ex.Message}";
                ReloadViewData();
                return View(model);
            }
        }

        [HttpGet]
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult EditHeadCount(int id)
        {
            var headCount = _context.SyHeadcounts.Find(id);
            if (headCount == null)
            {
                return NotFound();
            }

            var viewModel = new EditHeadCountViewModel
            {
                PkHeadcount = headCount.PkHeadcount,
                ControlNumber = headCount.ControlNumber,
                Names = headCount.Names,
                LastName = headCount.LastName ?? "",
                SecondName = headCount.SecondName ?? "",
                LevelEmployee = headCount.LevelEmployee,
                ShiftWork = headCount.ShiftWork,
                StarDate = headCount.StarDate,
                Curp = headCount.Curp,
                Rfc = headCount.Rfc,
                SocialSecurity = headCount.SocialSecurity,
                Birthdate = headCount.Birthdate,
                Sex = headCount.Sex,
                MaritalStatus = headCount.MaritalStatus ?? "",
                Street = headCount.Street,
                Neighborhood = headCount.Neighborhood,
                City = headCount.City ?? "CHIHUAHUA",
                ZipCode = headCount.ZipCode,
                Phone1 = headCount.Phone1,
                Phone2 = headCount.Phone2 ?? "0",
                Email = headCount.Email ?? "",
                EducationLevel = headCount.EducationLevel ?? "",
                Specialization = headCount.Specialization ?? "",
                FkDepartment = headCount.FkDepartment,
                FkPosition = headCount.FkPosition,
                ZipCodesat = headCount.ZipCodesat,
                FkSupervisorId = headCount.FkSupervisorId,
                Departments = _context.CtDepartments.ToList(),
                Position = _context.CtPositions.ToList(),
                Supervisors = (
                    from supervisor in _context.CtSupervisors
                    join sHead in _context.SyHeadcounts
                        on supervisor.FkHeadcount equals sHead.PkHeadcount
                    where supervisor.Available == 1 && sHead.Available == 1
                    select new SupervisorDisplayViewModel
                    {
                        PkSupervisorId = supervisor.PkSupervisorId,
                        FkHeadcount = supervisor.FkHeadcount,
                        ControlNumber = sHead.ControlNumber,
                        Names = sHead.Names,
                        SecondName = sHead.SecondName,
                        LastName = sHead.LastName
                    }
                ).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHeadCount(EditHeadCountViewModel viewModel)
        {
            // Helper method to reload view data
            void ReloadViewData()
            {
                viewModel.Departments = _context.CtDepartments.ToList();
                viewModel.Position = _context.CtPositions.ToList();
                viewModel.Supervisors = (
                    from supervisor in _context.CtSupervisors
                    join sHead in _context.SyHeadcounts
                        on supervisor.FkHeadcount equals sHead.PkHeadcount
                    where supervisor.Available == 1 && sHead.Available == 1
                    select new SupervisorDisplayViewModel
                    {
                        PkSupervisorId = supervisor.PkSupervisorId,
                        FkHeadcount = supervisor.FkHeadcount,
                        ControlNumber = sHead.ControlNumber,
                        Names = sHead.Names,
                        SecondName = sHead.SecondName,
                        LastName = sHead.LastName
                    }
                ).ToList();
            }

            // Helper method to set error message and reload view
            IActionResult ReturnWithError(string errorMessage)
            {
                TempData["ErrorMessage"] = errorMessage;
                ReloadViewData();
                return View(viewModel);
            }

            try
            {
                var headCount = await _context.SyHeadcounts.FindAsync(viewModel.PkHeadcount);
                if (headCount == null)
                {
                    TempData["ErrorMessage"] = "The record was not found.";
                    return RedirectToAction(nameof(IndexHeadCount));
                }

                // Validar el modelo
                if (!ModelState.IsValid)
                {
                    ReloadViewData();
                    return View(viewModel);
                }

                // Validar LastName o SecondName
                if (string.IsNullOrWhiteSpace(viewModel.LastName) && string.IsNullOrWhiteSpace(viewModel.SecondName))
                    return ReturnWithError("Please provide either a Last Name or a Second Name.");

                // Validar duplicados en ControlNumber, excluyendo el registro actual
                if (_context.SyHeadcounts.Any(h => h.ControlNumber == viewModel.ControlNumber && h.PkHeadcount != viewModel.PkHeadcount))
                    return ReturnWithError("The 'Control Number' already exists.");

                // Actualizar headCount con datos validados
                headCount.ControlNumber = viewModel.ControlNumber;
                headCount.Names = viewModel.Names;
                headCount.LastName = viewModel.LastName ?? "";
                headCount.SecondName = viewModel.SecondName ?? "";
                headCount.LevelEmployee = viewModel.LevelEmployee;
                headCount.ShiftWork = viewModel.ShiftWork;
                headCount.StarDate = viewModel.StarDate;
                headCount.Curp = viewModel.Curp;
                headCount.Rfc = viewModel.Rfc;
                headCount.SocialSecurity = viewModel.SocialSecurity;
                headCount.Birthdate = viewModel.Birthdate;
                headCount.Sex = viewModel.Sex;
                headCount.MaritalStatus = viewModel.MaritalStatus ?? "";
                headCount.Street = viewModel.Street;
                headCount.Neighborhood = viewModel.Neighborhood;
                headCount.City = viewModel.City ?? "CHIHUAHUA";
                headCount.ZipCode = viewModel.ZipCode;
                headCount.Phone1 = viewModel.Phone1;
                headCount.Phone2 = viewModel.Phone2 ?? "0";
                headCount.Email = viewModel.Email ?? "";
                headCount.EducationLevel = viewModel.EducationLevel ?? "";
                headCount.Specialization = viewModel.Specialization ?? "";
                headCount.FkDepartment = viewModel.FkDepartment;
                headCount.FkPosition = viewModel.FkPosition;
                headCount.ZipCodesat = viewModel.ZipCodesat;
                headCount.FkSupervisorId = viewModel.FkSupervisorId;
                headCount.Lastuser = "ADMIN";
                headCount.Lastupdate = DateTime.Now;

                _context.Update(headCount);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The record has been successfully updated.";
                return RedirectToAction(nameof(IndexHeadCount));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the head count: {ex.Message}";
                ReloadViewData();
                return View(viewModel);
            }
        }

        // POST: SyHeadCounts/Delete/5
        [HttpPost, ActionName("DeleteHeadCount")]
        //[Authorize(Roles = "Administrador, RHGerente")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHeadCountConfirmed(int id)
        {
            var headCount = await _context.SyHeadcounts.FindAsync(id);
            if (headCount != null)
            {
                // Elimina el registro de la base de datos
                _context.SyHeadcounts.Remove(headCount);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Head count deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Head count not found.";
            }
            return RedirectToAction(nameof(IndexHeadCount));
        }

        // POST: SyHeadCounts/ToggleAvailability/5
        [HttpPost, ActionName("ToggleAvailabilityHeadCount")]
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailabilityConfirmedHeadCount(int id)
        {
            var headCount = await _context.SyHeadcounts.FindAsync(id);
            if (headCount == null)
            {
                TempData["ErrorMessage"] = "Head count not found.";
                return RedirectToAction(nameof(IndexHeadCount));
            }

            // Determina el nuevo valor
            var newAvailable = (headCount.Available == 1) ? 0 : 1;

            // Aplica cambios
            headCount.Available = newAvailable;
            headCount.Layoffday = (newAvailable == 0) ? DateTime.Now : (DateTime?)null;

            // (Opcional) auditoría
            // headCount.LastUpdateUser = User.Identity?.Name ?? "system";
            // headCount.LastUpdateDate = DateTime.Now;

            try
            {
                _context.Update(headCount);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = (newAvailable == 1)
                    ? "Head count enabled successfully."
                    : "Head count disabled successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Concurrency error while updating the head count.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Unexpected error while updating the head count: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexHeadCount));
        }


    }
}
