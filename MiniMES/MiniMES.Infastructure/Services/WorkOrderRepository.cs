using Dapper;
using MiniMes.Domain.Commons;
using MiniMes.Infrastructure.Interfaces;
using MiniMes.Infrastructure.Services;
using MiniMES.Infastructure.interfaces;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
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
                //db.Open();
                //using (var transaction = db.BeginTransaction())
                //{
                    try
                    {
                        var param = new { WO_ID = woId, USER_ID = userId, EQ_CODE = eqCode };

                        await db.ExecuteAsync("SP_StartWorkOrder",
                                                param,
                                               //transaction: transaction,
                                                commandType: CommandType.StoredProcedure);

                        // [핵심] 성공적으로 실행되었다면 반드시 Commit을 호출해야 DB에 반영됩니다.
                        //transaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        //transaction.Rollback();
                        // 3. 로그만 남기고 상위(ViewModel)로 던짐
                        throw new Exception($"작업 시작 DB 반영 실패 (ID: {woId})", ex);
                    }
                //}
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
    }
}
