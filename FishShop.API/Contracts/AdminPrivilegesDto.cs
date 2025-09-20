namespace FishShop.API.Contracts;

public class AdminPrivilegesDto
{
    public bool CanAddProduct { get; set; }
    public bool CanUpdateProduct { get; set; }
    public bool CanDeleteProduct { get; set; }
    public bool CanAddCategory { get; set; }
    public bool CanUpdateCategory { get; set; }
    public bool CanDeleteCategory { get; set; }
    public bool CanUpdateOrderStatus { get; set; }
}