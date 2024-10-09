using BusinessObjects.Contracts.UnitOfWorks;
using BusinessObjects.Entities;
using BusinessObjects.Models.Response;
using Services.Interfaces;

namespace Services.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponse<IList<Category>>> GetCategories()
        {
            try
            {
                var categories = await _unitOfWork.CategoryRepository.GetCategories();
                return new BaseResponse<IList<Category>>
                {
                    IsSucceed = true,
                    Result = categories,
                    Message = "Categories retrieved successfully"
                };
            }
            catch (Exception e)
            {
                return new BaseResponse<IList<Category>>
                {
                    IsSucceed = false,
                    Message = $"Error retrieving categories: {e.Message}"
                };
            }
        }
    }
}
