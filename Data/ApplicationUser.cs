using Microsoft.AspNetCore.Identity;

namespace Otium.Data;

public class ApplicationUser : IdentityUser
{
    public string CodigoAmigo { get; set; } = "";
}
