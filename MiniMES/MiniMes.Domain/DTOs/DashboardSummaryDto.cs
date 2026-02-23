using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.DTOs
{
    /// <summary>
    /// 대시보드 상단 현황판 및 차트용 통합 데이터 바구니
    /// </summary>
    public class DashboardSummaryDto
    {
        public int TotalGoodQty { get; set; }      // 오늘 누적 양품 수량
        public int TotalBadQty { get; set; }       // 오늘 누적 불량 수량
        public double AchievementRate { get; set; } // 목표 대비 달성률 (%)
        public int ActiveOrderCount { get; set; }   // 현재 진행 중인 작업 수
    }
}
