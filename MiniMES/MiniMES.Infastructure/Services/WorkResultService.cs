using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMes.Infrastructure.Interfaces;// IWorkResultService 참조
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Transactions;

namespace MiniMes.Infrastructure.Services

{

    /// <summary>
    /// 실제 공장에서 발생한 생산 실적을 DB에 기록하는 역할을 합니다.
    /// 'IWorkResultService'라는 매뉴얼(인터페이스)을 따라 기능을 구현했습니다.
    /// </summary>
    public class WorkResultService : IWorkResultService

    {

        // [1. 다른 팀과의 협력] 
        // 실적을 등록하면 "작업 지시" 팀에게 "이 지시서는 이제 완료됐어!"라고 알려줘야 합니다.
        // 그래서 작업 지시 서비스(IWorkOrderService) 리모컨을 하나 가지고 있는 것입니다.
        private readonly IWorkOrderService _workOrderService;



        // [2. 생성자 1: 정석적인 방법 (DI 주입)]
        // 외부에서 "자, 이 리모컨(workOrderService) 써!"라고 건네주는 방식입니다.
        public WorkResultService(IWorkOrderService workOrderService)
        {
            // 리모컨이 비어있으면(null) 에러를 내고, 있으면 우리 팀 필드에 딱 보관합니다.
            _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        }



        // [3. 생성자 2: 간편한 방법 (기본 생성자)]
        // 리모컨을 대신 전해줄 사람이 없을 때, 내가 직접 'WorkOrderService'를 새로 하나 만듭니다.
        public WorkResultService()
        {
            // 직접 리모컨을 하나 새로 조립합니다.
            _workOrderService = new WorkOrderService();
        }



        // ************************************************************
        // 기능 1: 실적 등록하기 (Insert)
        // ************************************************************
        public async Task RegisterWorkResult(WorkResultDto dto)
        {
            using (var context = new MesDbContext())
            {
                // 1. TransactionScope를 사용하면 서로 다른 Service/Context라도 하나의 트랜잭션으로 묶어줍니다.
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        // ① 바구니(DTO)에 담긴 정보를 실제 DB 테이블용 종이(Entity)에 옮겨 적습니다.
                        var newResult = new WorkResultEntity
                        {
                            WO_ID = dto.WorkOrderId,      // 어떤 작업지시인가?
                            GOOD_QTY = dto.GoodQuantity,  // 양품 몇 개인가?
                            BAD_QTY = dto.BadQuantity,    // 불량 몇 개인가?
                            RESULT_DATE = DateTime.Now    // 지금 이 시간으로 기록!
                        };

                        // ② DB에 "이 실적 기록 추가해줘!"라고 요청하고 저장합니다.
                        context.WorkResults.Add(newResult);
                        context.SaveChanges();

                        // ③ [자동화] 실적이 들어왔으니, 해당 작업 지시는 이제 '완료(Complete)' 상태로 바꿉니다.
                        // 아까 보관해둔 작업 지시 팀 리모컨(_workOrderService)을 써서 상태만 쓱 고칩니다.
                        await _workOrderService.UpdateWorkOrderStatus(dto.WorkOrderId, WorkOrderStatus.Complete);

                        // [둘 다 성공하면 실제 반영!]
                        scope.Complete();
                    }
                    catch (Exception ex)
                    {
                        // 에러 발생 시 scope.Complete()가 호출되지 않으므로 자동 Rollback 됩니다.
                        throw new Exception("실적 등록 중 오류가 발생하여 모든 작업이 취소되었습니다.", ex);
                    }
                }
            }
        }




        // ************************************************************
        // 기능 2: 특정 작업에 대한 실적들만 쏙 뽑아보기 (Select)
        // ************************************************************
        public async Task<List<WorkResultDto>> GetResultsByWorkOrder(int workOrderId)
        {
            using (var context = new MesDbContext())
            {
                // ① DB에서 해당 작업지시 번호(WO_ID)와 일치하는 실적들만 찾습니다.
                // OrderByDescending: 최근 실적이 가장 위로 오게 정렬합니다.
                var results = context.WorkResults
                    .Where(r => r.WO_ID == workOrderId)
                    .OrderByDescending(r => r.RESULT_DATE)
                    .ToList();

                // ② DB용 종이(Entity) 뭉치를 다시 화면용 바구니(DTO) 리스트로 변환해서 돌려줍니다.
                return results.Select(r => new WorkResultDto
                {
                    ResultId = r.RESULT_ID,
                    WorkOrderId = r.WO_ID,
                    GoodQuantity = r.GOOD_QTY,
                    BadQuantity = r.BAD_QTY,
                    ResultDate = r.RESULT_DATE
                }).ToList();
            }
        }


    }

}