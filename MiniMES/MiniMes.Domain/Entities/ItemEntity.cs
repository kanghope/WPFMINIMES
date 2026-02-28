using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMes.Domain.Entities
{
    [Table("TB_ITEM_MASTER")]
    public class ItemEntity : BaseEntity
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string ITEM_CODE { get; set; } // 품목 코드 (PK)

        [Required]
        [MaxLength(100)]
        public string ITEM_NAME { get; set; } // 품목명

        [MaxLength(100)]
        public string ITEM_SPEC { get; set; } // 규격

        [MaxLength(10)]
        public string ITEM_UNIT { get; set; } // 단위 (EA, KG 등)

        [MaxLength(20)]
        public string ITEM_TYPE { get; set; } // 구분 (FG, RM)

        public bool IS_ACTIVE { get; set; } = true; // 활성화 여부
    }
}
