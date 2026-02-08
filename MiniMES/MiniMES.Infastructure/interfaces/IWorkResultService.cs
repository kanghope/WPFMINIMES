using MiniMes.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Infrastructure.Interfaces
{
    public interface IWorkResultService
    {
        /// <summary>새로운 작업 실적을 등록하고, WorkOrder의 Status를 업데이트합니다.</summary>
        Task RegisterWorkResult(WorkResultDto dto);

        /// <summary>특정 작업 지시에 대한 모든 실적을 조회합니다.</summary>
        Task<List<WorkResultDto>> GetResultsByWorkOrder(int workOrderId);
        // 필요하다면 Update/Delete 메서드도 추가
    }
}