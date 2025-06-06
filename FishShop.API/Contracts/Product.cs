﻿namespace FishShop.API.Contracts;

public record Product
{
    public int Id { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public string? Description { get; init; }
}