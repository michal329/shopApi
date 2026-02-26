
namespace DTOs
{
    public record OrderDTO
    (
        int? OrderId,

        DateOnly OrderDate ,

        double OrderSum,

        int UserId,

        string Status,

        ICollection<OrderItemDTO> OrderItems
    );
}

  
