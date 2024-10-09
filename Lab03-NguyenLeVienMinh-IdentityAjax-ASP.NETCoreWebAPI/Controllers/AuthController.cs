using BusinessObjects.Models.Request;
using BusinessObjects.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Lab03_NguyenLeVienMinh_IdentityAjax_ASP.NETCoreWebAPI.Controllers
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IAuthService _authService;
		private readonly IUserService _userService;

		public AuthController(IAuthService authService, IUserService userService)
		{
			_authService = authService;
			_userService = userService;
		}

		[HttpPost]
		[Route("roles")]
		public async Task<IActionResult> SeedRoles()
		{
			var seedRoles = await _authService.SeedRolesAsync();

			return Ok(seedRoles);
		}

		[HttpPost]
		[Route("register")]
		public async Task<IActionResult> Register([FromBody][Required] RegisterResponse registerResponse)
		{
			if (!ModelState.IsValid)
				return BadRequest(new BaseResponse<object>
				{ IsSucceed = false, Message = "Invalid model state", Result = null });

			var authServiceResponse = await _authService.RegisterAsync(registerResponse);

			if (authServiceResponse.IsSucceed)
				return Ok(new BaseResponse<object>
				{
					IsSucceed = true,
					Message = "Account created successfully, please verify the token yourself!",
					Result = null
				});
			return BadRequest(new BaseResponse<object>
			{ IsSucceed = false, Message = authServiceResponse.Token, Result = null });
		}

		[HttpPost]
		[Route("login")]
		public async Task<IActionResult> Login([FromBody] LoginResponse loginResponse)
		{
			var loginResult = await _authService.LoginAsync(loginResponse);

			if (loginResult.IsSucceed)
				return Ok(loginResult);

			return BadRequest(new { message = "Username or password is incorrect" });
		}

		[HttpGet]
		[Route("verify")]
		public async Task<IActionResult> VerifyEmail(string email, string token)
		{
			try
			{
				var result = await _userService.VerifyEmailTokenAsync(email, token);

				if (result)
					return Ok(new BaseResponse<object>
					{ IsSucceed = true, Message = "Email verified successfully.", Result = null });
				return BadRequest(new BaseResponse<object>
				{ IsSucceed = false, Message = "Invalid or expired token.", Result = null });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(new BaseResponse<object> { IsSucceed = false, Message = ex.Message, Result = null });
			}
		}

		[HttpPost("token")]
		public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refresh)
		{
			try
			{
				var response = await _authService.RefreshTokenAsync(refresh);
				return Ok(response);
			}
			catch (SecurityTokenException ex)
			{
				return Unauthorized(new { message = ex.Message });
			}
		}

		[HttpPost]
		[Route("admin")]
		public async Task<IActionResult> MakeAdmin([FromBody] UpdatePermissionResponse updatePermissionResponse)
		{
			var operationResult = await _authService.MakeAdminAsync(updatePermissionResponse);

			if (operationResult.IsSucceed)
				return Ok(operationResult);

			return BadRequest(operationResult);
		}
	}
}
