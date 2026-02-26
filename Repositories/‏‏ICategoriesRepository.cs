using Entities.Models;

namespace Repositories
{
    public interface ICategoriesRepository
    {
        Task<IEnumerable<Category>> GetCategories();

    }
}