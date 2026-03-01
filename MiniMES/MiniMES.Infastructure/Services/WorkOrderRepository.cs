using Dapper;
using Microsoft.Data.SqlClient;
using MiniMes.Domain.Commons;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using System.Configuration;
using System.Data;
using System.IO.Ports;

namespace MiniMES.Infastructure.Services
{
    public class WorkOrderRepository : IWorkOrderRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["MesConnection"].ConnectionString;
        /*
        // 1. [활성화] IWorkOrderService 인터페이스 필드 선언
        private readonly IWorkOrderRepository _WorkOrderRepository;

        // [활성화] 생성자: DI 환경에서 IWorkOrderService를 주입받도록 구현
        public WorkOrderRepository(IWorkOrderRepository WorkOrderRepository)
        {
            _WorkOrderRepository = WorkOrderRepository ?? throw new ArgumentNullException(nameof(WorkOrderRepository));
        }
        */
        // Repository는 DB와 직접 통신하므로 별도의 주입 없이 Connection String을 사용합니다.
        public WorkOrderRepository() { }

        // 작업 시작 프로시저 호출 (새로 생성 필요)
        public async Task StartWorkOrder(int woId, string userId, string eqCode)
        {

            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                // Dapper는 Open을 명시적으로 해주는 것이 좋습니다 (Transaction 사용 시 필수)
                if (db.State == ConnectionState.Closed) db.Open();

                try
                {
                    // 1. 작업지시 정보 조회 (Dapper 방식)
                    // SQL에서 직접 조회하여 ITEM_CODE와 WO_QTY를 가져옵니다.
                    //var workOrder = await db.QueryFirstOrDefaultAsync("SP_GetWorkOrderInfo", new { WO_ID = woId });

                    var workOrder = await db.QueryFirstOrDefaultAsync(
                                    "QueryFirstOrDefaultAsync",
                                    new { WO_ID = woId },
                                    commandType: CommandType.StoredProcedure
                         );
                  
                    if (workOrder == null)
                        throw new Exception("작업지시를 찾을 수 없습니다.");

                    // 2. BOM 및 재고 정보 확인 (기존 구현된 SP 활용)
                    // 동일한 클래스 내의 메소드를 호출할 때는 별도의 필드 주입 없이 바로 호출합니다.
                    var bomRequirements = await GetBomRequirementAsync(workOrder.ITEM_CODE);

                    foreach (var item in bomRequirements)
                    {
                        // SP_GetBomRequirement의 결과 컬럼명: CHILD_ITEM, CONSUMPTION, CURRENT_QTY
                        decimal requiredQty = (decimal)item.CONSUMPTION * (decimal)workOrder.WO_QTY;
                        decimal currentStock = (decimal)(item.CURRENT_QTY ?? 0m);

                        if (currentStock < requiredQty)
                        {
                            throw new Exception($"원재료 재고가 부족합니다.\n품목: {item.CHILD_ITEM}\n필요: {requiredQty}, 현재: {currentStock}");
                        }
                    }

                    // 3. 재고가 충분하면 SP_StartWorkOrder 호출
                    var param = new { WO_ID = woId, USER_ID = userId, EQ_CODE = eqCode };

                    await db.ExecuteAsync("SP_StartWorkOrder",
                                          param,
                                          commandType: CommandType.StoredProcedure);
                }
                catch (Exception ex)
                {
                    // 예외를 상위(ViewModel)로 던져서 사용자에게 알림창을 띄울 수 있게 합니다.
                    throw new Exception(ex.Message, ex);
                }
            }
        }

        // 작업 종료 프로시저 호출 (새로 생성 필요)
        public async Task StopWorkOrder(int woId, string userId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                //db.Open();
                //using (var transaction = db.BeginTransaction())
                //{
                    try
                    {
                        var param = new { WO_ID = woId, USER_ID = userId };

                        await db.ExecuteAsync("SP_StopWorkOrder",
                                                param,
                                                //transaction: transaction,
                                                commandType: CommandType.StoredProcedure);

                        // [핵심] 성공적으로 실행되었다면 반드시 Commit을 호출해야 DB에 반영됩니다.
                        //transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                    //transaction.Rollback();
                    throw new Exception($"작업 종료 DB 반영 실패 (ID: {woId})", ex);
                }
                //}
            }

        }

        // PLC 실적 처리 프로시저 호출 (기존 생성한 SP_ProcessDeviceProduction)
        public async Task<string> ProcessProduction(string eqCode, long logId, int goodQty, int badQty, string userId)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                //db.Open();
                //using (var transaction = db.BeginTransaction())
                //{
                    try
                    {
                        var param = new {
                            EqCode = eqCode,
                            RawLogId = logId,
                            GoodQty = goodQty,
                            BadQty = badQty,
                            WorkerId = userId
                        };

                        /*
                        await db.ExecuteAsync("SP_ProcessDeviceProduction",
                                  param,
                                  commandType: CommandType.StoredProcedure);*/
                        var currentStatus = await db.QueryFirstOrDefaultAsync<string>(
                            "SP_ProcessDeviceProduction",
                            param,
                            commandType: CommandType.StoredProcedure
                        );
                        // [핵심] 성공적으로 실행되었다면 반드시 Commit을 호출해야 DB에 반영됩니다.
                        //transaction.Commit();
                        return currentStatus;
                    }
                    catch (Exception ex)
                    {
                        // 상위 SerialDeviceService에서 로깅할 수 있도록 예외 전달
                        throw new Exception($"실적 처리 중 오류 발생 (설비: {eqCode}): {ex.Message}", ex);
                    }
                }
           //}
           
        }

        // WorkOrderRepository 구현부
        public async Task<IEnumerable<dynamic>> GetBomRequirementAsync(string itemCode)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                try
                {
                    var param = new { ItemCode = itemCode };

                    var result = await db.QueryAsync("SP_GetBomRequirement",
                                            param,
                                            //transaction: transaction,
                                            commandType: CommandType.StoredProcedure);

                    return result;
                }
                catch (Exception ex)
                {
                    //transaction.Rollback();
                    throw new Exception($"조회 오류 (ID: {itemCode})", ex);
                }
               
            }
        }
    }
}
