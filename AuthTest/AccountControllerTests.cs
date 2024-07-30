using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using IdentityService.Data;
using IdentityService.Models.UserModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthTest;

public class AccountControllerTests
{
    private ServiceProvider _serviceProvider;
    private UserManager<ApplicationUser> _userManager;
    private SignInManager<ApplicationUser> _signInManager;
    private IConfiguration _configuration;
    private ApplicationDbContext _context;
    
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
            
        // InMemory Database
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName: "InMemoryDb"));

        // Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Mock ILogger for UserManager and SignInManager
        services.AddSingleton(Mock.Of<ILogger<UserManager<ApplicationUser>>>());
        services.AddSingleton(Mock.Of<ILogger<SignInManager<ApplicationUser>>>());
        services.AddSingleton(Mock.Of<ILogger<DataProtectorTokenProvider<ApplicationUser>>>());
        services.AddSingleton(Mock.Of<ILogger<RoleManager<IdentityRole>>>());
        // Mock ITokenProvider
        var tokenProviderMock = new Mock<IUserTwoFactorTokenProvider<ApplicationUser>>();
        services.AddSingleton(tokenProviderMock.Object);
        
        // Configuration
        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "79dfc0e0df8e4ff68ffee980cbe59f75"},
            {"Jwt:Issuer", "Identity"},
            {"Jwt:Audience", "users"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        _serviceProvider = services.BuildServiceProvider();

        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        _configuration = _serviceProvider.GetRequiredService<IConfiguration>();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
    }
    [Test]
    public async Task Register_Should_Create_User()
    {
        // Arrange
        var accountController = new AccountController(_userManager, _signInManager, _configuration);
        var registerUser = new RegisterUser()
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "Test@1234"
        };

        // Act
        var result = await accountController.Register(registerUser);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
    [Test]
    public async Task Login_Should_Return_Token()
    {
        // Arrange
        var accountController = new AccountController(_userManager, _signInManager, _configuration);
        var registerUser = new RegisterUser()
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "Test@1234"
        };

        await accountController.Register(registerUser);

        var loginModel = new LoginUser
        {
            Username = "testuser",
            Password = "Test@1234"
        };

        // Act
        var result = await accountController.Login(loginModel);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        
    }
    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}