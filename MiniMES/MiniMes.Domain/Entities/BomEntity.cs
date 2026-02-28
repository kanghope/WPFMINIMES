using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.Entities
{
    [Table("TB_BOM")] // DB 테이블 이름을 명시적으로 지정
    public class BomEntity : BaseEntity
    {
        [Key]
        [Column("BOM_ID")]
        public int BomId { get; set; }

        [Required]
        [Column("PARENT_ITEM")]
        public string ParentItemCode { get; set; } = string.Empty; // 속성명 확인

        [Required]
        [Column("CHILD_ITEM")]
        public string ChildItemCode { get; set; } = string.Empty; // 속성명 확인

        [Column("CONSUMPTION", TypeName = "decimal")]
        public decimal Consumption { get; set; }

        // 참조 엔티티를 ItemEntity로 통일 (가장 중요)
        [ForeignKey("ParentItemCode")]
        public virtual ItemEntity ParentItem { get; set; }

        [ForeignKey("ChildItemCode")]
        public virtual ItemEntity ChildItem { get; set; }
    }
}
