using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<SXBIM_Login.Services.LoginService>();
builder.Services.AddControllers();


var app = builder.Build();
app.UseHttpsRedirection();

app.UseStaticFiles();


app.UseRouting();




//自定义中间件，记录每个请求
app.Use(async (context, next) =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString();
    var path = context.Request.Path;
    var method = context.Request.Method;
    var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    Console.WriteLine($"[访问日志] 时间: {time}, IP: {ip}, 路径: {method} {path}");
    await next.Invoke();
});








app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


//

app.MapControllers();
app.Run();
