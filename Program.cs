using ClothingWebApp.Data;
using ClothingWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HTTP client for external API calls
builder.Services.AddHttpClient();

// Register StyleRecommendationService
builder.Services.AddScoped<StyleRecommendationService>(provider => {
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var apiKey = builder.Configuration["OpenAI:ApiKey"];
    
    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("WARNING: OpenAI API key is missing from configuration!");
        throw new InvalidOperationException("OpenAI API key is required but was not provided in the configuration.");
    }
    
    Console.WriteLine("OpenAI API key found in configuration.");
    return new StyleRecommendationService(context, apiKey);
});



// Session state for shopping cart
builder.Services.AddDistributedMemoryCache(); // In-memory cache for session state
builder.Services.AddSession(options => { // Configure session options
    options.IdleTimeout = TimeSpan.FromHours(2); // Set session timeout to 2 hours
    options.Cookie.HttpOnly = true; // Make session cookie HTTP-only for security
    options.Cookie.IsEssential = true; // Make session cookie essential
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme) // Use cookie authentication
    .AddCookie(options => {
        options.LoginPath = "/Account/Login"; // Redirect to login page if not authenticated
        options.LogoutPath = "/Account/Logout";// Redirect to logout page when logging out
    });


// MVC, here we connect the backend with the frontend controllers with views
builder.Services.AddControllersWithViews();


// Build the application
var app = builder.Build();

// Configure the HTTP request,Security middleware
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
app.UseSession();

// Configure routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Test OpenAI API connection on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var styleService = scope.ServiceProvider.GetRequiredService<StyleRecommendationService>();
        Console.WriteLine("Checking OpenAI API connection...");
        var apiConnected = await styleService.TestApiConnection();
        
        if (!apiConnected)
        {
            Console.WriteLine(" Could not connect to OpenAI API. The style assistant may not work correctly");
        }
        else
        {
            Console.WriteLine(" Successfully connected to OpenAI APi");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error checking API connection: {ex.Message}");
    }
}

// Initialize the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        logger.LogInformation("Database created successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        Console.WriteLine($"Error during database initialization: {ex.Message}");
    }
}

app.Run();