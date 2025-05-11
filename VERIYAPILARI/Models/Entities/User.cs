using System.ComponentModel.DataAnnotations;

namespace VERIYAPILARI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        public string Role { get; set; } = "User";
    }
} 