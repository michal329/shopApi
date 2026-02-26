using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public record ProductDTO(
     
     int ProductId,

     string ProductName,

     decimal Price,

     int CategoryId,

     string Description,

     int Quantity,

     string? ImageUrl,

     bool IsActive
        );
    

}
