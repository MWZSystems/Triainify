using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public class CatalogController : Controller
    {
        private readonly RH_CHDBContext _context;

        public CatalogController(RH_CHDBContext context)
        {
            _context = context;
        }
        // GET: CtDepartment
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexDepartment()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            return View(departments);
        }
        // GET: CtDepartment/Create
        public IActionResult CreateDepartment()
        {
            return View();
        }

        // POST: CtDepartment/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(CtDepartment ctDepartment)
        {
            // Verificar si el nombre del departamento ya existe
            bool departmentExists = await _context.CtDepartments
                .AnyAsync(d => d.NameDeparment == ctDepartment.NameDeparment);

            if (departmentExists)
            {
                // Añadir un mensaje de error al TempData
                TempData["ErrorMessage"] = "The department name already exists. Please choose a different name.";

                // Volver a cargar la vista con el modelo actual
                return View(ctDepartment);
            }

            // Asignar valores automáticos
            ctDepartment.Createuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctDepartment.Createdate = DateTime.Now;
            ctDepartment.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctDepartment.Lastupdatedate = DateTime.Now;
            ctDepartment.Available = 1; // Valor predeterminado: habilitado

            try
            {
                _context.Add(ctDepartment);
                await _context.SaveChangesAsync();

                // Añadir un mensaje de éxito al TempData
                TempData["SuccessMessage"] = "Department created successfully.";

                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the department: {ex.Message}";
                return View(ctDepartment);
            }
        }


        // GET: CtDepartment/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditDepartment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctDepartment = await _context.CtDepartments.FindAsync(id);
            if (ctDepartment == null)
            {
                return NotFound();
            }
            return View(ctDepartment);
        }

        // POST: CtDepartment/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(int id, CtDepartment ctDepartment)
        {
            if (id != ctDepartment.PkDepartment)
            {
                TempData["ErrorMessage"] = "The specified department was not found.";
                return NotFound();
            }

            // Verificar si el nombre del departamento ya existe
            bool departmentExists = await _context.CtDepartments
                .AnyAsync(d => d.NameDeparment == ctDepartment.NameDeparment && d.PkDepartment != ctDepartment.PkDepartment);

            if (departmentExists)
            {
                TempData["ErrorMessage"] = "The department name already exists. Please choose a different name.";
                return View(ctDepartment);
            }

            try
            {
                // Recuperar los datos originales para evitar sobrescrituras accidentales
                var existingDepartment = await _context.CtDepartments.FindAsync(id);
                if (existingDepartment == null)
                {
                    TempData["ErrorMessage"] = "The department no longer exists in the database.";
                    return NotFound();
                }

                // Actualizar solo los campos permitidos
                existingDepartment.NameDeparment = ctDepartment.NameDeparment;
                existingDepartment.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Usuario actual
                existingDepartment.Lastupdatedate = DateTime.Now; // Fecha de actualización

                _context.Update(existingDepartment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department updated successfully.";
                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "There was a concurrency error while updating the department. Please try again.";
                return View(ctDepartment);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the department: {ex.Message}";
                return View(ctDepartment);
            }
        }


        // POST: /Catalog/ToggleDepartment/5
        [HttpPost]
        [Route("ToggleDepartment")]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDepartment(int id)
        {
            var ctDepartment = await _context.CtDepartments.FindAsync(id);
            if (ctDepartment != null)
            {
                // Alternar el estado de disponibilidad
                ctDepartment.Available = ctDepartment.Available == 1 ? 0 : 1;
                _context.Update(ctDepartment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexDepartment));
        }

        // POST: /Catalog/DeleteDepartment/5
        [HttpPost]
        [Route("DeleteDepartment")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var ctDepartment = await _context.CtDepartments.FindAsync(id);
            if (ctDepartment != null)
            {
                _context.CtDepartments.Remove(ctDepartment);  // Eliminar el departamento
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexDepartment));
        }

        private bool CtDepartmentExists(int id)
        {
            return _context.CtDepartments.Any(e => e.PkDepartment == id);
        }

        // GET: CtPosition
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexPosition()
        {
            var positions = await _context.CtPositions.ToListAsync();
            return View(positions);
        }

        // GET: CtPosition/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreatePosition()
        {
            return View();
        }

        // POST: CtPosition/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePosition(CtPosition ctPosition)
        {
            // Validar si el nombre de la posición ya existe (español)
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition);

            // Validar si el nombre de la posición en inglés ya existe
            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish);

            if (positionExists || englishPositionExists)
            {
                // Añadir un mensaje de error al TempData
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                // Volver a cargar la vista con el modelo actual
                return View(ctPosition);
            }

            // Asignar valores automáticos
            ctPosition.Createuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctPosition.Createdate = DateTime.Now;
            ctPosition.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctPosition.Lastupdatedate = DateTime.Now;
            ctPosition.Available = 1; // Valor predeterminado: habilitado

            try
            {
                _context.Add(ctPosition);
                await _context.SaveChangesAsync();

                // Añadir un mensaje de éxito al TempData
                TempData["SuccessMessage"] = "Position created successfully.";

                return RedirectToAction(nameof(IndexPosition));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the position: {ex.Message}";
                return View(ctPosition);
            }
        }

        // GET: CtPosition/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditPosition(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition == null)
            {
                return NotFound();
            }

            return View(ctPosition);
        }

        // POST: CtPosition/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPosition(int id, CtPosition ctPosition)
        {
            if (id != ctPosition.PkPosition)
            {
                TempData["ErrorMessage"] = "The specified position was not found.";
                return RedirectToAction(nameof(IndexPosition));
            }

            // Verificar si el nombre de la posición ya existe
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition && p.PkPosition != ctPosition.PkPosition);

            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish && p.PkPosition != ctPosition.PkPosition);

            if (positionExists || englishPositionExists)
            {
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                return View(ctPosition);
            }

            try
            {
                // Recuperar los datos originales para evitar sobrescrituras accidentales
                var existingPosition = await _context.CtPositions.FindAsync(id);
                if (existingPosition == null)
                {
                    TempData["ErrorMessage"] = "The position no longer exists in the database.";
                    return NotFound();
                }

                // Actualizar solo los campos que pueden ser modificados
                existingPosition.NamePosition = ctPosition.NamePosition;
                existingPosition.NamePositionEnglish = ctPosition.NamePositionEnglish;
                existingPosition.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Usuario actual
                existingPosition.Lastupdatedate = DateTime.Now; // Fecha de actualización

                _context.Update(existingPosition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Position updated successfully.";
                return RedirectToAction(nameof(IndexPosition));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "There was a concurrency error while updating the position. Please try again.";
                return View(ctPosition);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the position: {ex.Message}";
                return View(ctPosition);
            }
        }

        // POST: /Catalog/TogglePosition/5
        [HttpPost]
        [Route("TogglePosition")]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition != null)
            {
                // Alternar el estado de disponibilidad
                ctPosition.Available = ctPosition.Available == 1 ? 0 : 1;

                _context.Update(ctPosition);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexPosition));
        }

        // POST: /Catalog/DeletePosition/5
        [HttpPost]
        [Route("DeletePosition")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedPosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition != null)
            {
                _context.CtPositions.Remove(ctPosition); // Eliminar la posición de la base de datos
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexPosition));
        }
        private bool CtPositionExists(int id)
        {
            return _context.CtPositions.Any(e => e.PkPosition == id);
        }

        // GET: CtCourse
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexCourse()
        {
            var courses = await _context.CtCourses.ToListAsync();
            return View(courses);
        }

        // GET: CtCourse/Create
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CtCourse ctCourse)
        {
            // Validar campos requeridos manualmente
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = "The Management System field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = "The Course ID field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = "The Course Name field is required.";
                return View(ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = "The Revision must be greater than 0.";
                return View(ctCourse);
            }

            // Verificar si el Course ID ya existe
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse);

            // Verificar si el Course Name ya existe
            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = "The Course ID and Course Name already exist. Please choose different values.";
                return View(ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = "The Course ID already exists. Please choose a different ID.";
                return View(ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = "The Course Name already exists. Please choose a different name.";
                return View(ctCourse);
            }

            // Asignar valores automáticos
            ctCourse.CreateUser = User.Identity.Name ?? "Unknown";
            ctCourse.CreateDate = DateTime.Now;
            ctCourse.LastUpdateUser = User.Identity.Name ?? "Unknown";
            ctCourse.LastUpdateDate = DateTime.Now;
            ctCourse.Available = 1;

            try
            {
                _context.Add(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course created successfully.";
                return RedirectToAction(nameof(IndexCourse));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the course: {ex.Message}";
                return View(ctCourse);
            }
        }

        // GET: CtCourse/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse == null)
            {
                return NotFound();
            }
            return View(ctCourse);
        }

        // POST: CtCourse/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CtCourse ctCourse)
        {
            if (id != ctCourse.PkCourse)
            {
                TempData["ErrorMessage"] = "The specified course was not found.";
                return RedirectToAction(nameof(IndexCourse));
            }


            // Validar campos requeridos manualmente
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = "The Management System field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = "The Course ID field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = "The Course Name field is required.";
                return View(ctCourse);
            }

            if (ctCourse.CourseValidityDays <= 0)
            {
                TempData["ErrorMessage"] = "The Course Validity Days must be greater than 0.";
                return View(ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = "The Revision must be greater than 0.";
                return View(ctCourse);
            }

            // Verificar duplicados en la base de datos (excluyendo el curso actual)
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse && c.PkCourse != ctCourse.PkCourse);

            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName && c.PkCourse != ctCourse.PkCourse);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = "The Course ID and Course Name already exist. Please choose different values.";
                return View(ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = "The Course ID already exists. Please choose a different ID.";
                return View(ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = "The Course Name already exists. Please choose a different name.";
                return View(ctCourse);
            }

            try
            {
                var existingCourse = await _context.CtCourses.FindAsync(id);
                if (existingCourse == null)
                {
                    TempData["ErrorMessage"] = "The course no longer exists in the database.";
                    return NotFound();
                }

                // Actualizar campos permitidos
                existingCourse.ManagementSystem = ctCourse.ManagementSystem;
                existingCourse.Idcourse = ctCourse.Idcourse;
                existingCourse.CourseName = ctCourse.CourseName;
                existingCourse.CourseValidityDays = ctCourse.CourseValidityDays;
                existingCourse.Revision = ctCourse.Revision;
                existingCourse.LastUpdateUser = User.Identity.Name ?? "Unknown";
                existingCourse.LastUpdateDate = DateTime.Now;

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course updated successfully.";
                return RedirectToAction(nameof(EditCourse));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "There was a concurrency error while updating the course.";
                return View(ctCourse);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the course: {ex.Message}";
                return View(ctCourse);
            }
        }


        // POST: /CtCourse/ToggleCourse/5
        [HttpPost]
        [Route("ToggleCourse")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourse(int id)
        {
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse != null)
            {
                ctCourse.Available = ctCourse.Available == 1 ? 0 : 1;
                _context.Update(ctCourse);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexCourse));
        }

        // POST: /CtCourse/DeleteCourse/5
        [HttpPost]
        [Route("DeleteCourse")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse != null)
            {
                _context.CtCourses.Remove(ctCourse);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexCourse));
        }

        private bool CtCourseExists(int id)
        {
            return _context.CtCourses.Any(e => e.PkCourse == id);
        }

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseassignments()
        {
            var courseAssignments = _context.CtCourseassignments
                .Join(_context.CtPositions,
                      ca => ca.FkPosition,
                      p => p.PkPosition,
                      (ca, p) => new { ca, p })
                .Join(_context.CtCourses,
                      temp => temp.ca.FkCourse,
                      c => c.PkCourse,
                      (temp, c) => new
                      {
                          temp.ca.PkCourseAssignment,
                          PositionName = temp.p.NamePosition,
                          CourseName = c.CourseName,
                          temp.ca.FkRequiredCourseLevels,
                          temp.ca.Requiered, // bool
                          temp.ca.Available
                      })
                .ToList()
                .Select(item => new
                {
                    item.PkCourseAssignment,
                    item.PositionName,
                    item.CourseName,
                    RequiredCourseLevelDescription = GetCourseLevelDescription(item.FkRequiredCourseLevels),
                    item.Requiered, // bool
                    item.Available
                });

            return View(courseAssignments);
        }

        private static string GetCourseLevelDescription(int level)
        {
            return level switch
            {
                1 => "Introducción",
                2 => "Básico",
                3 => "Intermedio",
                4 => "Avanzado",
                _ => "Nivel desconocido",
            };
        }

        // GET: CourseAssignments/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseAssignment()
        {
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();
            ViewBag.Courses = _context.CtCourses.Where(c => c.Available == 1).ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignment(CtCourseassignment courseAssignment)
        {
            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels);

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Guardar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;
            courseAssignment.Available = 1;
            _context.CtCourseassignments.Add(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment created successfully.";
            return RedirectToAction(nameof(CreateCourseAssignment));
        }


        // GET: CourseAssignments/Edit/5
        [Authorize(Roles = "Administrador")]
        public IActionResult EditCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment == null) return NotFound();

            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();
            ViewBag.Courses = _context.CtCourses.Where(c => c.Available == 1).ToList();
            return View(courseAssignment);
        }

        // POST: CourseAssignments/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseAssignment(int id, CtCourseassignment courseAssignment)
        {
            if (id != courseAssignment.PkCourseAssignment) return NotFound();

            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels &&
                ca.PkCourseAssignment != id); // Excluir el registro actual

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Actualizar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;

            _context.CtCourseassignments.Update(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment updated successfully.";
            return RedirectToAction(nameof(EditCourseAssignment));
        }


        // POST: CourseAssignments/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment != null)
            {
                _context.CtCourseassignments.Remove(courseAssignment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Course assignment deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        // POST: /CtCourseassignment/ToggleCourseAssignment/5
        [HttpPost]
        [Route("ToggleCourseAssignment")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseAssignment(int id)
        {
            var courseAssignment = await _context.CtCourseassignments.FindAsync(id);
            if (courseAssignment != null)
            {
                // Alternar el valor de Available
                courseAssignment.Available = courseAssignment.Available == 1 ? 0 : 1;
                courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
                courseAssignment.LastUpdateDate = DateTime.Now;

                _context.CtCourseassignments.Update(courseAssignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The course assignment availability status was successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexSupervisor()
        {
            var supervisors = _context.CtSupervisors
                //.Where(s => s.Available == 1)
                .Join(_context.SyHeadCounts.Where(h => h.Available == 1),
                    s => s.FkHeadcount,
                    h => h.PkHeadcount,
                    (s, h) => new { s, h })
                .Join(_context.CtDepartments.Where(d => d.Available == 1),
                    temp => temp.s.FkDepartment,
                    d => d.PkDepartment,
                    (temp, d) => new { temp.s, temp.h, d })
                .Join(_context.CtPositions.Where(p => p.Available == 1),
                    temp => temp.s.FkPosition,
                    p => p.PkPosition,
                    (temp, p) => new
                    {
                        temp.s.PkSupervisorId,
                        temp.h.ControlNumber,
                        FullName = $"{temp.h.Names} {(temp.h.LastName ?? "")} {(temp.h.SecondName ?? "")}".Trim(),
                        temp.s.Available,
                        DepartmentName = temp.d.NameDeparment,
                        PositionName = p.NamePosition
                    })
                .ToList();

            return View(supervisors);
        }

        // GET: CtSupervisor/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreateSupervisor()
        {
            ViewBag.Headcount = _context.SyHeadCounts.Where(d => d.Available == 1).ToList();
            ViewBag.Departments = _context.CtDepartments.Where(d => d.Available == 1).ToList();
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();

            return View();
        }


        // POST: CtSupervisor/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupervisor(CtSupervisor ctSupervisor)
        {
            // Validar combinación única FkHeadcount + FkDepartment
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s => s.FkHeadcount == ctSupervisor.FkHeadcount && s.FkDepartment == ctSupervisor.FkDepartment && ctSupervisor.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] = "A supervisor with the same Headcount and Department already exists.";
                // Redirigir para mostrar el mensaje en la vista
                return RedirectToAction(nameof(CreateSupervisor));
            }

            // Asignar datos de auditoría
            ctSupervisor.Createuser = User.Identity?.Name ?? "Unknown";
            ctSupervisor.Createdate = DateTime.Now;
            ctSupervisor.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            ctSupervisor.Lastupdatedate = DateTime.Now;
            ctSupervisor.Available = 1;

            try
            {
                _context.Add(ctSupervisor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Supervisor created successfully.";
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating supervisor: {ex.Message}";
                return RedirectToAction(nameof(CreateSupervisor));
            }
        }

        // GET: CtSupervisor/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditSupervisor(int? id)
        {
            if (id == null)
                return NotFound();

            var ctSupervisor = await _context.CtSupervisors.FindAsync(id);
            if (ctSupervisor == null)
                return NotFound();

            // Cargar los dropdowns
            ViewBag.Headcount = _context.SyHeadCounts.Where(h => h.Available == 1).ToList();
            ViewBag.Departments = _context.CtDepartments.Where(d => d.Available == 1).ToList();
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();

            return View(ctSupervisor);
        }

        // POST: CtSupervisor/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupervisor(int id, CtSupervisor ctSupervisor)
        {
            if (id != ctSupervisor.PkSupervisorId)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // Validar combinación única FkHeadcount + FkDepartment (excluyendo el mismo registro)
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s => s.FkHeadcount == ctSupervisor.FkHeadcount
                            && s.FkDepartment == ctSupervisor.FkDepartment
                            && s.PkSupervisorId != id
                            && s.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] = "Another supervisor with the same Headcount and Department already exists.";
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }

            try
            {
                var existing = await _context.CtSupervisors.FindAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Supervisor no longer exists.";
                    return RedirectToAction(nameof(IndexSupervisor));
                }

                existing.FkHeadcount = ctSupervisor.FkHeadcount;
                existing.FkDepartment = ctSupervisor.FkDepartment;
                existing.FkPosition = ctSupervisor.FkPosition;
                existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                existing.Lastupdatedate = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Supervisor updated successfully.";
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating supervisor: {ex.Message}";
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }
        }


        // POST: /Catalog/ToggleSupervisor/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // Si se va a habilitar, validar que no exista combinación duplicada
            if (supervisor.Available == 0)
            {
                bool combinationExists = await _context.CtSupervisors
                    .AnyAsync(s =>
                        s.FkHeadcount == supervisor.FkHeadcount &&
                        s.FkDepartment == supervisor.FkDepartment &&
                        s.PkSupervisorId != id &&
                        s.Available == 1);

                if (combinationExists)
                {
                    TempData["ErrorMessage"] = "Cannot enable this supervisor because another active supervisor with the same Headcount and Department already exists.";
                    return RedirectToAction(nameof(IndexSupervisor));
                }
            }

            // Alternar el estado
            supervisor.Available = supervisor.Available == 1 ? 0 : 1;
            supervisor.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            supervisor.Lastupdatedate = DateTime.Now;

            _context.Update(supervisor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Supervisor status updated successfully.";
            return RedirectToAction(nameof(IndexSupervisor));
        }


        // POST: /Catalog/DeleteSupervisor/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor != null)
            {
                _context.CtSupervisors.Remove(supervisor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexSupervisor));
        }

        private bool CtSupervisorExists(int id)
        {
            return _context.CtSupervisors.Any(e => e.PkSupervisorId == id);
        }

        // GET: CtCoursematerial
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseMaterial()
        {
            var materials = _context.CtCoursematerials
                .Where(m => m.Available == 1)
                .Join(
                    _context.CtCourses.Where(c => c.Available == 1),
                    m => m.FkCourse,
                    c => c.PkCourse,
                    (m, c) => new { m, c }
                )
                .Select(temp => new
                {
                    temp.m.PkCoursematerial,
                    MaterialName = temp.m.NameMaterial,  // NOMBRE CONSISTENTE
                    temp.m.Available,
                    CourseName = temp.c.CourseName,
                    temp.c.ManagementSystem,
                    //temp.c.Idcourse,
                    //temp.c.Revision,
                    //temp.c.CourseValidityDays
                })
                .ToList();

            return View(materials);
        }


        // GET: CtCoursematerial/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateCourseMaterial()
        {
            // Cargar cursos activos
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View();
        }
        // POST: CtCoursematerial/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseMaterial(CtCoursematerial model, IFormFile uploadedFile)
        {
            // Validar archivo obligatorio
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "You must upload a PDF file.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Validar Level no negativo
            if (model.Level.HasValue && model.Level < 0)
            {
                TempData["ErrorMessage"] = "Level cannot be negative.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Obtener el FkCourse desde el radio button
            var selectedFkCourse = Request.Form["FkCourse"].FirstOrDefault();
            if (string.IsNullOrEmpty(selectedFkCourse))
            {
                TempData["ErrorMessage"] = "You must select a course.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            if (!int.TryParse(selectedFkCourse, out var fkCourseValue))
            {
                TempData["ErrorMessage"] = "Invalid course selection.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            using var ms = new MemoryStream();
            await uploadedFile.CopyToAsync(ms);
            model.File = ms.ToArray();

            model.FkCourse = fkCourseValue;
            model.Createuser = User.Identity?.Name ?? "Unknown";
            model.Createdate = DateTime.Now;
            model.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            model.Lastupdatedate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material created successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        // GET: CtCoursematerial/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditCourseMaterial(int? id)
        {
            if (id == null)
                return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            // Cargar cursos activos para la tabla
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(material);
        }

        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseMaterial(int id, CtCoursematerial model, IFormFile? uploadedFile)
        {
            if (id != model.PkCoursematerial)
                return NotFound();

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Obtener el FkCourse desde el radio button
            var selectedFkCourse = Request.Form["FkCourse"].FirstOrDefault();

            if (string.IsNullOrEmpty(selectedFkCourse))
            {
                TempData["ErrorMessage"] = "You must select a course.";

                // Recargar cursos y devolver vista
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            if (!int.TryParse(selectedFkCourse, out var fkCourseValue))
            {
                TempData["ErrorMessage"] = "Invalid course selection.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Actualizar campos
            existing.NameMaterial = model.NameMaterial;
            existing.FkCourse = fkCourseValue;
            existing.Level = model.Level;
            existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            existing.Lastupdatedate = DateTime.Now;

            // Si subió nuevo archivo
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);
                existing.File = ms.ToArray();
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        // POST: CtCoursematerial/Toggle/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            material.Available = material.Available == 1 ? 0 : 1;
            material.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            material.Lastupdatedate = DateTime.Now;

            _context.Update(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material status updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // POST: CtCoursematerial/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material != null)
            {
                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // GET: CtCoursematerial/Download/5
        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf", $"{material.NameMaterial}.pdf");
        }

        // GET: CtCoursematerial/ViewPdf/5
        public async Task<IActionResult> ViewPdfCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf");
        }

        // GET: CtTest/IndexTest
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> IndexTest()
        {
            var tests = await _context.CtTests
                .OrderByDescending(t => t.Createdate)
                .ToListAsync();

            return View(tests);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Obtener todas las preguntas del test
                var questions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                foreach (var question in questions)
                {
                    // 2. Eliminar respuestas correctas
                    var correctAnswers = await _context.CtCorrectanswers
                        .Where(ca => ca.FkQuestions == question.PkQuestions)
                        .ToListAsync();
                    _context.CtCorrectanswers.RemoveRange(correctAnswers);

                    // 3. Eliminar opciones
                    var options = await _context.CtOptions
                        .Where(o => o.FkQuestions == question.PkQuestions)
                        .ToListAsync();
                    _context.CtOptions.RemoveRange(options);
                }

                // 4. Eliminar preguntas
                _context.CtQuestions.RemoveRange(questions);

                // 5. Eliminar el test
                var test = await _context.CtTests.FindAsync(id);
                if (test != null)
                {
                    _context.CtTests.Remove(test);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test and all related data were deleted successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting test: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexTest));
        }

        // GET: CtTest/EditTest/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditTest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctTest = await _context.CtTests.FindAsync(id);
            if (ctTest == null)
            {
                return NotFound();
            }

            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(ctTest);
        }

        // GET: Catalog/CreateQuestions
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateQuestions()
        {
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var vm = new TestCreateViewModel
            {
                Questions = new List<QuestionCreateViewModel>
                {
                    new QuestionCreateViewModel
                    {
                        Options = new List<OptionCreateViewModel>
                        {
                            new OptionCreateViewModel(),
                            new OptionCreateViewModel()
                        }
                    }
                }
            };

            return View(vm);
        }

        // POST: Catalog/CreateQuestions
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestions(TestCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TestName))
            {
                TempData["ErrorMessage"] = "The Test Name field is required.";
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                return View(model);
            }

            if (model.CourseLevel < 0)
            {
                TempData["ErrorMessage"] = "The Course Level cannot be negative.";
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Insert Test
                var test = new CtTest
                {
                    FkCourse = model.FkCourse,
                    CourseLevel = model.CourseLevel,
                    TestName = model.TestName,
                    Createuser = User.Identity?.Name ?? "Unknown",
                    Createdate = DateTime.Now,
                    Lastupdateuser = User.Identity?.Name ?? "Unknown",
                    Lastupatedate = DateTime.Now,
                    Available = 1
                };

                _context.CtTests.Add(test);
                await _context.SaveChangesAsync();

                // Insert Questions and Options
                foreach (var q in model.Questions)
                {
                    var question = new CtQuestion
                    {
                        FkTest = test.PkTest,
                        Question = q.QuestionText,
                        Createuser = test.Createuser,
                        Createdate = DateTime.Now,
                        Lastupdateuser = test.Createuser,
                        Lastupatedate = DateTime.Now,
                        Available = 1
                    };

                    _context.CtQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    foreach (var optionVm in q.Options)
                    {
                        var option = new CtOption
                        {
                            FkQuestions = question.PkQuestions,
                            Options = optionVm.OptionText,
                            Answer = optionVm.IsCorrect ? 1 : 0,
                            Createuser = test.Createuser,
                            Createdate = DateTime.Now,
                            Lastupdateuser = test.Createuser,
                            Lastupatedate = DateTime.Now,
                            Available = 1
                        };

                        _context.CtOptions.Add(option);
                        await _context.SaveChangesAsync();

                        if (optionVm.IsCorrect)
                        {
                            var correctAnswer = new CtCorrectanswer
                            {
                                FkQuestions = question.PkQuestions,
                                FkOptions = option.PkOptions.ToString(),
                                Createuser = test.Createuser,
                                Createdate = DateTime.Now,
                                Lastupdateuser = test.Createuser,
                                Lastupatedate = DateTime.Now,
                                Available = 1
                            };

                            _context.CtCorrectanswers.Add(correctAnswer);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test, questions, and options created successfully.";
                return RedirectToAction(nameof(IndexTest), "Catalog");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error saving data: {ex.Message}";
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                return View(model);
            }
        }

        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditQuestions(int id)
        {
            // Obtiene el test
            var test = await _context.CtTests
                .FirstOrDefaultAsync(t => t.PkTest == id);

            if (test == null)
            {
                return NotFound();
            }

            // Obtiene preguntas y opciones
            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == id)
                .Select(q => new QuestionCreateViewModel
                {
                    QuestionText = q.Question,
                    Options = _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .Select(o => new OptionCreateViewModel
                        {
                            OptionText = o.Options,
                            IsCorrect = o.Answer == 1
                        })
                        .ToList()
                })
                .ToListAsync();

            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var vm = new TestCreateViewModel
            {
                FkCourse = test.FkCourse,
                CourseLevel = test.CourseLevel,
                TestName = test.TestName,
                Questions = questions
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar datos del test
                var test = await _context.CtTests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                test.TestName = model.TestName;
                test.FkCourse = model.FkCourse;
                test.CourseLevel = model.CourseLevel;
                test.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                test.Lastupatedate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Eliminar preguntas, opciones y respuestas previas
                var oldQuestions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                foreach (var q in oldQuestions)
                {
                    var correctAnswers = await _context.CtCorrectanswers
                        .Where(c => c.FkQuestions == q.PkQuestions)
                        .ToListAsync();
                    _context.CtCorrectanswers.RemoveRange(correctAnswers);

                    var options = await _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .ToListAsync();
                    _context.CtOptions.RemoveRange(options);
                }

                _context.CtQuestions.RemoveRange(oldQuestions);
                await _context.SaveChangesAsync();

                // Insertar nuevas preguntas y opciones
                foreach (var q in model.Questions)
                {
                    var question = new CtQuestion
                    {
                        FkTest = test.PkTest,
                        Question = q.QuestionText,
                        Createuser = test.Createuser,
                        Createdate = DateTime.Now,
                        Lastupdateuser = test.Lastupdateuser,
                        Lastupatedate = DateTime.Now,
                        Available = 1
                    };

                    _context.CtQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    foreach (var optionVm in q.Options)
                    {
                        var option = new CtOption
                        {
                            FkQuestions = question.PkQuestions,
                            Options = optionVm.OptionText,
                            Answer = optionVm.IsCorrect ? 1 : 0,
                            Createuser = test.Createuser,
                            Createdate = DateTime.Now,
                            Lastupdateuser = test.Lastupdateuser,
                            Lastupatedate = DateTime.Now,
                            Available = 1
                        };

                        _context.CtOptions.Add(option);
                        await _context.SaveChangesAsync();

                        if (optionVm.IsCorrect)
                        {
                            var correctAnswer = new CtCorrectanswer
                            {
                                FkQuestions = question.PkQuestions,
                                FkOptions = option.PkOptions.ToString(),
                                Createuser = test.Createuser,
                                Createdate = DateTime.Now,
                                Lastupdateuser = test.Lastupdateuser,
                                Lastupatedate = DateTime.Now,
                                Available = 1
                            };

                            _context.CtCorrectanswers.Add(correctAnswer);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test questions updated successfully.";
                return RedirectToAction(nameof(IndexTest));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error updating questions: {ex.Message}";
                return View(model);
            }
        }

    }
}
