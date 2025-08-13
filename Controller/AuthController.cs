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
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.OldPassword) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { success = false, message = "用户名、原密码、新密码不能为空" });

            if (req.NewPassword == req.OldPassword)
                return BadRequest(new { success = false, message = "新密码不能与原密码相同" });

            if (req.NewPassword.Length < 3)
                return BadRequest(new { success = false, message = "新密码长度至少为 3 位" });

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // 1) 校验旧密码
            const string checkSql = "SELECT COUNT(*) FROM Users WHERE Username=@u AND Password=@p";
            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@u", req.Username);
                checkCmd.Parameters.AddWithValue("@p", req.OldPassword);
                var cnt = (int)await checkCmd.ExecuteScalarAsync();
                if (cnt == 0)
                    return Unauthorized(new { success = false, message = "原密码不正确" });
            }

            // 2) 更新新密码
            const string updateSql = "UPDATE Users SET Password=@np WHERE Username=@u";
            int rows;
            using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                updateCmd.Parameters.AddWithValue("@u", req.Username);
                updateCmd.Parameters.AddWithValue("@np", req.NewPassword);
                rows = await updateCmd.ExecuteNonQueryAsync();
            }
            if (rows <= 0)
                return StatusCode(500, new { success = false, message = "修改密码失败，请稍后重试" });

            // 3) 取真实姓名（仅用于日志打印）
            string realName;
            const string realNameQuery = "SELECT RealName FROM Users WHERE Username=@username";
            using (var nameCmd = new SqlCommand(realNameQuery, conn))
            {
                nameCmd.Parameters.AddWithValue("@username", req.Username);
                var realNameObj = await nameCmd.ExecuteScalarAsync();
                realName = realNameObj?.ToString() ?? req.Username;
            }

            // 4) 写操作日志（不含 App 列）
            const string logSql = @"
        INSERT INTO OperationLog (Username, Action, ActionTime)
        VALUES (@username, @action, @time)";
            using (var logCmd = new SqlCommand(logSql, conn))
            {
                logCmd.Parameters.AddWithValue("@username", req.Username);
                logCmd.Parameters.AddWithValue("@action", "ChangePassword");
                logCmd.Parameters.AddWithValue("@time", DateTime.Now);
                await logCmd.ExecuteNonQueryAsync();
            }

            Console.WriteLine($"[{DateTime.Now}] ------User change password：[{realName}]  原密码: [{req.OldPassword}] -> 新密码: [{req.NewPassword}]");
            return Ok(new { success = true, message = "密码修改成功" });
        }
    }
    public class ChangePasswordRequest
    {
        public string Username { get; set; } = default!;
        public string OldPassword { get; set; } = default!;
        public string NewPassword { get; set; } = default!;
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




    public class 请求方法
    {
        public static string UrlIP= "http://192.168.7.194";
        public static HttpClient CreateUnsafeClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            return new HttpClient(handler);
        }
    }
}
