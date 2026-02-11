using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using MiniMes.Domain.Entities;

namespace MiniMes.Infrastructure.Data
{
    class MesDbContext : DbContext
    {
        // App.config의 <connectionStrings> 섹션에 정의된 이름("MesConnection")을 사용합니다.
        public MesDbContext() : base("MesConnection")
        {
            // 이 줄을 추가하여 모델이 변경되었는지 확인하는 EF의 초기화 전략을 변경합니다.
            // 개발 초기 단계에서만 사용하고, 실서버에서는 Migrations를 사용해야 합니다.
            Database.SetInitializer<MesDbContext>(null);
        }

        // DBSet: 각 테이블에 대응되는 컬렉션입니다.
        public DbSet<WorkOrderEntity>? WorkOrders { get; set; }
        public DbSet<WorkResultEntity>? WorkResults { get; set; }

        // [추가] UserEntity를 DB 모델에 포함시킵니다.
        public DbSet<UserEntity>? Users { get; set; }
        // 추가적인 모델 설정이 필요할 경우 OnModelCreating 오버라이딩
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Ex: 복합 키 설정 등
        }
    }
}