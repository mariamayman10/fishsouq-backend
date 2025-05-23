﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FishShop.API.Entities;

public class ProductSales
{
    [Key] [ForeignKey("Product")] public int ProductId { get; init; }

    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }

    public Product? Product { get; init; }
}