using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoPartsShop.Models;
using System.Security.Claims;

namespace AutoPartsShop.Controllers
{
    public class ServiceController : Controller
    {
        private readonly AutoPartsShopContext _context;

        public ServiceController(AutoPartsShopContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _context.Services.ToListAsync();
            return View(services);
        }

        public IActionResult Book(int id)
        {
            var service = _context.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Book(int serviceId, string fullName, string phoneNumber, DateTime bookingDate)
        {
            var service = _context.Services.Find(serviceId); // Получаем модель для передачи в вид
            if (service == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(service); // Возвращаем модель при некорректных данных
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "Ошибка авторизации. Пожалуйста, войдите в систему.");
                return View(service); // Остаёмся на странице с предупреждением
            }

            // Проверка на дату, которая уже прошла (включая текущее время)
            var currentDateTime = DateTime.Now;
            if (bookingDate < currentDateTime)
            {
                ModelState.AddModelError("BookingDate", "Нельзя записаться на эту дату, так как она уже прошла.");
                return View(service); // Остаёмся на странице с предупреждением
            }

            // Округляем bookingDate до начала часа
            var roundedBookingDate = new DateTime(bookingDate.Year, bookingDate.Month, bookingDate.Day, bookingDate.Hour, 0, 0);

            // Проверка уникальности записи на основе начала часа
            var serviceBookings = await _context.ServiceBookings
                .Where(sb => sb.ServiceId == serviceId)
                .OrderBy(sb => sb.BookingDate)
                .ToListAsync();

            var existingBooking = serviceBookings
                .FirstOrDefault(sb =>
                    Math.Abs((new DateTime(sb.BookingDate.Year, sb.BookingDate.Month, sb.BookingDate.Day, sb.BookingDate.Hour, 0, 0) - roundedBookingDate).TotalMinutes) < 60);

            if (existingBooking != null)
            {
                // Находим следующее доступное время
                var nextAvailableTime = roundedBookingDate;
                while (serviceBookings.Any(sb =>
                    Math.Abs((new DateTime(sb.BookingDate.Year, sb.BookingDate.Month, sb.BookingDate.Day, sb.BookingDate.Hour, 0, 0) - nextAvailableTime).TotalMinutes) < 60))
                {
                    nextAvailableTime = nextAvailableTime.AddHours(1);
                }

                ModelState.AddModelError("BookingDate",
                    $"На эту услугу на этот час ({roundedBookingDate:dd.MM.yyyy HH:00}) уже записан другой человек. " +
                    $"Следующее доступное время: {nextAvailableTime:dd.MM.yyyy HH:00}.");
                return View(service); // Остаёмся на странице с предупреждением
            }

            var booking = new ServiceBooking
            {
                ServiceId = serviceId,
                UserId = userId,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                BookingDate = roundedBookingDate, // Сохраняем с началом часа
                CreatedAt = DateTime.Now,
                UpdatedAt = null // Устанавливаем как null, так как это nullable
            };

            try
            {
                _context.ServiceBookings.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Вы успешно записаны на услугу!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при сохранении записи: {ex.Message}");
                return View(service); // Остаёмся на странице с предупреждением об ошибке
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateService()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateService(Service service)
        {
            if (ModelState.IsValid)
            {
                service.CreatedAt = DateTime.Now;
                service.UpdatedAt = DateTime.Now;
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            return View(service);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditService(int id, Service service)
        {
            if (id != service.ServiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    service.UpdatedAt = DateTime.Now;
                    _context.Update(service);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Services.Any(e => e.ServiceId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(service);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.ServiceBookings
                .Include(b => b.Service)
                .ToListAsync();
            return View(bookings);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditBooking(int id)
        {
            var booking = await _context.ServiceBookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditBooking(int id, ServiceBooking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    booking.UpdatedAt = DateTime.Now;
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ServiceBookings.Any(e => e.BookingId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction("Bookings");
            }
            return View(booking);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.ServiceBookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            _context.ServiceBookings.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction("Bookings");
        }
    }
}