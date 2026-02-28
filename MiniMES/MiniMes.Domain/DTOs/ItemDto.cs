using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.DTOs
{
    public class ItemDto
    {
        public string ItemCode { get; set; }//품목코드
        public string ItemName { get; set; }//품콕코드이름
        public string ItemSpec { get; set; }//규격
        public string ItemUnit { get; set; } //// 단위 (EA, KG 등)
        public string ItemType { get; set; } // "FG" or "RM"
        public bool IsActive { get; set; } // 활성화 여부

        // [추가] 감사(Audit) 데이터 전송용
        public DateTime? CreatedAt { get; set; } // 생성일
        public string CreatedBy { get; set; }    // 생성자
        public DateTime? UpdatedAt { get; set; } // 수정일
        public string UpdatedBy { get; set; }    // 수정자

        // 화면 표시용: FG -> 완제품, RM -> 원자재
        public string ItemTypeName => ItemType == "FG" ? "완제품" : (ItemType == "RM" ? "원자재" : "기타");

        // 상태 표시용
        public string StatusText => IsActive ? "사용중" : "중지";
    }
}
