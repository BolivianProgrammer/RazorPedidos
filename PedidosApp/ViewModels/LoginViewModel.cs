using System.ComponentModel.DataAnnotations;

namespace PedidosApp.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo electr�nico es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo electr�nico no es v�lido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrase�a es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}