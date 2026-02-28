using MiniMes.Domain.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiniMES.Infastructure.interfaces
{
    public interface IItemService
    {
        Task<List<ItemDto>> GetAllItemsAsync();
        Task<bool> SaveItemAsync(ItemDto itemDto);
        Task<bool> DeleteItemAsync(string itemCode);

 
    }
}
