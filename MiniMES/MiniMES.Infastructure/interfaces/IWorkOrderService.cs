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

        void CreateWorkOrder(WorkOrderDto dto);

        void UpdateWorkOrder(WorkOrderDto dto);

        void DeleteWorkOrder(int workOrderId);



        // --- 새로 추가된 메서드 ---

        void UpdateWorkOrderStatus(int id, WorkOrderStatus newStatus);

    }

}