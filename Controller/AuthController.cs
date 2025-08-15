using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace SXBIM_Login.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // === 按你的数据库实际长度修改这些常量 ===
        private const int MaxUsernameLen = 50;
        private const int MaxPasswordLen = 128;
        private const int MaxAppLen = 50;
        private const int MaxActionLen = 200;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // 小工具：裁剪字符串到指定长度（null -> ""），并去掉首尾空格
        private static string Trunc(string? s, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim();
            return s.Length > maxLen ? s.Substring(0, maxLen) : s;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 统一做安全裁剪
            var u = Trunc(request.Username, MaxUsernameLen);
            var p = Trunc(request.Password, MaxPasswordLen);
            var app = Trunc(request.App, MaxAppLen);

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // 登录校验
            const string query = "SELECT COUNT(*) FROM Users WHERE Username = @username AND [Password] = @password";
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                cmd.Parameters.Add("@password", SqlDbType.NVarChar, MaxPasswordLen).Value = p;

                var result = (int)await cmd.ExecuteScalarAsync();

                // 查询真实姓名（仅用于日志/提示）
                const string realNameQuery = "SELECT RealName FROM Users WHERE Username = @username";
                string realName;
                using (var nameCmd = new SqlCommand(realNameQuery, conn))
                {
                    nameCmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                    var realNameObj = await nameCmd.ExecuteScalarAsync();
                    realName = realNameObj?.ToString() ?? u;
                }

                if (result > 0)
                {
                    // 登录成功，插入登录历史（明确类型与长度，避免截断异常）
                    const string insertLog = "INSERT INTO LoginHistory (Username, App) VALUES (@username, @app)";
                    using var logCmd = new SqlCommand(insertLog, conn);
                    logCmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                    logCmd.Parameters.Add("@app", SqlDbType.NVarChar, MaxAppLen).Value = app;
                    await logCmd.ExecuteNonQueryAsync();

                    // 如果有裁剪，做个提示日志，便于定位异常调用方
                    if (u != (request.Username ?? string.Empty).Trim() || app != (request.App ?? string.Empty).Trim())
                        Console.WriteLine($"[WARN] login fields truncated: Username/App");

                    Console.WriteLine($"[{DateTime.Now}] ------User login successful：[ {realName} ] -> [ {app} ]");
                    return Ok(new { success = true });
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] ------User login failed：[ {realName} ]");
                    return Unauthorized(new { success = false, message = "用户名或密码错误" });
                }
            }
        }

        [HttpPost("write")]
        public async Task<IActionResult> WriteLog([FromBody] LogRequest request)
        {
            // 安全裁剪
            var u = Trunc(request.Username, MaxUsernameLen);
            var act = Trunc(request.Action, MaxActionLen);
            var app = Trunc(request.App, MaxAppLen);

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // 查询真实姓名（日志展示用）
            const string realNameQuery = "SELECT RealName FROM Users WHERE Username = @username";
            string realName;
            using (var nameCmd = new SqlCommand(realNameQuery, conn))
            {
                nameCmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                var realNameObj = await nameCmd.ExecuteScalarAsync();
                realName = realNameObj?.ToString() ?? u;
            }

            const string insertQuery = @"
                INSERT INTO OperationLog (Username, Action, App, ActionTime)
                VALUES (@username, @action, @app, @actionTime)";
            using (var cmd = new SqlCommand(insertQuery, conn))
            {
                cmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                cmd.Parameters.Add("@action", SqlDbType.NVarChar, MaxActionLen).Value = act;
                cmd.Parameters.Add("@app", SqlDbType.NVarChar, MaxAppLen).Value = app;
                cmd.Parameters.Add("@actionTime", SqlDbType.DateTime).Value = DateTime.Now;
                await cmd.ExecuteNonQueryAsync();
            }

            if (u != (request.Username ?? string.Empty).Trim() ||
                act != (request.Action ?? string.Empty).Trim() ||
                app != (request.App ?? string.Empty).Trim())
            {
                Console.WriteLine($"[WARN] write fields truncated: Username/Action/App");
            }

            Console.WriteLine($"[{DateTime.Now}] ------User command：{app} -> [ {realName} ] -> [ {act} ]");
            return Ok(new { success = true });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            // 校验与裁剪
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.OldPassword) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { success = false, message = "用户名、原密码、新密码不能为空" });

            if (req.NewPassword == req.OldPassword)
                return BadRequest(new { success = false, message = "新密码不能与原密码相同" });

            if (req.NewPassword.Length < 3)
                return BadRequest(new { success = false, message = "新密码长度至少为 3 位" });

            var u = Trunc(req.Username, MaxUsernameLen);
            var oldp = Trunc(req.OldPassword, MaxPasswordLen);
            var newp = Trunc(req.NewPassword, MaxPasswordLen);

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // 1) 校验旧密码
            const string checkSql = "SELECT COUNT(*) FROM Users WHERE Username=@u AND [Password]=@p";
            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.Add("@u", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                checkCmd.Parameters.Add("@p", SqlDbType.NVarChar, MaxPasswordLen).Value = oldp;
                var cnt = (int)await checkCmd.ExecuteScalarAsync();
                if (cnt == 0)
                    return Unauthorized(new { success = false, message = "原密码不正确" });
            }

            // 2) 更新新密码
            const string updateSql = "UPDATE Users SET [Password]=@np WHERE Username=@u";
            int rows;
            using (var updateCmd = new SqlCommand(updateSql, conn))
            {
                updateCmd.Parameters.Add("@u", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                updateCmd.Parameters.Add("@np", SqlDbType.NVarChar, MaxPasswordLen).Value = newp;
                rows = await updateCmd.ExecuteNonQueryAsync();
            }
            if (rows <= 0)
                return StatusCode(500, new { success = false, message = "修改密码失败，请稍后重试" });

            // 3) 取真实姓名（仅用于日志打印）
            string realName;
            const string rnQuery = "SELECT RealName FROM Users WHERE Username=@username";
            using (var nameCmd = new SqlCommand(rnQuery, conn))
            {
                nameCmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                var realNameObj = await nameCmd.ExecuteScalarAsync();
                realName = realNameObj?.ToString() ?? u;
            }

            // 4) 写操作日志
            const string logSql = @"
                INSERT INTO OperationLog (Username, Action, ActionTime)
                VALUES (@username, @action, @time)";
            using (var logCmd = new SqlCommand(logSql, conn))
            {
                logCmd.Parameters.Add("@username", SqlDbType.NVarChar, MaxUsernameLen).Value = u;
                logCmd.Parameters.Add("@action", SqlDbType.NVarChar, MaxActionLen).Value = "ChangePassword";
                logCmd.Parameters.Add("@time", SqlDbType.DateTime).Value = DateTime.Now;
                await logCmd.ExecuteNonQueryAsync();
            }

            if (u != (req.Username ?? string.Empty).Trim())
                Console.WriteLine($"[WARN] change-password fields truncated: Username");

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
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string App { get; set; } = default!;
    }
    public class LogRequest
    {
        public string Username { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string App { get; set; } = default!;
    }

    public class 请求方法
    {
        public static string UrlIP = "http://192.168.7.194";
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
