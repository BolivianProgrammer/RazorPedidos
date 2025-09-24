using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PedidosApp.Models
{
    public enum OrderStatus
    {
        [Display(Name = "Pendiente")]
        Pending,
        
        [Display(Name = "En Proceso")]
        Processing,
        
        [Display(Name = "Completado")]
        Completed,
        
        [Display(Name = "Cancelado")]
        Cancelled
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El cliente es obligatorio")]
        [Display(Name = "Cliente")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = false)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [Display(Name = "Estado")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required(ErrorMessage = "El total es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El total debe ser mayor que 0")]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Total")]
        [DisplayFormat(DataFormatString = "{0:C2}", ApplyFormatInEditMode = false)]
        public decimal Total { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}