using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMes.Domain.Entities
{
    [Table("TB_STOCK")]
    public class StockEntity : BaseEntity
    {
        [Key]
        [Column("ITEM_CODE")]
        [StringLength(50)]
        public string ItemCode { get; set; } = string.Empty;

        [Column("CURRENT_QTY")]
        public decimal CurrentQty { get; set; }

        //[Column("UPDATED_AT")]
        //public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation Property (품목 마스터와 연결)
        [ForeignKey("ItemCode")]
        public virtual ItemEntity Item { get; set; }
    }
}
