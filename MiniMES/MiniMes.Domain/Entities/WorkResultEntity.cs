using MiniMes.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace MiniMes.Domain.Entities

{

    [Table("TB_WORKRESULT")]

    public class WorkResultEntity

    {

        [Key]

        public int RESULT_ID { get; set; }



        public int WO_ID { get; set; } // Foreign Key

        public int GOOD_QTY { get; set; }

        public int BAD_QTY { get; set; }



        public DateTime RESULT_DATE { get; set; }

        // Navigation Property: TB_WORKORDER와의 관계 설정

        // 이 엔티티가 어떤 작업 지시(WorkOrder)에 속하는지 나타냅니다.

        [ForeignKey("WO_ID")] // 외래키 관계 지정

        public virtual WorkOrderEntity WorkOrder { get; set; }

    }

}