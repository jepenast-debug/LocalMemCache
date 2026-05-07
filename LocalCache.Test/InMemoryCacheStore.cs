using NUnit.Framework;
using LocalCache.Infrastructure;
using LocalCache.Domain.Cache;
using System.Text;

[TestFixture]
public class InMemoryCacheStoreTests {
    private InMemoryCacheStore _store;

    [SetUp]
    public void Setup () {
        // Inicializamos con límites de 256 caracteres para llave y 1MB para valor
        _store = new InMemoryCacheStore(256, 1048576);
    }

    [Test]
    public async Task SetAndGet_ValidData_ReturnsCorrectValue () {
        // Arrange
        string key = "testKey";
        byte[] value = Encoding.UTF8.GetBytes("testValue");

        // Act
        await _store.SetData("client1", key, value, null);
        var result = await _store.GetKey(key);

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public async Task GetKey_ExpiredItem_ReturnsNull () {
        // Arrange
        string key = "expiredKey";
        byte[] value = Encoding.UTF8.GetBytes("value");
        // TTL de 1 milisegundo para forzar expiración rápida
        await _store.SetData("client1", key, value, TimeSpan.FromMilliseconds(1));

        // Act
        await Task.Delay(10); // Esperar a que expire
        var result = await _store.GetKey(key);

        // Assert
        Assert.That(result, Is.Null);
    }
}