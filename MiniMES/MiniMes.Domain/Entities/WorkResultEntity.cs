using MiniMes.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMes.Domain.Entities
{
    // [1. 테이블 이름표] 실제 DB의 "TB_WORKRESULT" 테이블과 연결합니다.
    [Table("TB_WORKRESULT")]
    public class WorkResultEntity
    {
        // [2. 실적 번호] 이 기록의 고유한 일련번호(PK)입니다.
        [Key]
        public int RESULT_ID { get; set; }

        // [3. 작업 지시 번호] 
        // "어떤 지시서"를 보고 만든 결과인지 알려주는 연결 고리(FK)입니다.
        public int WO_ID { get; set; }

        public int GOOD_QTY { get; set; } // 잘 만든 개수 (양품)
        public int BAD_QTY { get; set; }  // 못 만든 개수 (불량)

        public string EQ_CODE { get; set; }

        public int RAW_LOG_ID { get; set; }  // 

        public DateTime RESULT_DATE { get; set; } // 실적을 등록한 날짜와 시간

        // [4. 내 부모님(지시서) 찾아가기]
        // "이 실적의 주인인 작업 지시서 정보 전체를 통째로 가져오고 싶어!" 할 때 사용합니다.
        // [ForeignKey("WO_ID")]는 "위의 WO_ID 번호를 써서 지시서를 찾아와라"는 뜻입니다.
        [ForeignKey("WO_ID")]
        public virtual WorkOrderEntity WorkOrder { get; set; }
    }
}