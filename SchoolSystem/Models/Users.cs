using Microsoft.AspNetCore.Identity;

namespace SchoolSystem.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
