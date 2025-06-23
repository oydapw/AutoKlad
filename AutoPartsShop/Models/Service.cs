﻿namespace AutoPartsShop.Models
{
    public class Service
    {
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }  
        public DateTime? UpdatedAt { get; set; } 
    }
}