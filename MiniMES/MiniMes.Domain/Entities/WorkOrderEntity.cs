using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMes.Domain.Entities
{
    // [1. 테이블 이름표] 실제 DB에 있는 테이블 이름이 "TB_WORKORDER"라고 알려줍니다.
    [Table("TB_WORKORDER")]
    public class WorkOrderEntity
    {
        // [2. 기본 키(PK)] 이 테이블에서 각 데이터를 구별하는 유일한 번호(ID)입니다.
        [Key]
        public int WO_ID { get; set; }

        // [3. 필수 입력값] 빈칸(NULL)일 수 없으며, 최대 길이는 50글자입니다.
        [Required]
        [MaxLength(50)]
        public string ITEM_CODE { get; set; } // 품목 코드

        public int WO_QTY { get; set; } // 작업 지시 수량

        // [4. 컬럼 이름 매핑] C# 변수명은 WO_DATE지만, DB 컬럼명도 똑같이 맞춘다는 설정입니다.
        [Column("WO_DATE")]
        public DateTime WO_DATE { get; set; } // 작업 지시 날짜

        // 작업 상태 ('W': 대기, 'P': 진행, 'C': 완료)
        [MaxLength(10)]
        public string WO_STATUS { get; set; }

        // --- 추가된 실무 컬럼 ---
        public string EQ_CODE { get; set; }     // 작업을 수행 중인 설비 코드
        public int? COMPLETE_QTY { get; set; } = 0;   // 실시간 누적 양품 수량 // 방법 2: 기본값을 0으로 강제 지정 (가장 권장됨)
        // -----------------------

        // [5. 자동 생성 값] 데이터가 들어갈 때 DB가 자동으로 시간을 찍어주는 컬럼입니다.
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CREATED_AT { get; set; } // 생성 일시

        public DateTime UPDATED_AT { get; set; } // 수정 일시

        // -----------------------
        // [6. 연관 관계] 
        // "하나의 작업 지시"에는 "여러 개의 실적 기록"이 붙을 수 있음을 나타냅니다. (1:N 관계)
        // virtual 키워드는 EF가 필요할 때 데이터를 자동으로 불러오는 기능을 켜줍니다.
        /*
            상황,사용 여부,이유
            1:N 관계의 자식 목록,권장 (virtual),실적이 수만 개일 때 한꺼번에 가져오면 프로그램이 멈출 수 있습니다.
            N:1 관계의 부모 정보,권장 (virtual),상품 정보를 볼 때 카테고리 정보가 항상 필요하지 않을 수 있습니다.
            "단순한 데이터 (ID, 이름)",미사용,"클래스 내부의 일반 속성(string, int)에는 쓰지 않습니다."
        "부모가 만든 기능을 자식이 입맛에 맞게 바꿔 쓸 수 있도록 허락해주기 위해서" 사용합니다.
         */
        public virtual ICollection<WorkResultEntity> Results { get; set; }
    }
}