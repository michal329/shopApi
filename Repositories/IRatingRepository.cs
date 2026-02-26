using Entities.Models;


namespace Repositories
{
    public interface IRatingRepository
    {
        Task<Rating>AddRating(Rating rating);
    }
}
