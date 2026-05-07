using LocalCache.Domain.Entities;

namespace LocalCache.Domain.Interfaces;

public interface IDBRepository {
    Task<ClientUser?> GetById (string id);
    Task AddUser (ClientUser User);
    Task Activate (string id);
    Task Deactivate (string id);
    Task<bool> UpdatePwd (ClientUser User);
    Task<bool> DeleteUser (string Id);
}