using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SXBIM_Login.Controller
{
    [ApiController]
    [Route("api/[controller]")] // => /api/stats/...
    public class StatsController : ControllerBase
    {
        private readonly string _conn;
        public StatsController(IConfiguration cfg)
        {
            _conn = cfg.GetConnectionString("DefaultConnection")
                   ?? throw new Exception("Missing ConnectionStrings:DefaultConnection");
        }

        // ========= 登录：返回原始记录 =========
        // GET /api/stats/login-all?from=2025-07-01&to=2025-08-31&app=cad&limit=10000
        // 不传 from/to 就全量；支持按 App 过滤；可用 limit 限制返回条数（默认 50000）
        [HttpGet("login-all")]
        public IActionResult GetLoginAll([FromQuery] DateTime? from, [FromQuery] DateTime? to,
                                         [FromQuery] string? app, [FromQuery] int? limit)
        {
            var rows = new List<object>();
            using var con = new SqlConnection(_conn);
            con.Open();

            var sql = @"
SELECT {TOP}
       L.LoginTime,
       L.Username,
       ISNULL(U.RealName, L.Username) AS RealName,
       L.App
FROM dbo.LoginHistory AS L WITH (NOLOCK)
LEFT JOIN dbo.Users      AS U WITH (NOLOCK) ON U.Username = L.Username
WHERE 1=1
  {W_FROM}
  {W_TO}
  {W_APP}
ORDER BY L.LoginTime ASC;";

            // 组装 where
            var where_from = from.HasValue ? "AND L.LoginTime >= @from" : "";
            var where_to = to.HasValue ? "AND L.LoginTime <= @to" : "";
            var where_app = !string.IsNullOrWhiteSpace(app) ? "AND L.App = @app" : "";
            sql = sql.Replace("{W_FROM}", where_from)
                     .Replace("{W_TO}", where_to)
                     .Replace("{W_APP}", where_app)
                     .Replace("{TOP}", $"TOP({Math.Clamp(limit ?? 50000, 1, 2_000_000)})");

            using var cmd = new SqlCommand(sql, con);
            if (from.HasValue) cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Value = from.Value });
            if (to.HasValue) cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Value = to.Value });
            if (!string.IsNullOrWhiteSpace(app)) cmd.Parameters.Add(new SqlParameter("@app", SqlDbType.NVarChar, 50) { Value = app! });

            using var rd = cmd.ExecuteReader();

            // 用列名获取序号，避免列顺序变化
            int ordLoginTime = rd.GetOrdinal("LoginTime");
            int ordUsername = rd.GetOrdinal("Username");
            int ordRealName = rd.GetOrdinal("RealName");
            int ordApp = rd.GetOrdinal("App");

            while (rd.Read())
            {
                rows.Add(new
                {
                    LoginTime = rd.GetDateTime(ordLoginTime),
                    Username = rd.GetString(ordUsername),
                    RealName = rd.IsDBNull(ordRealName) ? rd.GetString(ordUsername) : rd.GetString(ordRealName),
                    App = rd.IsDBNull(ordApp) ? "" : rd.GetString(ordApp)
                });
            }
            return Ok(rows);
        }

        // ========= 操作：返回原始记录 =========
        // GET /api/stats/op-all?from=2025-07-01&to=2025-08-31&app=rhino&limit=10000
        [HttpGet("op-all")]
        public IActionResult GetOpAll([FromQuery] DateTime? from, [FromQuery] DateTime? to,
                                      [FromQuery] string? app, [FromQuery] int? limit)
        {
            var rows = new List<object>();
            using var con = new SqlConnection(_conn);
            con.Open();

            var sql = @"
SELECT {TOP}
       O.ActionTime,
       O.Username,
       ISNULL(U.RealName, O.Username) AS RealName,
       O.[Action],
       O.App
FROM dbo.OperationLog AS O WITH (NOLOCK)
LEFT JOIN dbo.Users    AS U WITH (NOLOCK) ON U.Username = O.Username
WHERE 1=1
  {W_FROM}
  {W_TO}
  {W_APP}
ORDER BY O.ActionTime ASC;";

            var where_from = from.HasValue ? "AND O.ActionTime >= @from" : "";
            var where_to = to.HasValue ? "AND O.ActionTime <= @to" : "";
            var where_app = !string.IsNullOrWhiteSpace(app) ? "AND O.App = @app" : "";
            sql = sql.Replace("{W_FROM}", where_from)
                     .Replace("{W_TO}", where_to)
                     .Replace("{W_APP}", where_app)
                     .Replace("{TOP}", $"TOP({Math.Clamp(limit ?? 50000, 1, 2_000_000)})");

            using var cmd = new SqlCommand(sql, con);
            if (from.HasValue) cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Value = from.Value });
            if (to.HasValue) cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Value = to.Value });
            if (!string.IsNullOrWhiteSpace(app)) cmd.Parameters.Add(new SqlParameter("@app", SqlDbType.NVarChar, 50) { Value = app! });

            using var rd = cmd.ExecuteReader();

            int ordActionTime = rd.GetOrdinal("ActionTime");
            int ordUsername = rd.GetOrdinal("Username");
            int ordRealName = rd.GetOrdinal("RealName");
            int ordAction = rd.GetOrdinal("Action");
            int ordApp = rd.GetOrdinal("App");

            while (rd.Read())
            {
                rows.Add(new
                {
                    ActionTime = rd.GetDateTime(ordActionTime),
                    Username = rd.GetString(ordUsername),
                    RealName = rd.IsDBNull(ordRealName) ? rd.GetString(ordUsername) : rd.GetString(ordRealName),
                    Action = rd.IsDBNull(ordAction) ? "" : rd.GetString(ordAction),
                    App = rd.IsDBNull(ordApp) ? "" : rd.GetString(ordApp)
                });
            }
            return Ok(rows);
        }
    }
}
