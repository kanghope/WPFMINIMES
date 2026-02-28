using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Entity;

namespace MiniMES.Infastructure.Services
{
    public class BomService : IBomService
    {
        public async Task<List<BomDto>> GetBomListByParentAsync(string parentCode)
        {
            using (var db = new MesDbContext())
            {
                // ChildItem 정보를 Include하여 한 번에 가져옴
                var query = await db.Boms
                    .Include(b => b.ChildItem)
                    .Where(b => b.ParentItemCode == parentCode)
                    .ToListAsync();

                return query.Select(b => new BomDto
                {
                    BomId = b.BomId,
                    ParentItemCode = b.ParentItemCode,
                    ChildItemCode = b.ChildItemCode,
                    ChildItemName = b.ChildItem.ITEM_NAME,
                    ChildItemSpec = b.ChildItem.ITEM_SPEC,
                    ChildItemUnit = b.ChildItem.ITEM_UNIT,
                    Consumption = b.Consumption
                }).ToList();
            }
        }

        public async Task<bool> SaveBomAsync(BomEntity bomEntity)
        {
            using (var db = new MesDbContext())
            {
                if (bomEntity.BomId == 0)
                {
                    // 1. 부모와 자식 코드가 마스터에 존재하는지 먼저 검사
                    bool parentExists = await db.Items.AnyAsync(i => i.ITEM_CODE == bomEntity.ParentItemCode);
                    bool childExists = await db.Items.AnyAsync(i => i.ITEM_CODE == bomEntity.ChildItemCode);

                    if (!parentExists || !childExists)
                    {
                        // 여기서 예외를 던지거나 false를 반환하여 UI에서 "마스터를 확인하세요"라고 띄움
                        throw new Exception("등록되지 않은 품목 코드가 포함되어 있습니다.");
                    }
                    // 신규 등록 시 감사 컬럼 채우기
                    //bomEntity.CreatedAt = DateTime.Now;
                    db.Boms.Add(bomEntity);
                }
                else
                {
                    // 수정 시 감사 컬럼 업데이트
                    var existing = await db.Boms.FindAsync(bomEntity.BomId);
                    if (existing == null) return false;

                    existing.ChildItemCode = bomEntity.ChildItemCode;
                    existing.Consumption = bomEntity.Consumption;
                    //existing.UpdatedAt = DateTime.Now;
                }
                return await db.SaveChangesAsync() > 0;
            }
        }

        public async Task<bool> DeleteBomAsync(int bomId)
        {
            using (var db = new MesDbContext())
            {
                var bom = await db.Boms.FindAsync(bomId);
                if (bom == null) return false;

                db.Boms.Remove(bom);
                return await db.SaveChangesAsync() > 0;
            }
        }
    }
}
