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
    }
}
