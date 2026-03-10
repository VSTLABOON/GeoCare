using Microsoft.AspNetCore.Identity;

namespace GeoCare.Core.Entities;

//Se hereda de IdentityUSer para tener todo listo:
//(Password, hash, email, tel, etc)
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}