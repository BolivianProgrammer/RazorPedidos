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
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Index()
        {
            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);
            
            var users = await _context.Users
                .Where(u => u.Id != currentUserId)
                .ToListAsync();
                
            return View(users);
        }

        // GET: Users/Details/5
        [Authorize(Roles = "Admin,Empleado")]
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
        [Authorize(Roles = "Admin,Empleado")]
        public IActionResult Create()
        {
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
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Create([Bind("Name,Email,Password,Role")] User user)
        {
            if (User.IsInRole("Empleado") && user.Role != UserRole.Cliente)
            {
                ModelState.AddModelError("Role", "Como Empleado, solo puedes crear cuentas de Cliente");
            }

            if (ModelState.IsValid)
            {
                user.CreatedAt = DateTime.Now; 
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            
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
        [Authorize]
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
            
            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);

            bool isEditingSelf = currentUserId == user.Id;
            
            if (User.IsInRole("Cliente") && !isEditingSelf)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (User.IsInRole("Empleado") && !isEditingSelf && user.Role != UserRole.Cliente)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            user.Password = string.Empty;
            
            if (User.IsInRole("Cliente") || isEditingSelf)
            {
                
                ViewData["Roles"] = new SelectList(new[] { user.Role });
                ViewData["CanChangePassword"] = true;
            }
            else if (User.IsInRole("Empleado"))
            {
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
                ViewData["CanChangePassword"] = false; 
            }
            else
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["CanChangePassword"] = false; 
            }
            
            ViewData["IsEditingSelf"] = isEditingSelf;
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Role,Password")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);

            bool isEditingSelf = currentUserId == user.Id;

            if (User.IsInRole("Cliente") && !isEditingSelf)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (User.IsInRole("Empleado") && !isEditingSelf && user.Role != UserRole.Cliente)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var originalUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (originalUser == null)
            {
                return NotFound();
            }
            
            if (!isEditingSelf)
            {
                user.Password = originalUser.Password;
            }
            else if (string.IsNullOrWhiteSpace(user.Password))
            {
                user.Password = originalUser.Password;
            }

            if (isEditingSelf && !User.IsInRole("Admin"))
            {
                user.Role = originalUser.Role;
            }
            else if (User.IsInRole("Empleado") && !isEditingSelf)
            {
                if (user.Role != UserRole.Cliente)
                {
                    ModelState.AddModelError("Role", "Como Empleado, solo puedes asignar el rol de Cliente");
                    user.Role = UserRole.Cliente;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.CreatedAt = originalUser.CreatedAt;
                    user.UpdatedAt = DateTime.Now; 
                    
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    
                    if (isEditingSelf)
                    {
                        TempData["Success"] = "Tu perfil ha sido actualizado exitosamente";
                    }
                    else
                    {
                        TempData["Success"] = "Usuario actualizado exitosamente";
                    }
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
                
                if (isEditingSelf && User.IsInRole("Cliente"))
                {
                    return RedirectToAction("Index", "Home");
                }
                
                return RedirectToAction(nameof(Index));
            }
            
            if (User.IsInRole("Cliente") || isEditingSelf)
            {
                ViewData["Roles"] = new SelectList(new[] { user.Role });
                ViewData["CanChangePassword"] = true; 
            }
            else if (User.IsInRole("Empleado"))
            {
                ViewData["Roles"] = new SelectList(new[] { UserRole.Cliente });
                ViewData["CanChangePassword"] = false; 
            }
            else 
            {
                ViewData["Roles"] = new SelectList(Enum.GetValues(typeof(UserRole)));
                ViewData["CanChangePassword"] = false; 
            }
            
            ViewData["IsEditingSelf"] = isEditingSelf;
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin,Empleado")]
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
            
            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);

            if (currentUserId == user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (User.IsInRole("Empleado") && (user.Role == UserRole.Admin || user.Role == UserRole.Empleado))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            int.TryParse(User.FindFirst("UserId")?.Value, out int currentUserId);

            if (currentUserId == user.Id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
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