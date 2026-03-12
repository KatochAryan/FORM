using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// EPPlus license (works for EPPlus 6 & 7)
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
builder.Services.AddControllersWithViews();

builder.Services.AddSession();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=StudentErp}/{action=Login}/{id?}"
);

app.Run();
