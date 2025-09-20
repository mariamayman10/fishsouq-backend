namespace FishShop.API.Entities.Enums;

public enum OrderBy
{
    PriceDesc = 0,
    PriceAsc,

    SalesDesc,
    SalesAsc,

    DeliveryDateDesc,
    DeliveryDateAsc,

    CreatedAtDesc,
    CreatedAtAsc,
    
    AvQuantityAsc,
    AvQuantityDesc,
}