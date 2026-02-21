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
    }
}
