using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniMes.Domain.Entities
{
    [Table("TB_USER")]
    public class UserEntity : BaseEntity
    {
        [Key]
        public string USER_ID { get; set; }
        public string USER_PW { get; set; }
        public string USER_NAME { get; set; }
        public string USER_ROLE { get; set; } // ADMIN, OPERATOR
        public bool IS_ACTIVE { get; set; }
    }
}