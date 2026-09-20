using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;
using RH_CM.Messages.HeadCount;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public class HeadCountController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HeadCountController(db_abcd61_rhchdbContext context,
                                    UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Index()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(hc => hc.Available == 1)
                .ToListAsync();

            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts,
                HeadCountAges = headCountAges
            };

            return View(model);
        }

        private int CalculateAge(DateTime birthdate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthdate.Year;
            if (birthdate.Date > today.AddYears(-age)) age--;
            return age;
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportHeadCountToExcel()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                                .Where(h => h.Available == 1)
                                .ToListAsync();
            var supervisors = await _context.CtSupervisors.AsNoTracking().ToListAsync();

            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            string GetSupervisorDisplayName(int? fkSupervisorId)
            {
                if (fkSupervisorId == null)
                {
                    return "";
                }

                var supervisorRecord = supervisors.FirstOrDefault(s => s.PkSupervisorId == fkSupervisorId);
                if (supervisorRecord == null)
                {
                    return "";
                }

                var supervisorHeadCount = syHeadCounts.FirstOrDefault(h => h.PkHeadcount == supervisorRecord.FkHeadcount);
                if (supervisorHeadCount == null)
                {
                    return "";
                }

                return $"{supervisorHeadCount.Names} {supervisorHeadCount.LastName} {supervisorHeadCount.SecondName}";
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("HeadCountList");

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

                var headerRange = worksheet.Range("A1:Z1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGreen;

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
                    worksheet.Cell(row, 10).Value = GetSupervisorDisplayName(item.FkSupervisorId);
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

                worksheet.Columns().AdjustToContents();

                var dataRange = worksheet.Range(1, 1, row - 1, 26);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                dataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "HeadCountList.xlsx");
                }
            }
        }

        /// <summary>
        /// Raw export of every column in SyHeadcounts (all records, active or not, except the
        /// binary "Photo" column, which is excluded so the query doesn't have to pull every
        /// employee's photo over the wire), with no joins or translations, so staff can
        /// cross-check the data behind the Head Count catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportHeadCountFullData()
        {
            var data = await _context.SyHeadcounts.AsNoTracking()
                .Select(h => new
                {
                    h.PkHeadcount,
                    h.ControlNumber,
                    h.Names,
                    h.LastName,
                    h.SecondName,
                    h.LevelEmployee,
                    h.ShiftWork,
                    h.StarDate,
                    h.Layoffday,
                    h.Curp,
                    h.Rfc,
                    h.SocialSecurity,
                    h.Birthdate,
                    h.Sex,
                    h.MaritalStatus,
                    h.Street,
                    h.Neighborhood,
                    h.City,
                    h.ZipCode,
                    h.Phone1,
                    h.Phone2,
                    h.Email,
                    h.EducationLevel,
                    h.Specialization,
                    h.FkDepartment,
                    h.FkPosition,
                    h.ZipCodesat,
                    h.FkSupervisorId,
                    h.Createuser,
                    h.Lastuser,
                    h.Createdate,
                    h.Lastupdate,
                    h.Available
                })
                .ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "HeadCount");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("HeadCount"));
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> HeadCountUnavailable()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(hc => hc.Available == 0)
                .ToListAsync();

            var headCountAges = syHeadCounts.ToDictionary(
                hc => hc.PkHeadcount,
                hc => CalculateAge(hc.Birthdate)
            );

            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts,
                HeadCountAges = headCountAges
            };

            return View(model);
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexHeadCount()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .AsNoTracking()
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


        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexOFFBoarding()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();
            var syHeadCounts = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(h => h.Available == 0)
                .ToListAsync();

            var model = new IndexHeadCountListViewModel
            {
                Departments = departments,
                Positions = positions,
                SyHeadCount = syHeadCounts
            };

            return View(model);
        }

        private async Task<CreateHeadCountViewModel> InitializeCreateHeadCountAsync()
        {
            var departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var positions = await _context.CtPositions.AsNoTracking().ToListAsync();

            var supervisors = await (
                from s in _context.CtSupervisors.AsNoTracking()
                join h in _context.SyHeadcounts.AsNoTracking()
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
            ).ToListAsync();

            var model = new CreateHeadCountViewModel
            {
                Departments = departments,
                Position = positions,
                Supervisors = supervisors
            };

            return model;
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateHeadCount()
        {
            var model = await InitializeCreateHeadCountAsync();
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHeadCount(CreateHeadCountViewModel model)
        {
            async Task ReloadViewDataAsync()
            {
                model.Departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
                model.Position = await _context.CtPositions.AsNoTracking().ToListAsync();
                model.Supervisors = await (
                    from s in _context.CtSupervisors.AsNoTracking()
                    join h in _context.SyHeadcounts.AsNoTracking()
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
                ).ToListAsync();
            }

            async Task<IActionResult> ReturnWithErrorAsync(string errorMessage)
            {
                TempData["ErrorMessage"] = errorMessage;
                await ReloadViewDataAsync();
                return View(model);
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    await ReloadViewDataAsync();
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.LastName) && string.IsNullOrWhiteSpace(model.SecondName))
                    return await ReturnWithErrorAsync("Please provide either a Last Name or a Second Name.");

                if (await _context.SyHeadcounts.AnyAsync(h => h.ControlNumber == model.ControlNumber))
                    return await ReturnWithErrorAsync("The Control Number already exists.");

                if (await _context.AspNetUsers.AnyAsync(u => u.Ntuser == model.Ntuser))
                    return await ReturnWithErrorAsync("The UserName " + model.Ntuser + " already exists.");

                var headCount = new SyHeadcount
                {
                    ControlNumber = model.ControlNumber,
                    Photo = model.Photo ?? new byte[0],
                    Names = model.Names,
                    LastName = model.LastName ?? "",
                    SecondName = model.SecondName ?? "",
                    LevelEmployee = model.LevelEmployee ?? "",
                    ShiftWork = model.ShiftWork,
                    StarDate = model.StarDate,
                    Curp = model.Curp,
                    Rfc = model.Rfc,
                    SocialSecurity = model.SocialSecurity,
                    Birthdate = model.Birthdate,
                    Sex = model.Sex,
                    MaritalStatus = model.MaritalStatus ?? "",
                    Street = model.Street ?? "",
                    Neighborhood = model.Neighborhood ?? "",
                    City = model.City ?? "CHIHUAHUA",
                    ZipCode = model.ZipCode,
                    Phone1 = model.Phone1 ?? "",
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

                _context.SyHeadcounts.Add(headCount);
                await _context.SaveChangesAsync();

                var usuario = new AppUsuario
                {
                    UserName = model.Ntuser,
                    EmployeeNumber = model.ControlNumber.ToString(),
                    Email = model.Email,
                    Names = model.Names,
                    LastName = model.LastName ?? "",
                    Available = 1,
                    CreateDate = DateTime.Today,
                    Ntuser = model.Ntuser
                };

                var resultado = await _userManager.CreateAsync(usuario, "Temp12345!");

                if (resultado.Succeeded)
                {
                    await _userManager.AddToRoleAsync(usuario, "Empleado");

                    TempData["SuccessMessage"] = HeadCountMessages.HeadCountCreatedSuccessfully;
                    return RedirectToAction(nameof(IndexHeadCount));
                }

                TempData["ErrorMessage"] = HeadCountMessages.HeadCountAddedButUserCouldNotBe;
                return RedirectToAction(nameof(IndexHeadCount));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(HeadCountMessages.ErrorOccurredWhileCreatingTheHeadCountFormat, ex.Message);
                await ReloadViewDataAsync();
                return View(model);
            }
        }

        private async Task<EditHeadCountViewModel> BuildHeadCountViewModelAsync(SyHeadcount headCount)
        {
            return new EditHeadCountViewModel
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
                Departments = await _context.CtDepartments.AsNoTracking().ToListAsync(),
                Position = await _context.CtPositions.AsNoTracking().ToListAsync(),
                Supervisors = await (
                    from supervisor in _context.CtSupervisors.AsNoTracking()
                    join sHead in _context.SyHeadcounts.AsNoTracking()
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
                ).ToListAsync()
            };
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditHeadCount(int id)
        {
            return await ShowHeadCountViewAsync(id, nameof(EditHeadCount));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DetailHeadCount(int id)
        {
            return await ShowHeadCountViewAsync(id, nameof(DetailHeadCount));
        }

        private async Task<IActionResult> ShowHeadCountViewAsync(int id, string viewName)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                return NotFound();
            }

            var viewModel = await LoadHeadCountViewModelAsync(id);
            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewName, viewModel);
        }

        /// <summary>
        /// Loads the headcount and builds its view model, shared by EditHeadCount and DetailHeadCount.
        /// </summary>
        private async Task<EditHeadCountViewModel?> LoadHeadCountViewModelAsync(int id)
        {
            var headCount = await _context.SyHeadcounts.FindAsync(id);
            if (headCount == null)
            {
                return null;
            }

            return await BuildHeadCountViewModelAsync(headCount);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHeadCount(EditHeadCountViewModel viewModel)
        {
            async Task ReloadViewDataAsync()
            {
                viewModel.Departments = await _context.CtDepartments.AsNoTracking().ToListAsync();
                viewModel.Position = await _context.CtPositions.AsNoTracking().ToListAsync();
                viewModel.Supervisors = await (
                    from supervisor in _context.CtSupervisors.AsNoTracking()
                    join sHead in _context.SyHeadcounts.AsNoTracking()
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
                ).ToListAsync();
            }

            async Task<IActionResult> ReturnWithErrorAsync(string errorMessage)
            {
                TempData["ErrorMessage"] = errorMessage;
                await ReloadViewDataAsync();
                return View(viewModel);
            }

            try
            {
                var headCount = await _context.SyHeadcounts.FindAsync(viewModel.PkHeadcount);
                if (headCount == null)
                {
                    TempData["ErrorMessage"] = HeadCountMessages.RecordWasNotFound;
                    return RedirectToAction(nameof(IndexHeadCount));
                }

                if (!ModelState.IsValid)
                {
                    await ReloadViewDataAsync();
                    return View(viewModel);
                }

                if (string.IsNullOrWhiteSpace(viewModel.LastName) && string.IsNullOrWhiteSpace(viewModel.SecondName))
                    return await ReturnWithErrorAsync("Please provide either a Last Name or a Second Name.");

                if (await _context.SyHeadcounts.AnyAsync(h => h.ControlNumber == viewModel.ControlNumber && h.PkHeadcount != viewModel.PkHeadcount))
                    return await ReturnWithErrorAsync("The 'Control Number' already exists.");

                headCount.ControlNumber = viewModel.ControlNumber;
                headCount.Names = viewModel.Names;
                headCount.LastName = viewModel.LastName ?? "";
                headCount.SecondName = viewModel.SecondName ?? "";
                headCount.LevelEmployee = viewModel.LevelEmployee ?? "";
                headCount.ShiftWork = viewModel.ShiftWork;
                headCount.StarDate = viewModel.StarDate;
                headCount.Curp = viewModel.Curp;
                headCount.Rfc = viewModel.Rfc;
                headCount.SocialSecurity = viewModel.SocialSecurity;
                headCount.Birthdate = viewModel.Birthdate;
                headCount.Sex = viewModel.Sex;
                headCount.MaritalStatus = viewModel.MaritalStatus ?? "";
                headCount.Street = viewModel.Street ?? "";
                headCount.Neighborhood = viewModel.Neighborhood ?? "";
                headCount.City = viewModel.City ?? "CHIHUAHUA";
                headCount.ZipCode = viewModel.ZipCode;
                headCount.Phone1 = viewModel.Phone1 ?? "";
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

                TempData["SuccessMessage"] = HeadCountMessages.RecordHasBeenSuccessfullyUpdated;
                return RedirectToAction(nameof(IndexHeadCount));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(HeadCountMessages.ErrorOccurredWhileUpdatingTheHeadCountFormat, ex.Message);
                await ReloadViewDataAsync();
                return View(viewModel);
            }
        }

        [HttpPost, ActionName("DeleteHeadCount")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHeadCountConfirmed(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData["ErrorMessage"] = HeadCountMessages.InvalidHeadCountIdentifier;
                return RedirectToAction(nameof(IndexHeadCount));
            }

            var headCount = await _context.SyHeadcounts.FindAsync(id);
            if (headCount != null)
            {
                var hasDependencies = await _context.SyCoursemovements.AsNoTracking().AnyAsync(x => x.FkHeadcount == id)
                    || await _context.SyCoursecompleteds.AsNoTracking().AnyAsync(x => x.FkHeadcount == id)
                    || await _context.SyUserAnswers.AsNoTracking().AnyAsync(x => x.FkHeadcount == id)
                    || await _context.SyUserDiagnostics.AsNoTracking().AnyAsync(x => x.FkHeadcount == id)
                    || await _context.CtSupervisors.AsNoTracking().AnyAsync(x => x.FkHeadcount == id)
                    || await _context.AspNetUsers.AsNoTracking().AnyAsync(x => x.EmployeeNumber == headCount.ControlNumber.ToString());

                if (hasDependencies)
                {
                    TempData["ErrorMessage"] = "This employee cannot be deleted because identity or training history references the record. Use Offboarding to disable the employee instead.";
                    return RedirectToAction(nameof(IndexHeadCount));
                }

                try
                {
                    _context.SyHeadcounts.Remove(headCount);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = HeadCountMessages.HeadCountDeletedSuccessfully;
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "The employee became referenced by another record and was not deleted. Use Offboarding instead.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = HeadCountMessages.HeadCountNotFound;
            }
            return RedirectToAction(nameof(IndexHeadCount));
        }

        [HttpPost, ActionName("ToggleAvailabilityHeadCount")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailabilityConfirmedHeadCount(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData["ErrorMessage"] = HeadCountMessages.InvalidHeadCountIdentifier;
                return RedirectToAction(nameof(IndexHeadCount));
            }

            var headCount = await _context.SyHeadcounts.FindAsync(id);
            if (headCount == null)
            {
                TempData["ErrorMessage"] = HeadCountMessages.HeadCountNotFound;
                return RedirectToAction(nameof(IndexHeadCount));
            }

            var newAvailable = (headCount.Available == 1) ? 0 : 1;

            headCount.Available = newAvailable;
            headCount.Layoffday = (newAvailable == 0) ? DateTime.Now : (DateTime?)null;

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
                TempData["ErrorMessage"] = HeadCountMessages.ConcurrencyErrorWhileUpdatingTheHeadCount;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(HeadCountMessages.UnexpectedErrorWhileUpdatingTheHeadCountFormat, ex.Message);
            }

            return RedirectToAction(nameof(IndexHeadCount));
        }
    }
}
