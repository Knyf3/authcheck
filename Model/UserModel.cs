using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace authcheck.Model
{
    public class UserRegisterModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string Email { get; set; } // Optional
    }

    public class UserLoginModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class AssignRoleModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Role { get; set; }
    }

    public class UserModel
    {
        public string UserName { get; set; }
       
        public string Email { get; set; } // Optional
        public string role { get; set; }
    }
}
