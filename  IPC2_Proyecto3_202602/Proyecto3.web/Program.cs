var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient("Proyecto3API", cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5149/");
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
// Mantener HTTP en local evita redirecciones HTTPS sin puerto configurado durante desarrollo.
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
