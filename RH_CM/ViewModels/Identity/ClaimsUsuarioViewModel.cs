namespace RH_CM.ViewModels
{
    public class ClaimsUsuarioViewModel
    {
        public ClaimsUsuarioViewModel()
        {
            Claims = new List<ClaimUsuario>();
        }


        public string IdUsuario { get; set; } = string.Empty;
        public List<ClaimUsuario> Claims { get; set; }



        public class ClaimUsuario
        {
            public string TipoClaim { get; set; } = string.Empty;
            public bool Seleccionado { get; set; }
        }
    }
}
