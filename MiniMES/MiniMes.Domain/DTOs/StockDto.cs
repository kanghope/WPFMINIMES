using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.DTOs
{
    public class StockDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty; // ItemMaster JOIN 결과
        public string ItemUnit {  get; set; } = string.Empty;
        public decimal CurrentQty { get; set; }
        public DateTime UpdatedAt { get; set; }

        // UI에서 입고 수량을 입력받기 위한 속성
        public decimal InboundQty { get; set; }
    }
}
