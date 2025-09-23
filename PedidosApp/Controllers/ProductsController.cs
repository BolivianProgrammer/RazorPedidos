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
    [Authorize(Roles = "Admin,Empleado")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        [AllowAnonymous] // Anyone can view products
        public async Task<IActionResult> Index(
            string searchString,
            decimal? minPrice,
            decimal? maxPrice,
            string sortOrder)
        {
            // Create the view model
            var viewModel = new ProductListViewModel
            {
                SearchString = searchString,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortOrder = sortOrder
            };

            // Start with all products
            var productsQuery = _context.Products.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p => 
                    p.Name.Contains(searchString) || 
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            // Apply price range filter
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            // Handle sorting
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

            // Execute the query and set the Products property
            viewModel.Products = await productsQuery.ToListAsync();

            return View(viewModel);
        }

        // GET: Products/Details/5
        [AllowAnonymous] // Anyone can view product details
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

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Stock")] Product product)
        {
            // Additional validation for positive price and non-negative stock
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,Stock")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }
            
            // Additional validation for positive price and non-negative stock
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
                    // Retrieve the original product to keep CreatedAt
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