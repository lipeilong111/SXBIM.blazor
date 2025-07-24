using Microsoft.AspNetCore.Mvc;

namespace SXBIM_Login.Services
{

    [ApiController]
    [Route("api/[controller]")]
    public class LoginApiController : ControllerBase
    {
        private readonly LoginService _loginService;

        public LoginApiController(LoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LoginRequest request)
        {
            var result = await _loginService.ValidateUserAsync(request.Username, request.Password);
            if (result)
                return Ok(new { success = true, message = "登录成功" });
            else
                return Unauthorized(new { success = false, message = "用户名或密码错误" });
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

    }
}
