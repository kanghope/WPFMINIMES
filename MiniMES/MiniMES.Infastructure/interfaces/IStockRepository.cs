using MiniMes.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMES.Infastructure.interfaces
{
    public interface IStockRepository
    {
        Task<IEnumerable<StockDto>> GetStockListAsync(string searchText);
        Task UpdateStockInboundAsync(string itemCode, decimal qty);
    }
}
