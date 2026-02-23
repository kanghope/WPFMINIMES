using Dapper;
using MiniMes.Domain.DTOs;
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MiniMES.Infastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["MesConnection"].ConnectionString;

        // 1. [활성화] IWorkOrderService 인터페이스 필드 선언
        //private readonly IDashboardService _dashboardServic;

        // [활성화] 생성자: DI 환경에서 IWorkOrderService를 주입받도록 구현
        public DashboardService()
        {
            //_dashboardServic = dashboardServic ?? throw new ArgumentNullException(nameof(dashboardServic));
        }

        /// <summary>
        /// 오늘 하루의 전체 생산 현황을 집계하여 가져옵니다.
        /// </summary>
        public async Task<DashboardSummaryDto> GetTodayProductionSummaryAsync()
        {
            using (var conn = new SqlConnection(_connStr))
            {
                // 1. Dapper는 <DashboardSummaryDto> 타입을 지정하면 해당 객체로 자동 매핑하여 리턴합니다.
                var result = await conn.QueryFirstOrDefaultAsync<DashboardSummaryDto>(
                    "SP_GetDashboardSummary",
                    commandType: CommandType.StoredProcedure
                );

                // 2. [수정] 리턴 시 <DashboardSummaryDto>를 붙이지 않고 변수명만 리턴합니다.
                // ?? 연산자를 사용하여 DB 결과가 없을 경우 빈 객체를 반환하면 ViewModel에서 Null 체크 부담이 줄어듭니다.
                return result ?? new DashboardSummaryDto();
            }
        }
    }
}
