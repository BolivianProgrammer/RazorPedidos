using System.Collections.Generic;
using PedidosApp.Models;

namespace PedidosApp.ViewModels
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public string SearchString { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortOrder { get; set; }

        public string NameSortParam { get; set; }
        public string PriceSortParam { get; set; }
    }
}