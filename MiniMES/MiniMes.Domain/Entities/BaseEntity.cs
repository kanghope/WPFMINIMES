using System;

namespace MiniMes.Domain.Entities
{
    //공통 이력 관리 컬럼을 담은 BaseEntity를 만들고, 기존 Entity들이 이를 상속받게 합니다.
    public abstract class BaseEntity
    {
        public DateTime CREATED_AT { get; set; } = DateTime.Now;
        public string CREATED_BY { get; set; }
        public DateTime UPDATED_AT { get; set; } = DateTime.Now;
        public string UPDATED_BY { get; set; }
    }
}