using System.Collections.Generic;
using AutoPartsShop.Models;

public class OrdersViewModel
{
    public IEnumerable<Order> Orders { get; set; }
    public IEnumerable<ServiceBooking> ServiceBookings { get; set; }
}
