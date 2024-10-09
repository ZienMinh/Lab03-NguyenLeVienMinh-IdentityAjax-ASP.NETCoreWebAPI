using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Lab03_NguyenLeVienMinh_IdentityAjax_ASP.NETCoreWebAPI.Controllers
{
	[ApiController]
	[Route("api/categories")]
	public class CategoryController : ControllerBase
	{
		private readonly ICategoryService _categoryService;

		public CategoryController(ICategoryService categoryService)
		{
			_categoryService = categoryService;
		}

		// GET: api/categories
		[HttpGet]
		public async Task<IActionResult> GetCategories()
		{
			var response = await _categoryService.GetCategories();
			if (response.IsSucceed)
			{
				return Ok(response.Result);
			}
			return BadRequest(response.Message);
		}
	}
}
