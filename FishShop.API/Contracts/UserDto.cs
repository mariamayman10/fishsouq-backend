namespace FishShop.API.Contracts;

public class UserDto
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string Gender { get; set; }
    public string JoinDate { get; set; }
    public string Age { get; set; }
    public string Role { get; set; }
    public int OrdersCount { get; set; }
    public string PhoneNumber { get; set; }
    public string Governorate { get; set; }
    public string Area  { get; set; }
    public AdminPrivilegesDto? Privileges { get; set; }


    public List<OrderDetails>? Orders { get; set; }
}