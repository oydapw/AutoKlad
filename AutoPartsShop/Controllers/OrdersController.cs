using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoPartsShop.Models;
using AutoPartsShop.Models.ViewModels;
using System.Security.Claims;
using NReco.PdfGenerator;

namespace AutoPartsShop.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AutoPartsShopContext _context;

        public OrdersController(AutoPartsShopContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var serviceBookings = await _context.ServiceBookings
                .Include(sb => sb.Service)
                .Where(sb => sb.UserId == userId)
                .ToListAsync();

            var viewModel = new OrdersViewModel
            {
                Orders = orders,
                ServiceBookings = serviceBookings
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Checkout(int cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Корзина пуста или не найдена.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartId = cart.CartId,
                CartItems = cart.CartItems,
                TotalAmount = cart.CartItems.Sum(ci => ci.PriceAtTimeOfAddition * ci.Quantity)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CartId == model.CartId && c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Корзина пуста или не найдена.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.CartItems = cart.CartItems;
                model.TotalAmount = cart.CartItems.Sum(ci => ci.PriceAtTimeOfAddition * ci.Quantity);
                return View("Checkout", model);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var order = new Order
                    {
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        TotalAmount = cart.CartItems.Sum(ci => ci.PriceAtTimeOfAddition * ci.Quantity),
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        FullName = model.FullName,
                        Address = model.Address,
                        City = model.City,
                        PostalCode = model.PostalCode,
                        PhoneNumber = model.PhoneNumber,
                        PaymentMethod = model.PaymentMethod
                    };

                    foreach (var item in cart.CartItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product == null || product.Stock < item.Quantity)
                        {
                            TempData["Error"] = $"Недостаточно товара на складе: {product?.Name ?? "Неизвестный товар"}. Доступно: {product?.Stock ?? 0}, запрошено: {item.Quantity}.";
                            return RedirectToAction("Checkout", new { cartId = cart.CartId });
                        }

                        product.Stock -= item.Quantity; // Списываем количество с товара
                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.PriceAtTimeOfAddition
                        });
                    }

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync(); // Сохраняем заказ и обновляем Stock

                    _context.CartItems.RemoveRange(cart.CartItems);
                    cart.CartItems.Clear();
                    cart.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync(); // Очищаем корзину

                    transaction.Commit(); // Подтверждаем транзакцию

                    var htmlContent = GenerateReceiptHtml(order);
                    ViewBag.HtmlContent = htmlContent;
                    ViewBag.OrderId = order.OrderId;

                    TempData["Success"] = "Заказ успешно оформлен!";
                    return View("Receipt", order);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = $"Ошибка при оформлении заказа: {ex.Message}";
                    return RedirectToAction("Checkout", new { cartId = cart.CartId });
                }
            }
        }

        [HttpGet]
        public IActionResult DownloadPdf(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var htmlContent = GenerateReceiptHtml(order);
            var pdf = new HtmlToPdfConverter();
            var pdfBytes = pdf.GeneratePdf(htmlContent);

            return File(pdfBytes, "application/pdf", $"receipt_{orderId}.pdf");
        }

        private string GenerateReceiptHtml(Order order)
        {
            var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <title>Чек заказа №{order.OrderId}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20mm; }}
        h1 {{ text-align: center; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
        .total {{ font-weight: bold; }}
    </style>
</head>
<body>
    <h1>Чек заказа №{order.OrderId}</h1>
    <p style=""text-align: center;"">Магазин автозапчастей DetailBy<br>Дата: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}</p>

    <h2>Детали заказа</h2>
    <table>
        <tr>
            <th>Наименование</th>
            <th>Количество</th>
            <th>Цена (BYN)</th>
        </tr>";
            foreach (var detail in order.OrderDetails)
            {
                html += $@"
        <tr>
            <td>{detail.Product.Name}</td>
            <td>{detail.Quantity}</td>
            <td>{detail.UnitPrice}</td>
        </tr>";
            }
            html += $@"
        <tr>
            <td colspan=""2"" class=""total"">Итого</td>
            <td class=""total"">{order.TotalAmount}</td>
        </tr>
    </table>

    <h2>Информация о покупателе</h2>
    <ul>
        <li><strong>Полное имя:</strong> {order.FullName}</li>
        <li><strong>Адрес:</strong> {order.Address}, {order.City}, {order.PostalCode}</li>
        <li><strong>Телефон:</strong> {order.PhoneNumber}</li>
        <li><strong>Способ оплаты:</strong> {order.PaymentMethod}</li>
    </ul>
</body>
</html>";
            return html;
        }
    }
}