using Microsoft.EntityFrameworkCore;
using PedidosApp.Models;

namespace PedidosApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define your DbSet properties for your entities here
        // For example:
        // public DbSet<Pedido> Pedidos { get; set; }
        // public DbSet<Cliente> Clientes { get; set; }
    }
}