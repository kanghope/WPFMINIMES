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

        // 1. [활성화] IWorkOrderService 인터페이스 필드 선언
        private readonly IWorkOrderRepository _WorkOrderRepository;

        // [활성화] 생성자: DI 환경에서 IWorkOrderService를 주입받도록 구현
        public WorkOrderRepository(IWorkOrderRepository WorkOrderRepository)
        {
            _WorkOrderRepository = WorkOrderRepository ?? throw new ArgumentNullException(nameof(WorkOrderRepository));
        }


     
        // 작업 시작 프로시저 호출 (새로 생성 필요)
        public async Task StartWorkOrder(int woId, string userId, string eqCode)
        {
            
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        var param = new { WO_ID = woId, USER_ID = userId, EQ_CODE = eqCode };

                        await db.ExecuteAsync("SP_StartWorkOrder",
                                                param,
                                                transaction: transaction,
                                                commandType: CommandType.StoredProcedure);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        // 작업 종료 프로시저 호출 (새로 생성 필요)
        public async Task StopWorkOrder(int woId, string userId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        var param = new { WO_ID = woId, USER_ID = userId };

                        await db.ExecuteAsync("SP_StopWorkOrder",
                                                param,
                                                transaction: transaction,
                                                commandType: CommandType.StoredProcedure);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }

        }

        // PLC 실적 처리 프로시저 호출 (기존 생성한 SP_ProcessDeviceProduction)
        public async Task ProcessProduction(string eqCode, long logId, int goodQty, int badQty, string userId)
        {
            using (IDbConnection db = new SqlConnection(_connectionString))
            {
                db.Open();
                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        var param = new {
                            EqCode = eqCode,
                            RawLogId = logId,
                            GoodQty = goodQty,
                            BadQty = badQty,
                            WorkerId = userId
                        };

                        await db.ExecuteAsync("SP_ProcessDeviceProduction",
                                                param,
                                                transaction: transaction,
                                                commandType: CommandType.StoredProcedure);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
           
        }
    }
}
