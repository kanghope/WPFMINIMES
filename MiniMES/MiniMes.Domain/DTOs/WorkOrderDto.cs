using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniMes.Domain.Commons; // 작업 상태를 정의한 '약속(Enum)'을 가져옵니다.

namespace MiniMes.Domain.DTOs
{
    /// <summary>
    /// 작업 지시 정보를 담아서 화면(UI)과 서버 사이를 왔다 갔다 하는 '데이터 바구니'입니다.
    /// </summary>
    public class WorkOrderDto
    {
        // [1. 기본 정보들] DB 테이블의 컬럼들과 1:1로 대응되는 값들입니다.
        public int Id { get; set; }             // 작업 지시 번호 (고유 ID)
        public string ItemCode { get; set; }    // 제품 코드 (예: "BOLT_01")
        public int Quantity { get; set; }       // 주문 수량
        public string Status { get; set; }      // 원본 상태값 (DB에는 "W", "P", "C" 등으로 저장됨)
        public DateTime WoDate { get; set; }      //등록일
        public int? CompleteQty { get; set; } //완료된 정상 수량

        // 화면의 ProgressBar와 바인딩될 프로퍼티
        public double ProgressValue
        {
            get
            {
                if (Quantity <= 0) return 0;
                // (완료수량 / 계획수량) * 100 A ?? B A가 NULL이 아니면 A를 쓰고 NULL이면 B를써라.
                double progress = ((double)(CompleteQty ?? 0) / Quantity) * 100;
                return progress > 100 ? 100 : progress; // 100% 넘지 않게 방어
            }
        }

        // [2. 화면 표시용 속성] 
        // 사용자가 "W"라고 보면 무슨 뜻인지 모르니, 친절하게 "대기"라고 바꿔서 보여주는 역할입니다.
        public string DisplayStatus
        {
            get
            {
                // Status 변수에 들어있는 글자(W, P, C, X)가 무엇이냐에 따라 다른 한글 이름을 돌려줍니다.
                switch (Status)
                {
                    case "W":
                        return "대기";    // Wait -> 대기
                    case "P":
                        return "진행중";  // Processing -> 진행중
                    case "C":
                        return "완료";    // Complete -> 완료
                    case "X":
                        return "취소";    // Cancel -> 취소
                    default:
                        return "알수없음"; // 그 외의 이상한 값이 들어왔을 때
                }
            }
        }

        // [3. 로직 처리용 속성]
        // 프로그래밍적으로 "대기 중인지" 체크할 때, 글자("W")로 비교하면 오타가 날 수 있습니다.
        // 그래서 미리 정의된 Enum(열거형) 타입으로 변환해서 가져오는 편리한 기능입니다.
        public WorkOrderStatus StatusEnum
        {
            // FromDbCode라는 도구를 써서 "W"라는 글자를 'WorkOrderStatus.Wait'라는 프로그래밍 코드로 변환합니다.
            get { return WorkOrderStatusExtensions.FromDbCode(this.Status); }
        }
    }
}