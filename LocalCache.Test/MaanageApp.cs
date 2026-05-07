using NUnit.Framework;
using Moq;
using LocalCache.Domain.Interfaces;
using LocalCache.Domain.Entities;
using LocalMemCache.Core;
using LocalCache.Infrastructure.Persistence;

[TestFixture]
public class ManageAppTests {
    private Mock<IDBRepository> _dbRepoMock;
    private ManageApp _manageApp;
    private ManageCache _engine; // Se puede pasar real o mock según necesidad

    [SetUp]
    public void Setup () {
        _dbRepoMock = new Mock<IDBRepository>();
        // Mock simple de dependencias para inicializar ManageApp
        var settings = new LocalCache.Domain.General.Settings { PathLog = ".\\TestLog" };
        _engine = new ManageCache(settings);

        // Asumiendo una refactorización ligera para inyectar el Repo en lugar de instanciarlo
        _manageApp = new ManageApp((DBRepository)_dbRepoMock.Object, ref _engine);
    }

    [Test]
    public async Task AddUser_PasswordTooShort_ReturnsError () {
        // Arrange
        string[] cmdData = { "ADD_USER newUser short 1 Admin" };

        // Act
        // El método ExecuteCommand parsea los argumentos internamente
        var result = await _manageApp.ExecuteCommand("AdminUser", cmdData);

        // Assert
        Assert.That(result, Does.Contain("Paswword to short"));
    }

    [Test]
    public async Task DeleteUser_UserDoesNotExist_ReturnsNotFound () {
        // Arrange
        string userToDelete = "nonExistent";
        _dbRepoMock.Setup(r => r.GetById(userToDelete)).ReturnsAsync((ClientUser)null);

        // Act
        var result = await _manageApp.DeleteUser("AdminUser", userToDelete);

        // Assert
        Assert.That(result, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task ExecuteCommand_NonAdminUser_ReturnsNoAuthorized () {
        // Arrange
        string normalUser = "operator1";
        string[] cmdData = { "CLEAR_ALL" };

        // Simulamos que el usuario NO es Admin
        _dbRepoMock.Setup(r => r.GetById(normalUser))
                   .ReturnsAsync(new ClientUser { Id = normalUser, Profile = "Operator" });

        // Act
        var result = await _manageApp.ExecuteCommand(normalUser, cmdData);

        // Assert
        Assert.That(result, Is.EqualTo("ERROR No authorized"));
    }

    [Test]
    public async Task ChangeConfig_ValidKey_ReturnsOk () {
        // Arrange
        string admin = "admin";
        _dbRepoMock.Setup(r => r.GetById(admin))
                   .ReturnsAsync(new ClientUser { Id = admin, Profile = "Admin" });

        // Act
        // Intentamos cambiar el puerto en la configuración
        var result = await _manageApp.ChangeConfig("Port", "7000", admin);

        // Assert
        Assert.That(result, Is.EqualTo("OK"));
    }

}