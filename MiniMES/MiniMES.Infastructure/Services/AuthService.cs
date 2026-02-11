using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Data;
using MiniMes.Domain.Entities;

namespace MiniMes.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        public async Task<UserEntity> AuthenticateAsync(string userId, string password)
        {
            using (var db = new MesDbContext())
            {
                // 실무 팁: password는 여기서 해시 암호화 비교를 수행하는 것이 좋습니다.
                return await db.Set<UserEntity>()
                               .FirstOrDefaultAsync(u => u.USER_ID == userId && u.USER_PW == password && u.IS_ACTIVE);
            }
        }
    }
}