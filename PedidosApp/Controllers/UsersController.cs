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

namespace PedidosApp.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            // For Empleados, only allow creating Cliente role
            if (User.IsInRole("Empleado"))
            {
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
            }
            else
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            }
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Password,Role")] User user)
        {
            // If user is Empleado, enforce that they can only create Cliente accounts
            if (User.IsInRole("Empleado") && user.Role != UserRole.Cliente)
            {
                ModelState.AddModelError("Role", "Como Empleado, solo puedes crear cuentas de Cliente");
            }

            if (ModelState.IsValid)
            {
                user.CreatedAt = DateTime.Now; // Use local time for consistency
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
            // Re-populate roles dropdown based on user role
            if (User.IsInRole("Empleado"))
            {
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
            }
            else
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // For Empleados, don't allow editing Admin or Empleado roles
            if (User.IsInRole("Empleado") && (user.Role == UserRole.Admin || user.Role == UserRole.Empleado))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Set up role dropdown based on user's role
            if (User.IsInRole("Empleado"))
            {
                // Empleados can only set Cliente role
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
            }
            else
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            }
            
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Password,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            // If user is Empleado, ensure they're not changing roles to Admin/Empleado
            if (User.IsInRole("Empleado"))
            {
                var originalUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (originalUser != null && (originalUser.Role == UserRole.Admin || originalUser.Role == UserRole.Empleado))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
                
                if (user.Role != UserRole.Cliente)
                {
                    ModelState.AddModelError("Role", "Como Empleado, solo puedes asignar el rol de Cliente");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the original user to keep CreatedAt
                    var originalUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                    if (originalUser != null)
                    {
                        user.CreatedAt = originalUser.CreatedAt;
                    }
                    
                    user.UpdatedAt = DateTime.Now; // Use local time for consistency
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
            
            // Re-populate roles dropdown based on user role
            if (User.IsInRole("Empleado"))
            {
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
            }
            else
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Empleados cannot delete Admin or Empleado users
            if (User.IsInRole("Empleado") && (user.Role == UserRole.Admin || user.Role == UserRole.Empleado))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Empleados cannot delete Admin or Empleado users
            if (User.IsInRole("Empleado") && (user.Role == UserRole.Admin || user.Role == UserRole.Empleado))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}