using Dapper;
using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient; // SQL Client
using System.Linq;
using System.Threading.Tasks;

namespace MiniMES.Infastructure.Services
{
    public class WorkResultServicePro : IWorkResultService
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["MesConnection"].ConnectionString;

        // 1. [활성화] IWorkOrderService 인터페이스 필드 선언
        private readonly IWorkOrderService _workOrderService;

        // [활성화] 생성자: DI 환경에서 IWorkOrderService를 주입받도록 구현
        public WorkResultServicePro(IWorkOrderService workOrderService)
        {
            _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        }

        // ************************************************************
        // 1. 실적 등록 및 상태 변경 (Dapper + 트랜잭션)
        // ************************************************************
        public async Task RegisterWorkResult(WorkResultDto dto)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        // ① 실적 테이블 저장 (프로시저 호출)
                        var resultParams = new
                        {
                            WorkOrderId = dto.WorkOrderId,
                            GoodQty = dto.GoodQuantity,
                            BadQty = dto.BadQuantity
                        };

                        await db.ExecuteAsync("SP_RegisterWorkResult",
                                                resultParams,
                                                transaction: transaction,
                                                commandType: CommandType.StoredProcedure);
                        // ② 작업 지시 상태 변경 (이미 만들어둔 WorkOrderService 호출)
                        // 주의: 서비스 간 트랜잭션을 공유하려면 Connection을 넘겨주는 구조가 좋지만, 
                        // 여기서는 로직 분리를 위해 분리 호출합니다.
                        await _workOrderService.UpdateWorkOrderStatus(dto.WorkOrderId, WorkOrderStatus.Complete);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        // ************************************************************
        // 2. 특정 작업 지시에 대한 실적 조회 (Dapper 프로시저)
        // ************************************************************
        public async Task<List<WorkResultDto>> GetResultsByWorkOrder(int workOrderId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                // QueryAsync를 사용하면 DTO 속성과 프로시저 결과 컬럼명이 같을 때 자동 매핑됩니다.
                var results = await db.QueryAsync<WorkResultDto>(
                    "SP_GetResultsByWorkOrder",
                    new { WorkOrderId = workOrderId },
                    commandType: CommandType.StoredProcedure
                );

                return results.ToList();
            }
        }
    }
}
