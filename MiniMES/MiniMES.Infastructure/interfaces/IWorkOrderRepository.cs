using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMES.Infastructure.interfaces
{
    public interface IWorkOrderRepository
    {
        Task StartWorkOrder(int woId, string userId, string eqCode);
        Task StopWorkOrder(int woId, string userId);
        Task<string> ProcessProduction(string eqCode, long logId, int goodQty, int badQty, string userId);

        Task<IEnumerable<dynamic>> GetBomRequirementAsync(string itemCode);//BOM 정보를 화면에 보여주거나 생산 전 체크하기 위해
    }
}
