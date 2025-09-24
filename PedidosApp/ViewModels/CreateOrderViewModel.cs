using System.Collections.Generic;
using PedidosApp.Models;

namespace PedidosApp.ViewModels
{
    public class CreateOrderViewModel
    {
        public Order Order { get; set; }
        public List<User> Clients { get; set; } = new List<User>();
        public List<Product> Products { get; set; } = new List<Product>();
        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public List<int> Quantities { get; set; } = new List<int>();
    }
}