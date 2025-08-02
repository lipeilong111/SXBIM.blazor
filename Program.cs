using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddScoped<HttpClient>(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});


builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<SXBIM_Login.Services.LoginService>();
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

var app = builder.Build();
app.UseHttpsRedirection();

app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
#if DEBUG
        context.Response.Redirect("/download");
        return;
#else
        context.Response.Redirect("/login");
        return;
#endif
    }

    await next();
});


var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".dll"] = "application/octet-stream";
provider.Mappings[".gha"] = "application/octet-stream";
provider.Mappings[".rhp"] = "application/octet-stream";
provider.Mappings[".7z"] = "application/octet-stream";

// 添加静态文件中间件
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});




app.UseRouting();


//自定义中间件，记录每个请求
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString();
    var method = context.Request.Method;
    if (path == "/" && method == "GET") // 只记录首页 GET 请求
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[访问日志] 时间: {time}, IP: {ip}, 路径: {method} {path}");
    }
    await next.Invoke();
});








app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


app.MapControllers();
app.Run();
