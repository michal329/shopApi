using System.ComponentModel.DataAnnotations;

namespace DTOs
{
    public record ExisitingUserDTO
    (
        [EmailAddress]
        [Required]
        string Email,
        [Required]
        string Password
    );
}