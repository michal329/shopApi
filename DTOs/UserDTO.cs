namespace DTOs
{
    // Returned to clients, so it never includes the password hash.
    public record UserDTO
    (
        int UserId,
        string Email,
        string FirstName,
        string LastName,
        bool IsAdmin,
        ICollection<OrderDTO> Orders
    );
}
