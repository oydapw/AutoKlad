using System.ComponentModel.DataAnnotations;

namespace AutoPartsShop.Models
{
    public class ServiceBooking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Service Service { get; set; }
    }
}