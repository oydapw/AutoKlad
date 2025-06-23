using Microsoft.AspNetCore.Identity;

namespace AutoPartsShop.Models
{
    public class Cart
    {
        public int CartId { get; set; }
        public string UserId { get; set; } 
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public IdentityUser User { get; set; } 
    }
}