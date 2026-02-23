using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks; // 비동기(기다려주기) 기능을 위해 꼭 필요해요!

namespace MiniMes.Infrastructure.Services
{
    // 이 클래스는 작업지시(주문)를 DB에 넣고, 빼고, 고치는 '창고지기'입니다.
    public class WorkOrderService : IWorkOrderService
    {
        private readonly IWorkOrderRepository _workOrderRepository;

        // 생성자 주입 (DI) 설정
        public WorkOrderService(IWorkOrderRepository workOrderRepository)
        {
            _workOrderRepository = workOrderRepository;
        }

        // 기본 생성자 (필요 시 보존)
        public WorkOrderService() { }

        // ---------------------------------------------------------------------
        // 1. 모든 작업지시 목록 가져오기 (창고 뒤져서 다 보여주기)
        // ---------------------------------------------------------------------
        public async Task<List<WorkOrderDto>> GetAllWorkOrdersAsync(string strWoStatus, DateTime? startDate = null, DateTime? endDate = null, string searchText = null)
        {
            // using: DB 연결 상자를 열고, 작업이 끝나면 자동으로 안전하게 닫습니다.
            using (var context = new MesDbContext())
            {
                // DB에서 필요한 정보들만 쏙쏙 골라내서 가져옵니다 (Select).
                // ToListAsync: "데이터가 많을 수 있으니 다 가져올 때까지 기다려줄게"라는 뜻입니다.
                // 1. 일단 쿼리의 시작점을 만듭니다 (아직 DB로 명령이 전달되지 않음)
                var strQuery = context.WorkOrders.AsQueryable();

                if(strWoStatus != "ALL")
                {
                    strQuery = strQuery.Where(e => e.WO_STATUS == strWoStatus);
                }
                else
                {
                    // "전체(ALL)"인 경우: 'P'(진행)와 'W'(대기) 상태인 것만 포함
                    // SQL의 WHERE WO_STATUS IN ('P', 'W') 와 같은 역할을 합니다.
                    //var targetStatuses = new[] { "P", "W" }
;                   //strQuery = strQuery.Where(e => targetStatuse"s.Contains(e.WO_STATUS));
                    strQuery = strQuery.Where(e => e.WO_STATUS == "P" || e.WO_STATUS == "W");
                }

                // 3. [추가] 기간 조회 (WO_DATE 기준)
                // 시작일의 00:00:00부터 종료일의 23:59:59까지 포함하도록 날짜 처리가 중요합니다.
                if (startDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    strQuery = strQuery.Where(e => e.WO_DATE >= start);
                }

                if (endDate.HasValue)
                {
                    var end = endDate.Value.Date.AddDays(1).AddTicks(-1); // 해당 날짜의 끝 시간까지 포함
                    strQuery = strQuery.Where(e => e.WO_DATE <= end);
                }

                // 4. [추가] 검색어 필터링 (품목코드나 지시 ID)
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // 품목코드나 다른 필드에 검색어가 포함(Contains)되어 있는지 검사
                    strQuery = strQuery.Where(e => e.ITEM_CODE.Contains(searchText) || e.WO_ID.ToString().Contains(searchText));
                }

                // 5. [성능 최적화] 최신순 정렬 후 필요한 데이터만 익명 객체로 가져오기
                var entities = await strQuery
                    .OrderByDescending(e => e.WO_DATE) // 최근 지시가 위로 오게 정렬
                    .Select(e => new
                    {
                        e.WO_ID,
                        e.ITEM_CODE,
                        e.WO_QTY,
                        e.WO_STATUS,
                        e.WO_DATE,
                        e.COMPLETE_QTY
                    }).ToListAsync();

                // DB에서 꺼낸 원본 데이터(Entity)를 화면에 보여주기 좋은 형태(Dto)로 예쁘게 포장해서 돌려줍니다.
                return entities.Select(e => new WorkOrderDto
                {
                    Id = e.WO_ID,
                    ItemCode = e.ITEM_CODE,
                    Quantity = e.WO_QTY,
                    Status = e.WO_STATUS,
                    WoDate = e.WO_DATE,
                    // e.COMPLETE_QTY가 null이면 0을 넣고, 아니면 int로 변환
                    /*
                     HasValue의 역할
                     true: 상자 안에 값이 들어있음 (꺼내서 써도 안전함)
                     false: 상자가 비어있음 (null 상태, 억지로 꺼내려 하면 에러 발생)*/
                    CompleteQty = e.COMPLETE_QTY.HasValue? (int)e.COMPLETE_QTY : 0,
                }).ToList();
            }
        }

