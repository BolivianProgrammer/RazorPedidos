using System;
using System.Collections.Generic;
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
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        [AllowAnonymous] 
        public async Task<IActionResult> Index(
            string searchString,
            decimal? minPrice,
            decimal? maxPrice,
            string sortOrder)
        {
            var viewModel = new ProductListViewModel
            {
                SearchString = searchString,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder
            };

            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            viewModel.NameSortParam = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            viewModel.PriceSortParam = sortOrder == "price" ? "price_desc" : "price";

            switch (sortOrder)
            {
                case "name_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Name);
                    break;
                case "price":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
            }

            viewModel.Products = await productsQuery.ToListAsync();

            return View(viewModel);
        }

        // GET: Products/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Purchase
        [HttpPost]
        [Authorize(Roles = "Cliente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(AddToCartViewModel model)
        {
            if (model.Quantity <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor que cero.";
                return RedirectToAction("Details", new { id = model.ProductId });
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            if (product.Stock < model.Quantity)
            {
                TempData["Error"] = "No hay suficiente stock disponible.";
                return RedirectToAction("Details", new { id = model.ProductId });
            }

            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);
            var user = await _context.Users.FindAsync(currentUserId);
            if (user == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            decimal subtotal = product.Price * model.Quantity;

            var order = new Order
            {
                UserId = currentUserId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                Total = subtotal
            };

            var orderItem = new OrderItem
            {
                ProductId = model.ProductId,
                Quantity = model.Quantity,
                Subtotal = subtotal
            };

            order.OrderItems = new List<OrderItem> { orderItem };

            product.Stock -= model.Quantity;
            product.UpdatedAt = DateTime.Now;

            _context.Orders.Add(order);
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Pedido realizado exitosamente. Total: {subtotal:C2}";
            return RedirectToAction("Index", "Home");
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin,Empleado")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock")] Product product)
        {
            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "El precio debe ser mayor que 0");
            }

            if (product.Stock < 0)
            {
                ModelState.AddModelError("Stock", "El stock no puede ser negativo");
            }

            if (ModelState.IsValid)
            {
                product.CreatedAt = DateTime.Now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError("Price", "El precio debe ser mayor que 0");
            }

            if (product.Stock < 0)
            {
                ModelState.AddModelError("Stock", "El stock no puede ser negativo");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (originalProduct != null)
                    {
                        product.CreatedAt = originalProduct.CreatedAt;
                    }

                    product.UpdatedAt = DateTime.Now;
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Producto actualizado exitosamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Empleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto eliminado exitosamente";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}