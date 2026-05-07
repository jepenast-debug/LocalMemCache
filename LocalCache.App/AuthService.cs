using LocalCache.Domain.Entities;
using LocalCache.Domain.Interfaces;
using LocalCache.Infrastructure.Security;

namespace LocalCache.Application;

public class AuthService (IDBRepository Repository) {
    private readonly IDBRepository Repo = Repository;

    public async Task<bool> Authenticate (string id, string Password) {
        try {
            ClientUser user = await Repo.GetById(id) ?? new ClientUser();
            if (user == null || !user.IsActive)
                return false;
            return StringHasher.Verify(Password, user.PwdHash);
        } catch {
            return false;
        }
    }

    public async Task<bool> ChangePassword (ClientUser User) {
        try {
            return await Repo.UpdatePwd(User);
        } catch {
            return false;
        }
    }
}