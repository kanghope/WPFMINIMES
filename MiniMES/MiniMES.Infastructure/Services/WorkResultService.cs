using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMes.Infrastructure.Interfaces;// IWorkResultService 참조
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniMes.Infrastructure.Services

{

    // WorkOrderService를 DI 받기 위해 생성자 의존성 주입을 사용합니다.

    // 현재 WorkOrderService는 인터페이스를 사용하지 않으므로 구체 클래스를 참조합니다.

    public class WorkResultService : IWorkResultService

    {

        // 1. [활성화] IWorkOrderService 인터페이스 필드 선언

        private readonly IWorkOrderService _workOrderService;



        // -------------------------------------------------------------

        // [활성화] 생성자: DI 환경에서 IWorkOrderService를 주입받도록 구현

        // -------------------------------------------------------------

        public WorkResultService(IWorkOrderService workOrderService)

        {

            // 주입받은 인터페이스 인스턴스를 필드에 할당합니다.

            // (null 검사는 필수입니다.)

            if (workOrderService == null)

            {

                throw new ArgumentNullException(nameof(workOrderService));

            }

            else

            {

                _workOrderService = workOrderService;

            }



        }



        // -------------------------------------------------------------------

        // 2. [추가] 간소화된 사용을 위한 인수 없는 생성자

        // 이 생성자는 DI 컨테이너 없이 직접 'new'할 때 사용됩니다.

        // **경고:** 여기서 _workOrderService를 직접 초기화해야 합니다.

        public WorkResultService()

        {

            // WorkOrderService의 구체 클래스(구현체)를 직접 인스턴스화하여 할당합니다.

            // 이 WorkOrderService도 DI가 필요하다면 이 방식은 무한 순환을 일으킬 수 있습니다!

            // 여기서는 WorkOrderService가 인수 없는 생성자를 가지고 있다고 가정합니다.

            _workOrderService = new WorkOrderService();

        }

        // -------------------------------------------------------------------



        // ************************************************

        // 1. 실적 등록 및 작업 지시 상태 완료 처리

        // ************************************************

        public void RegisterWorkResult(WorkResultDto dto)

        {

            using (var context = new MesDbContext())

            {

                // 1. DTO를 Entity로 변환

                var newResult = new WorkResultEntity

                {

                    WO_ID = dto.WorkOrderId,

                    GOOD_QTY = dto.GoodQuantity,

                    BAD_QTY = dto.BadQuantity,

                    RESULT_DATE = DateTime.Now // 실적 등록 시간 기록

                };



                // 2. 실적 저장

                context.WorkResults.Add(newResult);

                context.SaveChanges();

            }



            // 3. 작업 지시 상태를 '완료'로 업데이트 (WorkOrderService 사용)

            // 실적 등록 후, 해당 작업 지시는 완료(Complete) 상태로 변경되어야 합니다.

            _workOrderService.UpdateWorkOrderStatus(dto.WorkOrderId, WorkOrderStatus.Complete);

        }



        // ************************************************

        // 2. 특정 작업 지시에 대한 실적 조회

        // ************************************************

        public List<WorkResultDto> GetResultsByWorkOrder(int workOrderId)

        {

            using (var context = new MesDbContext())

            {

                var results = context.WorkResults

                    .Where(r => r.WO_ID == workOrderId)

                    .OrderByDescending(r => r.RESULT_DATE)

                    .ToList();



                // Entity를 DTO로 변환

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