using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoPartsShop.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;

namespace AutoPartsShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly AutoPartsShopContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(AutoPartsShopContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string category)
        {
            var productsQuery = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            var products = await productsQuery.ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> ResetPasswords()
        {
            string message = "";
            var admin = await _userManager.FindByEmailAsync("admin@example.com");
            if (admin != null)
            {
                var adminToken = await _userManager.GeneratePasswordResetTokenAsync(admin);
                var adminResult = await _userManager.ResetPasswordAsync(admin, adminToken, "Admin123!");
                if (adminResult.Succeeded)
                {
                    message += "Пароль администратора сброшен на Admin123!<br>";
                    admin.EmailConfirmed = true;
                    await _userManager.UpdateAsync(admin);
                }
                else
                {
                    message += "Ошибка сброса пароля администратора: " + string.Join(", ", adminResult.Errors.Select(e => e.Description)) + "<br>";
                }
            }

            var user = await _userManager.FindByEmailAsync("user@example.com");
            if (user != null)
            {
                var userToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var userResult = await _userManager.ResetPasswordAsync(user, userToken, "User123!");
                if (userResult.Succeeded)
                {
                    message += "Пароль пользователя сброшен на User123!<br>";
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    message += "Ошибка сброса пароля пользователя: " + string.Join(", ", userResult.Errors.Select(e => e.Description));
                }
            }

            ViewBag.Message = message;
            return View("ResetResult");
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}