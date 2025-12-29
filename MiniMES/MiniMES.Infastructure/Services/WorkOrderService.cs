using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks; // 비동기(기다려주기) 기능을 위해 꼭 필요해요!
using MiniMes.Infrastructure.Data;

namespace MiniMes.Infrastructure.Services
{
    // 이 클래스는 작업지시(주문)를 DB에 넣고, 빼고, 고치는 '창고지기'입니다.
    public class WorkOrderService : IWorkOrderService
    {
        // ---------------------------------------------------------------------
        // 1. 모든 작업지시 목록 가져오기 (창고 뒤져서 다 보여주기)
        // ---------------------------------------------------------------------
        public async Task<List<WorkOrderDto>> GetAllWorkOrdersAsync()
        {
            // using: DB 연결 상자를 열고, 작업이 끝나면 자동으로 안전하게 닫습니다.
            using (var context = new MesDbContext())
            {
                // DB에서 필요한 정보들만 쏙쏙 골라내서 가져옵니다 (Select).
                // ToListAsync: "데이터가 많을 수 있으니 다 가져올 때까지 기다려줄게"라는 뜻입니다.
                var entities = await context.WorkOrders.Select(e => new
                {
                    e.WO_ID,
                    e.ITEM_CODE,
                    e.WO_QTY,
                    e.WO_STATUS,
                    e.WO_DATE,
                    e.UPDATED_AT,
                    e.CREATED_AT
                }).ToListAsync();

                // DB에서 꺼낸 원본 데이터(Entity)를 화면에 보여주기 좋은 형태(Dto)로 예쁘게 포장해서 돌려줍니다.
                return entities.Select(e => new WorkOrderDto
                {
                    Id = e.WO_ID,
                    ItemCode = e.ITEM_CODE,
                    Quantity = e.WO_QTY,
                    Status = e.WO_STATUS
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
    }
}