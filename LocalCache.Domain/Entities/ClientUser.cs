namespace LocalCache.Domain.Entities;

public class ClientUser {
    public string Id { get; set; } = "";
    public string PwdHash { get; set; } = "";
    public bool IsActive { get; set; }

    public string? Profile { get; set; }
}