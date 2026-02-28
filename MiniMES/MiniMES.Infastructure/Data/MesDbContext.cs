using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using MiniMes.Domain.Entities;
using MiniMes.Domain.DTOs;

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

        // --- DBSet 정의 ---
        public DbSet<ItemEntity> Items { get; set; } = null!;           // TB_ITEM_MASTER
        public DbSet<BomEntity> Boms { get; set; } = null!;             // TB_BOM
        public DbSet<UserEntity> Users { get; set; } = null!;           // TB_USER
        public DbSet<WorkOrderEntity> WorkOrders { get; set; } = null!; // TB_WORKORDER
        public DbSet<WorkResultEntity> WorkResults { get; set; } = null!; // TB_WORKRESULT
        // 추가적인 모델 설정이 필요할 경우 OnModelCreating 오버라이딩
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 1. BomEntity 관계 설정
            modelBuilder.Entity<BomEntity>()
                .HasRequired(b => b.ParentItem)      // "BOM 데이터 하나에는 반드시 부모 품목 정보가 있어야 한다"
                .WithMany()                         // "부모 품목 하나는 여러 개의 BOM 구성을 가질 수 있다"
                .HasForeignKey(b => b.ParentItemCode)// "DB의 PARENT_ITEM 컬럼을 ParentItemCode 속성과 연결한다"
                .WillCascadeOnDelete(false);        // "실수로 품목을 지워도 연결된 BOM 데이터가 자동으로 삭제되지 않게 막는다"

            modelBuilder.Entity<BomEntity>()
                .HasRequired(b => b.ChildItem)
                .WithMany()
                .HasForeignKey(b => b.ChildItemCode)
                .WillCascadeOnDelete(false);

            // 2. 소수점 정밀도 (EF6 방식)
            modelBuilder.Entity<BomEntity>()
                .Property(b => b.Consumption)      // "소요량(Consumption) 속성에 대해서..."
                .HasPrecision(18, 4);               // "전체 18자리 숫자 중 소수점 아래 4자리까지 정밀하게 관리하겠다"

            // 3. WorkOrder & WorkResult (1:N)
            modelBuilder.Entity<WorkResultEntity>()
                .HasRequired(r => r.WorkOrder)      // "실적(Result)은 반드시 어떤 지시(Order)에 소속되어야 한다"
                .WithMany(w => w.Results)           // "작업 지시는 여러 개의 실적 목록(Results)을 가질 수 있다"
                .HasForeignKey(r => r.WO_ID)        // "WO_ID라는 번호를 통해 서로를 찾아낸다"
                .WillCascadeOnDelete(true);         // "지시서를 삭제하면 그에 달린 실적들도 함께 삭제한다 (데이터 정리)"

            // 4. BaseEntity 공통 매핑 (ColumnName 명시)
            // 모든 엔티티에 대해 일일이 설정하거나, 아래처럼 명시
            modelBuilder.Entity<ItemEntity>().Property(e => e.CREATED_AT).HasColumnName("CREATED_AT");
            modelBuilder.Entity<ItemEntity>().Property(e => e.UPDATED_AT).HasColumnName("UPDATED_AT");
        }
    }
}