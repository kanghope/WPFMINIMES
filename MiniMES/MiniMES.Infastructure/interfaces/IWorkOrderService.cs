using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniMes.Domain.Commons; // WorkOrderStatus Enum 사용
using MiniMes.Domain.DTOs;

namespace MiniMes.Infrastructure.Interfaces
{
    public interface IWorkOrderService
    {
        Task<List<WorkOrderDto>> GetAllWorkOrdersAsync();
        Task CreateWorkOrder(WorkOrderDto dto);
        Task UpdateWorkOrder(WorkOrderDto dto);
        Task DeleteWorkOrder(int workOrderId);
        // --- 새로 추가된 메서드 ---
        Task UpdateWorkOrderStatus(int id, WorkOrderStatus newStatus);

    }

}