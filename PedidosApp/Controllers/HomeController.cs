using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PedidosApp.Data;
using PedidosApp.Models;

namespace PedidosApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["WelcomeMessage"] = "Bienvenido al Sistema de Gestión de Pedidos";

            if (User.Identity.IsAuthenticated && User.IsInRole("Cliente"))
            {
                int.TryParse(User.FindFirst("UserId")?.Value, out int userId);
                
                if (userId > 0)
                {
                    var recentOrders = await _context.Orders
                        .Where(o => o.UserId == userId)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5)
                        .ToListAsync();
                    
                    ViewBag.RecentOrders = recentOrders;
                    
                    var newProducts = await _context.Products
                        .Where(p => p.Stock > 0)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(3)
                        .ToListAsync();
                    
                    ViewBag.NewProducts = newProducts;
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
