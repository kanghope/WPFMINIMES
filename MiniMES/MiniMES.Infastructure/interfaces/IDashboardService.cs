using MiniMes.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMES.Infastructure.interfaces
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetTodayProductionSummaryAsync();
    }
}
