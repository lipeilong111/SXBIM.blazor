using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace SXBIM_Login.Services
{
    public class LoginService
    {
        private readonly IConfiguration _configuration;

        public LoginService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async static Task 录入表格信息(SqlConnection conn, string filepath)
        {
            string excelFile = filepath;
            //using var package = new ExcelPackage(new FileInfo(excelFile));
            //var worksheet = package.Workbook.Worksheets[0]; // 读取第一个Sheet
            //int rowCount = worksheet.Dimension.Rows;


            var lines = File.ReadAllLines(@"C:\Users\Public\Nwt\cache\recv\欧天行\sys_user_202507111449.csv");
            foreach (var line in lines)
            {
                var fields = line.Split(','); // 按逗号分割
                string username1 = fields[5];
                string realName1 = fields[4];
                string sql = @"
        INSERT INTO Users (Username ,RealName, Department, Position, CanLogin)
        VALUES (@Username, @RealName, @Department, @Position, @CanLogin)";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Username", username1);
                cmd.Parameters.AddWithValue("@RealName", string.IsNullOrEmpty(realName1) ? (object)DBNull.Value : realName1);
                cmd.Parameters.AddWithValue("@Department", "深圳分院");
                cmd.Parameters.AddWithValue("@Position", "设计师");
                cmd.Parameters.AddWithValue("@CanLogin", 1);
                await cmd.ExecuteNonQueryAsync();
            }
        //    for (int row = 1; row <= rowCount; row++) // 假设第一行是表头，从第2行开始
        //    {
        //        string username1 = worksheet.Cells[row, 2].Text;
        //        string realName = worksheet.Cells[row, 1].Text;

        //        string sql = @"
        //INSERT INTO Users (Username ,RealName, Department, Position, CanLogin)
        //VALUES (@Username, @Password, @RealName, @Department, @Position, @CanLogin)";

        //        cmd.Parameters.AddWithValue("@Username", username1);
        //        cmd.Parameters.AddWithValue("@RealName", string.IsNullOrEmpty(realName) ? (object)DBNull.Value : realName);
        //        cmd.Parameters.AddWithValue("@Department", "深圳分院");
        //        cmd.Parameters.AddWithValue("@Position", "设计师");
        //        cmd.Parameters.AddWithValue("@CanLogin", 1);

        //        await cmd.ExecuteNonQueryAsync();
        //    }
        }


        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            try
            {
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @username AND Password = @password";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", password);

                int result = (int)await cmd.ExecuteScalarAsync();

                if (result > 0)
                {
                    // 登录成功，插入登录记录
                    string insertLog = "INSERT INTO LoginHistory (Username) VALUES (@username)";
                    using var logCmd = new SqlCommand(insertLog, conn);
                    logCmd.Parameters.AddWithValue("@username", username);
                    Console.WriteLine($"[{DateTime.Now}] 用户登录成功：{username}");

                    //await 录入表格信息(conn, @"C:\Users\hw\Desktop\用户名列表.xlsx");

                    await logCmd.ExecuteNonQueryAsync();

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("数据库连接或验证失败：" + ex.Message);
                throw;
            }
        }
    }
}
