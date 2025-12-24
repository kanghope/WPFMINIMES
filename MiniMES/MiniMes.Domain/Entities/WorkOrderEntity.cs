using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;





namespace MiniMes.Domain.Entities

{

    [Table("TB_WORKORDER")] // DB 테이블명 지정

    public class WorkOrderEntity

    {

        [Key] // Primary Key 지정

        public int WO_ID { get; set; }



        [Required] // NOT NULL

        [MaxLength(50)]

        public string ITEM_CODE { get; set; }



        public int WO_QTY { get; set; }



        [Column("WO_DATE")] // 실제 DB의 컬럼 이름 (대소문자 포함)

        public DateTime WO_DATE { get; set; }



        // Status: 'W' (Wait), 'P' (Process), 'C' (Complete)

        [MaxLength(10)]

        public string WO_STATUS { get; set; }



        // CREATED_AT 및 UPDATED_AT은 DB에서 기본값을 계산하므로 Computed로 설정

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]

        public DateTime CREATED_AT { get; set; }



        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]

        public DateTime UPDATED_AT { get; set; }

        // -----------------------



        // Navigation Property: 외래키 관계 매핑 (주석 해제)

        public virtual ICollection<WorkResultEntity> Results { get; set; }

    }

}