        // ---------------------------------------------------------------------
        // 2. 새 작업지시 등록 (새 주문서 작성해서 창고에 넣기)
        // ---------------------------------------------------------------------
        public async Task CreateWorkOrder(WorkOrderDto dto)
        {
            using (var context = new MesDbContext())
            {
                var now = DateTime.Now; // 현재 시간 기록

                // 사용자가 입력한 내용(dto)을 DB 전용 종이(Entity)에 옮겨 적습니다.
                var newEntity = new WorkOrderEntity
                {
                    ITEM_CODE = dto.ItemCode,
                    WO_QTY = dto.Quantity,
                    // 처음 등록하면 무조건 '대기(Wait)' 상태로 시작합니다.
                    WO_STATUS = WorkOrderStatusExtensions.ToDbCode(WorkOrderStatus.Wait),
                    WO_DATE = now.Date,
                    UPDATED_AT = now
                };

                // 창고에 새 종이를 집어넣고...
                context.WorkOrders.Add(newEntity);
                // "진짜로 저장해!"라고 확정을 짓습니다. (이때 DB에 실제로 들어갑니다)
                await context.SaveChangesAsync();
            }
        }

        // ---------------------------------------------------------------------
        // 3. 작업지시 수정 (이미 있는 주문서 내용 고치기)
        // ---------------------------------------------------------------------
        public async Task UpdateWorkOrder(WorkOrderDto dto)
        {
            using (var context = new MesDbContext())
            {
                // 수정할 주문서 번호(Id)를 가지고 창고에서 딱 하나를 찾아냅니다.
                var entity = await context.WorkOrders.SingleOrDefaultAsync(e => e.WO_ID == dto.Id);

                if (entity != null) // 주문서를 찾았다면?
                {
                    // 새로운 내용으로 덮어씁니다.
                    entity.ITEM_CODE = dto.ItemCode;
                    entity.WO_QTY = dto.Quantity;
                    entity.UPDATED_AT = DateTime.Now; // 언제 고쳤는지 기록!

                    // 고친 내용을 확정 저장합니다.
                    await context.SaveChangesAsync();
                }
            }
        }

        // ---------------------------------------------------------------------
        // 4. 작업지시 삭제 (창고에서 주문서 찢어버리기)
        // ---------------------------------------------------------------------
        public async Task DeleteWorkOrder(int workOrderId)
        {
            using (var context = new MesDbContext())
            {
                // 삭제할 주문서를 번호로 찾습니다.
                var entity = await context.WorkOrders.SingleOrDefaultAsync(e => e.WO_ID == workOrderId);

                if (entity != null)
                {
                    // 목록에서 제거하고...
                    context.WorkOrders.Remove(entity);
                    // DB에서도 완전히 지웁니다.
                    await context.SaveChangesAsync();
                }
            }
        }

        // ---------------------------------------------------------------------
        // 5. 상태만 바꾸기 (예: '대기'중인 주문을 '요리중'으로 바꾸기)
        // ---------------------------------------------------------------------
        public async Task UpdateWorkOrderStatus(int id, WorkOrderStatus newStatus)
        {
            using (var context = new MesDbContext())
            {
                // 번호로 주문서를 찾아서...
                var entity = await context.WorkOrders.SingleOrDefaultAsync(e => e.WO_ID == id);

                if (entity != null)
                {
                    // 상태 정보만 새로 갈아 끼웁니다. (ToDbCode는 Enum을 "W" 같은 문자로 바꿔줍니다)
                    entity.WO_STATUS = newStatus.ToDbCode();
                    entity.UPDATED_AT = DateTime.Now;

                    // 변경 사항을 저장합니다.
                    await context.SaveChangesAsync();
                }
            }
        }

        // ---------------------------------------------------------------------
        // 신규 기능: 작업 시작 (StartWorkOrder)
        // ---------------------------------------------------------------------
        public async Task StartWorkOrderAsync(int woId, string userId, string eqCode)
        {
            
            // [신규 방식] Repository를 통해 SP_StartWorkOrder 호출 (설비 연동 및 트랜잭션 포함)
            await _workOrderRepository.StartWorkOrder(woId, userId, eqCode);
        }

        // ---------------------------------------------------------------------
        // 신규 기능: 작업 종료 (StopWorkOrder)
        // ---------------------------------------------------------------------
        public async Task StopWorkOrderAsync(int woId, string userId)
        {
            /* [기존 방식 - 주석 처리]
            using (var context = new MesDbContext()) {
                var entity = await context.WorkOrders.SingleOrDefaultAsync(e => e.WO_ID == woId);
                if (entity != null) {
                    entity.WO_STATUS = "C";
                    await context.SaveChangesAsync();
                }
            }
            */

            // [신규 방식] Repository를 통해 SP_StopWorkOrder 호출 (최종 실적 정산 및 상태 마감)
            await _workOrderRepository.StopWorkOrder(woId, userId);
        }
    }
}