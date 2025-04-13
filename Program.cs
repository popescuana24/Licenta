using ClothingWebApp.Data;
using ClothingWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add database context first
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register StyleRecommendationService BEFORE building the app
builder.Services.AddScoped<StyleRecommendationService>(provider => {
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("WARNING: OpenAI API key is missing from configuration!");
    }
    else
    {
        Console.WriteLine("OpenAI API key found in configuration.");
    }
    
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException("OpenAI API key is required but was not provided in the configuration.");
    }
    return new StyleRecommendationService(context, apiKey ?? throw new ArgumentNullException(nameof(apiKey), "API key cannot be null."));
});
// In Program.cs, add this before building the app
builder.Services.AddScoped<StyleRecommendationService>(provider => {
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("WARNING: OpenAI API key is missing from configuration!");
    }
    else
    {
        Console.WriteLine("OpenAI API key found in configuration.");
    }
    
    return new StyleRecommendationService(context, apiKey ?? throw new ArgumentNullException(nameof(apiKey), "API key cannot be null."));
});

// Add HTTP client
builder.Services.AddHttpClient();

// Add session state
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add controllers and views
builder.Services.AddControllersWithViews();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

// Build the app AFTER registering all services
var app = builder.Build();

// NOW you can test API connection
using (var scope = app.Services.CreateScope())
{
    try
    {
        var styleService = scope.ServiceProvider.GetRequiredService<StyleRecommendationService>();
        Console.WriteLine("Checking OpenAI API connection...");
        var apiConnected = await styleService.TestApiConnection();
        if (!apiConnected)
        {
            Console.WriteLine("⚠️ WARNING: Could not connect to OpenAI API. The style assistant may not work correctly.");
        }
        else
        {
            Console.WriteLine("✅ Successfully connected to OpenAI API.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking API connection: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Enable session before MVC
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Rest of your code...
// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        // Seed categories
        await DataSeeder.SeedCategoriesAsync(context);
        
        // Import products from CSV
        string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "products.csv");
        
        // IMPORTANT: Actually import the products!
        await DataSeeder.ImportProductsFromCsvAsync(context, csvPath);
        
        // Only add sample products if CSV import failed
        if (!await context.Products.AnyAsync())
        {
            Console.WriteLine("CSV import didn't add any products, adding samples instead");
            await DataSeeder.AddSampleProductsDirectly(context);
        }
        
        // Verify database state
        await DataSeeder.VerifyDatabaseState(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        Console.WriteLine($"Error during database initialization: {ex.Message}");
    }
}

app.Run();