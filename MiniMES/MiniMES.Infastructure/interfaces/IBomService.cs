using System.Collections.Generic;
using System.Threading.Tasks;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;

namespace MiniMES.Infastructure.interfaces
{
    public interface IBomService
    {
        Task<List<BomDto>> GetBomListByParentAsync(string parentCode);
        Task<bool> SaveBomAsync(BomEntity bomEntity);
        Task<bool> DeleteBomAsync(int bomId);
    }
}
