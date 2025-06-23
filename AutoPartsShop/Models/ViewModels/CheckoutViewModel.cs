namespace AutoPartsShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public int CartId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();

        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string PhoneNumber { get; set; }

        // Способ оплаты
        public string PaymentMethod { get; set; } // Например, "CashOnDelivery", "CreditCard"
    }
}