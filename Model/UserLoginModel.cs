using System.ComponentModel.DataAnnotations;

namespace authcheck.Model
{
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
}
