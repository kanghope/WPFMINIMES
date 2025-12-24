using MiniMes.Domain.Commons;// WorkOrderStatus Enum 사용
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniMes.Infrastructure.Data;

namespace MiniMes.Infrastructure.Services

{

    public class WorkOrderService : IWorkOrderService

    {

        // 1. 전체 작업 지시 목록 조회

        public async Task<List<WorkOrderDto>> GetAllWorkOrdersAsync()

        {

            using (var context = new MesDbContext())

            {

                // LINQ를 사용하여 DB 쿼리

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



                // Entity 목록을 DTO 목록으로 변환 (실무에서는 AutoMapper 사용 권장)

                return entities.Select(e => new WorkOrderDto

                {

                    Id = e.WO_ID,

                    ItemCode = e.ITEM_CODE,

                    Quantity = e.WO_QTY,

                    Status = e.WO_STATUS

                }).ToList();

            }

        }



        // 2. 새로운 작업 지시 등록

        public void CreateWorkOrder(WorkOrderDto dto)

        {

            using (var context = new MesDbContext())

            {

                var now = DateTime.Now;

                // DTO를 Entity로 변환

                var newEntity = new WorkOrderEntity

                {

                    ITEM_CODE = dto.ItemCode,

                    //WO_DATE = DateTime.Now,

                    WO_QTY = dto.Quantity,

                    WO_STATUS = WorkOrderStatusExtensions.ToDbCode(WorkOrderStatus.Wait), // 초기 상태: 대기



                    // DB의 NOT NULL 컬럼 처리

                    WO_DATE = now.Date, // DB 스키마가 DATE이므로 날짜만 저장

                    //CREATED_AT = now   // 생성 시간 설정

                    UPDATED_AT = now    // 초기 수정 시간 설정



                };



                context.WorkOrders.Add(newEntity);

                context.SaveChanges(); // DB에 반영

            }

        }



        // ---------------------------------------------------------------------

        // 3. 작업 지시 수정

        // ---------------------------------------------------------------------

        public void UpdateWorkOrder(WorkOrderDto dto)

        {

            using (var context = new MesDbContext())

            {

                var entity = context.WorkOrders.SingleOrDefault(e => e.WO_ID == dto.Id);

                if (entity != null)

                {

                    // DTO의 변경된 값을 Entity에 반영

                    entity.ITEM_CODE = dto.ItemCode;

                    entity.WO_QTY = dto.Quantity;

                    // 수정 시간 갱신 (반드시 필요)

                    entity.UPDATED_AT = DateTime.Now;



                    context.SaveChanges();//db에 반영

                }

            }

        }



        // ---------------------------------------------------------------------

        // 4. 작업 지시 삭제

        // ---------------------------------------------------------------------

        public void DeleteWorkOrder(int workOrderId)

        {

            using (var context = new MesDbContext())

            {

                var entity = context.WorkOrders.SingleOrDefault(e => e.WO_ID == workOrderId);

                if (entity != null)

                {

                    // 삭제 전 관련 실적(WorkResult)도 삭제해야 DB 무결성이 유지됩니다.

                    //context.WorkResults.RemoveRange(context.WorkResults.Where(r => r.WO_ID == workOrderId));



                    context.WorkOrders.Remove(entity);

                    context.SaveChanges();

                }

            }

        }



        // --- 5. 작업 상태 업데이트 메서드 구현 (신규) ---

        public void UpdateWorkOrderStatus(int id, WorkOrderStatus newStatus)

        {

            using (var context = new MesDbContext())

            {

                var entity = context.WorkOrders.SingleOrDefault(e => e.WO_ID == id);



                if (entity == null)

                {

                    // ID에 해당하는 작업 지시가 없으면 처리 중단

                    return;

                }



                // Enum을 DB 코드(예: "W", "P", "C")로 변환하여 할당

                entity.WO_STATUS = newStatus.ToDbCode();



                // 업데이트 시점 기록

                entity.UPDATED_AT = DateTime.Now;



                context.SaveChanges();

            }

        }

    }

}