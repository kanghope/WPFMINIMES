using System.Threading.Tasks;
using MiniMes.Domain.Entities;

namespace MiniMes.Infrastructure.Interfaces
{
    public interface IAuthService
    {
        // 로그인 검증 및 사용자 정보 반환
        Task<UserEntity> AuthenticateAsync(string userId, string password);
    }
}