using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.DTOs
{
    /// <summary>
    /// 작업이 끝난 후 "양품은 몇 개고, 불량은 몇 개다"라고 보고할 때 사용하는 바구니입니다.
    /// </summary>
    public class WorkResultDto
    {
        // [1. 결과 고유 번호] 
        // 이 실적 기록 자체가 가지는 번호입니다. (DB에서 자동으로 붙여주는 번호)
        public int ResultId { get; set; }

        // [2. 연결된 작업 지시 번호] 
        // "어떤 작업 지시"에 대한 결과인지 알려주는 열쇠입니다. (WO_ID와 짝꿍)
        public int WorkOrderId { get; set; }

        // [3. 잘 만든 개수] 
        // 검사를 통과한 깨끗한 제품(양품)의 수량입니다.
        public int GoodQuantity { get; set; }

        // [4. 못 만든 개수] 
        // 실수로 잘못 만들어서 버려야 하는 제품(불량)의 수량입니다.
        public int BadQuantity { get; set; }

        // [5. 기록된 시간] 
        // 이 실적을 언제 컴퓨터에 입력했는지 저장하는 날짜와 시간입니다.
        public DateTime ResultDate { get; set; }

        // [팁] 나중에 화면에 "품목 코드"도 같이 보여주고 싶다면 아래 주석을 풀어서 사용합니다.
        // public string ItemCode { get; set; }
    }
}