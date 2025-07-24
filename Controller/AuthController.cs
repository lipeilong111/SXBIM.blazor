using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace SXBIM_Login.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            string query = "SELECT COUNT(*) FROM Users WHERE Username = @username AND Password = @password";
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", request.Username);
            cmd.Parameters.AddWithValue("@password", request.Password);

            int result = (int)await cmd.ExecuteScalarAsync();

            // 查询真实姓名
            string realNameQuery = "SELECT RealName FROM Users WHERE Username = @username";
            using var nameCmd = new SqlCommand(realNameQuery, conn);
            nameCmd.Parameters.AddWithValue("@username", request.Username);
            var realNameObj = await nameCmd.ExecuteScalarAsync();
            string realName = realNameObj?.ToString() ?? request.Username; // 如果查不到就用用户名


            if (result > 0)
            {
                // 登录成功，插入日志
                string insertLog = "INSERT INTO LoginHistory (Username, App) VALUES (@username, @app)";
                using var logCmd = new SqlCommand(insertLog, conn);
                logCmd.Parameters.AddWithValue("@username", request.Username);
                logCmd.Parameters.AddWithValue("@app", request.App); // 或 "Rhino"，可以根据实际情况传参
                Console.WriteLine($"[{DateTime.Now}] ------User login successful：[ {realName} ]->[ {request.App} ]");
                await logCmd.ExecuteNonQueryAsync();

                return Ok(new { success = true });
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] ------User login failed：[ {realName} ]");
                return Unauthorized(new { success = false, message = "用户名或密码错误" });
            }
        }

        [HttpPost("write")]
        public async Task<IActionResult> WriteLog([FromBody] LogRequest request)
        {
            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();



            // 查询真实姓名
            string realNameQuery = "SELECT RealName FROM Users WHERE Username = @username";
            using var nameCmd = new SqlCommand(realNameQuery, conn);
            nameCmd.Parameters.AddWithValue("@username", request.Username);
            var realNameObj = await nameCmd.ExecuteScalarAsync();
            string realName = realNameObj?.ToString() ?? request.Username; // 如果查不到就用用户名



            string insertQuery = @"
            INSERT INTO OperationLog (Username, Action, App, ActionTime) 
            VALUES (@username, @action, @app, @actionTime)";

            using var cmd = new SqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("@username", request.Username);
            cmd.Parameters.AddWithValue("@action", request.Action);
            cmd.Parameters.AddWithValue("@app", request.App);
            cmd.Parameters.AddWithValue("@actionTime", DateTime.Now);
            Console.WriteLine($"[{DateTime.Now}] ------User command：{request.App}->[ {realName} ]->[ {request.Action} ]");
            await cmd.ExecuteNonQueryAsync();

            return Ok(new { success = true });
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string App { get; set; }
    }
    public class LogRequest
    {
        public string Username { get; set; }
        public string Action { get; set; }
        public string App { get; set; }
    }
}
