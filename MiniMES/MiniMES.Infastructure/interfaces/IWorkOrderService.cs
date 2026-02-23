using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiniMes.Domain.Commons; // '대기, 진행, 완료' 같은 상태 약속을 가져옵니다.
using MiniMes.Domain.DTOs;    // 데이터를 담아 나를 바구니(DTO)를 가져옵니다.

namespace MiniMes.Infrastructure.Interfaces
{
    /// <summary>
    /// 작업 지시(WorkOrder)를 관리하기 위해 꼭 필요한 기능 목록입니다.
    /// 'I'로 시작하는 것은 인터페이스(Interface)라는 뜻으로, "무엇을 할지"만 정의합니다.
    /// </summary>
    public interface IWorkOrderService
    {
        // [1. 전체 목록 가져오기]
        // DB에 있는 모든 작업 지시서를 리스트 형태로 가져옵니다. 
        // Task<>는 비동기 작업(작업 중에도 화면이 멈추지 않음)을 의미합니다.
        // 파라미터 개수, 타입, 기본값이 클래스 구현부와 반드시 일치해야 합니다.
        Task<List<WorkOrderDto>> GetAllWorkOrdersAsync(
            string strWoStatus,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string searchText = null);

        // [2. 새 작업 등록하기]
        // 화면에서 입력한 정보를 바구니(dto)에 담아 전달하면 DB에 저장합니다.
        Task CreateWorkOrder(WorkOrderDto dto);

        // [3. 정보 수정하기]
        // 수량이나 품목이 바뀌었을 때, 바구니(dto)를 보고 내용을 업데이트합니다.
        Task UpdateWorkOrder(WorkOrderDto dto);

        // [4. 삭제하기]
        // 특정 번호(id)를 알려주면 그 작업 지시를 DB에서 지웁니다.
        Task DeleteWorkOrder(int workOrderId);

        // --- [5. 상태만 쓱 바꾸기] ---
        // 전체 내용을 고치는 게 아니라, '대기'에서 '진행중'으로 상태값만 딱 바꿀 때 사용합니다.
        Task UpdateWorkOrderStatus(int id, WorkOrderStatus newStatus);
    }
}