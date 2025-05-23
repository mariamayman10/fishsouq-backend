﻿using FishShop.API.Shared;

namespace FishShop.API.Entities;

public class Category : BaseEntity
{
    public int Id { get; init; }
    public string? Name { get; set; }

    public ICollection<Product>? Products { get; init; }
}