

[TestFixture]
public class RespParserTests {
    [TestCase("SET mykey myvalue 60", "SET", "mykey", "myvalue", "60")]
    [TestCase("GET somekey", "GET", "somekey")]
    [TestCase("AUTH user pass123", "AUTH", "user", "pass123")]
    public void ParseTextToArgs_ValidInput_ReturnsExpectedParts (string input, params string[] expected) {
        // Act
        var result = LocalCache.Server.Protocol.RespParser.ParseTextToArgs(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}