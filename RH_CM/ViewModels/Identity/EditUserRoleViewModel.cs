using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    /// <summary>
    /// Binds only the fields the "Edit User Roles" form actually submits. Binding to
    /// the full AppUsuario entity instead made ModelState invalid on every save, because
    /// its required Names/LastName columns are never part of this form.
    /// </summary>
    public class EditUserRoleViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must select a role to continue.")]
        public string IdRol { get; set; } = string.Empty;
    }
}
