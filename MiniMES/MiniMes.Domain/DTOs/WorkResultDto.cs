using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;



namespace MiniMes.Domain.DTOs

{

    public class WorkResultDto

    {

        public int ResultId { get; set; }

        public int WorkOrderId { get; set; } // WO_ID

        public int GoodQuantity { get; set; } // 양품 수량

        public int BadQuantity { get; set; }  // 불량 수량

        public DateTime ResultDate { get; set; } // 등록 시간



        // 필요하다면 UI 표시를 위해 WorkOrder의 ItemCode 등 추가 가능

        // public string ItemCode { get; set; }

    }

}