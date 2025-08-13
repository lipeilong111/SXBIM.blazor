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
       LoginTime, Username, App
FROM dbo.LoginHistory WITH (NOLOCK)
WHERE 1=1
  {W_FROM}
  {W_TO}
  {W_APP}
ORDER BY LoginTime ASC;";

            // 组装 where
            var where_from = from.HasValue ? "AND LoginTime >= @from" : "";
            var where_to = to.HasValue ? "AND LoginTime <= @to" : "";
            var where_app = !string.IsNullOrWhiteSpace(app) ? "AND App = @app" : "";
            sql = sql.Replace("{W_FROM}", where_from)
                     .Replace("{W_TO}", where_to)
                     .Replace("{W_APP}", where_app)
                     .Replace("{TOP}", $"TOP({Math.Clamp(limit ?? 50000, 1, 2_000_000)})");

            using var cmd = new SqlCommand(sql, con);
            if (from.HasValue) cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Value = from.Value });
            if (to.HasValue) cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Value = to.Value });
            if (!string.IsNullOrWhiteSpace(app)) cmd.Parameters.Add(new SqlParameter("@app", SqlDbType.NVarChar, 50) { Value = app! });

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                rows.Add(new
                {
                    // 前端已做宽容映射，这里统一字段名更省心
                    LoginTime = rd.GetDateTime(0),
                    Username = rd.GetString(1),
                    App = rd.IsDBNull(2) ? "" : rd.GetString(2)
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
       ActionTime, Username, [Action], App
FROM dbo.OperationLog WITH (NOLOCK)
WHERE 1=1
  {W_FROM}
  {W_TO}
  {W_APP}
ORDER BY ActionTime ASC;";

            var where_from = from.HasValue ? "AND ActionTime >= @from" : "";
            var where_to = to.HasValue ? "AND ActionTime <= @to" : "";
            var where_app = !string.IsNullOrWhiteSpace(app) ? "AND App = @app" : "";
            sql = sql.Replace("{W_FROM}", where_from)
                     .Replace("{W_TO}", where_to)
                     .Replace("{W_APP}", where_app)
                     .Replace("{TOP}", $"TOP({Math.Clamp(limit ?? 50000, 1, 2_000_000)})");

            using var cmd = new SqlCommand(sql, con);
            if (from.HasValue) cmd.Parameters.Add(new SqlParameter("@from", SqlDbType.DateTime2) { Value = from.Value });
            if (to.HasValue) cmd.Parameters.Add(new SqlParameter("@to", SqlDbType.DateTime2) { Value = to.Value });
            if (!string.IsNullOrWhiteSpace(app)) cmd.Parameters.Add(new SqlParameter("@app", SqlDbType.NVarChar, 50) { Value = app! });

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                rows.Add(new
                {
                    ActionTime = rd.GetDateTime(0),
                    Username = rd.GetString(1),
                    Action = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    App = rd.IsDBNull(3) ? "" : rd.GetString(3)
                });
            }
            return Ok(rows);
        }
    }
}
