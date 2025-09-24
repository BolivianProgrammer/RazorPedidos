using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PedidosApp.Data;
using PedidosApp.Models;
using PedidosApp.ViewModels;

namespace PedidosApp.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            
            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateOrderViewModel
            {
                Order = new Order { 
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending 
                },
                Products = await _context.Products.Where(p => p.Stock > 0).ToListAsync(),
                Clients = await _context.Users.Where(u => u.Role == UserRole.Cliente).ToListAsync()
            };

            ViewData["UserSelectList"] = new SelectList(viewModel.Clients, "Id", "Name");
                
            return View(viewModel);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel viewModel)
        {
            if (viewModel.SelectedProductIds == null || !viewModel.SelectedProductIds.Any() || viewModel.Quantities == null)
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un producto con cantidad mayor a cero.");
                
                viewModel.Products = await _context.Products.Where(p => p.Stock > 0).ToListAsync();
                viewModel.Clients = await _context.Users.Where(u => u.Role == UserRole.Cliente).ToListAsync();
                
                ViewData["UserSelectList"] = new SelectList(viewModel.Clients, "Id", "Name", viewModel.Order.UserId);
                
                return View(viewModel);
            }

            var order = new Order
            {
                UserId = viewModel.Order.UserId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending, 
                Total = 0 
            };

            decimal total = 0;
            var orderItems = new List<OrderItem>();
            
            for (int i = 0; i < viewModel.SelectedProductIds.Count; i++)
            {
                int productId = viewModel.SelectedProductIds[i];
                int quantity = viewModel.Quantities[i];
                
                if (quantity <= 0)
                    continue;
                
                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.Stock < quantity)
                {
                    ModelState.AddModelError("", $"Producto {product?.Name ?? "desconocido"} no tiene suficiente stock.");
                    continue;
                }
                
                decimal subtotal = product.Price * quantity;
                total += subtotal;
                
                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Subtotal = subtotal
                };
                
                orderItems.Add(orderItem);
                
                product.Stock -= quantity;
                _context.Update(product);
            }
            
            if (!orderItems.Any())
            {
                ModelState.AddModelError("", "No se pudo crear ningún detalle del pedido. Verifique las cantidades y el stock disponible.");
                
                viewModel.Products = await _context.Products.Where(p => p.Stock > 0).ToListAsync();
                viewModel.Clients = await _context.Users.Where(u => u.Role == UserRole.Cliente).ToListAsync();
                
                ViewData["UserSelectList"] = new SelectList(viewModel.Clients, "Id", "Name", viewModel.Order.UserId);
                
                return View(viewModel);
            }

            order.Total = total;
            order.OrderItems = orderItems;
            
            _context.Add(order);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Pedido creado exitosamente con estado Pendiente";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }
            
            ViewData["StatusSelectList"] = new SelectList(Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
                .Select(s => new
                {
                    Id = (int)s,
                    Name = GetEnumDisplayName(s)
                }), "Id", "Name", (int)order.Status);
            
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Status")] Order orderUpdate)
        {
            if (id != orderUpdate.Id)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = orderUpdate.Status;
            
            try
            {
                _context.Update(order);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Estado del pedido actualizado exitosamente";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing)
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        _context.Update(product);
                    }
                }
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Pedido eliminado exitosamente";
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
        
        private string GetEnumDisplayName(OrderStatus status)
        {
            var memberInfo = status.GetType().GetMember(status.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            
            return attributes.Length > 0
                ? ((DisplayAttribute)attributes[0]).Name
                : status.ToString();
        }
    }
}