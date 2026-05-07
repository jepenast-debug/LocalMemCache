using LocalCache.Application;

[TestFixture]
public class QuotaManagerTests {
    private QuotaManager _quotaManager;

    [SetUp]
    public void Setup () {
        _quotaManager = new QuotaManager();
    }

    [Test]
    public void CheckMemory_OverLimit_ReturnsFalse () {
        // Arrange
        string clientId = "user1";
        // Simulamos un valor que excede los 10MB por defecto
        long largeSize = 11 * 1024 * 1024;

        // Act
        bool result = _quotaManager.CheckMemory(clientId, largeSize);

        // Assert
        Assert.That(result, Is.False);
    }
}