using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PedidosApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        [StringLength(150, ErrorMessage = "El correo electrónico no puede tener más de 150 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio")]
        public UserRole Role { get; set; }

        [Display(Name = "Fecha de Creación")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Última Actualización")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime? UpdatedAt { get; set; }
        
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    public enum UserRole
    {
        Admin,
        Empleado,
        Cliente
    }
}