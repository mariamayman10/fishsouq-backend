namespace FishShop.API.Shared;

public static class LengthConstants
{
    public const int CategoryName = 50;
    public const int ProductName = 50;
    public const int ProductDescription = 1200;
    public const int AddressName = 50;
    public const int Governorate = 50;
    public const int Street = 80;
    public const int BuildingNumber = 3;
    public const int FloorNumber = 3;
    public const int AptNumber = 3;
    public const int ImageUrl = 2000;
    public const int PromoCode = 50;
}

public static class PolicyConstants
{
    public const string AdminPolicy = "AdminPolicy";
    public const string UserPolicy = "UserPolicy";
    public const string ManagerPolicy = "ManagerPolicy";
    public const string ManagerOrAdminPolicy = "ManagerOrAdminPolicy";
}

public static class RateLimitPolicyConstants
{
    public const string Sliding = "sliding";
    public const string Concurrent = "concurrent";
}


public static class RolesConstants
{
    public const string AdminRole = "AdminRole";
    public const string UserRole = "UserRole";
    public const string ManagerRole = "ManagerRole";
}

public static class ClaimsConstants
{
    public const string AgeClaim = "Age";
    public const string JoinDateClaim = "JoinDate";
}