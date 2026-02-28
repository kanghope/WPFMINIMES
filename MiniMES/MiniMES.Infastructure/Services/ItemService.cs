using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity; // EF6 기준, EF Core는 Microsoft.EntityFrameworkCore
using System.Linq;
using System.Threading.Tasks;

namespace MiniMES.Infastructure.Services
{
    public class ItemService : IItemService
    {

        public ItemService() { }
       

        public async Task<List<ItemDto>> GetAllItemsAsync()
        {
            using (var context = new MesDbContext())
            {
                return await context.Items
                .Select(i => new ItemDto
                {
                    ItemCode = i.ITEM_CODE,
                    ItemName = i.ITEM_NAME,
                    ItemSpec = i.ITEM_SPEC,
                    ItemUnit = i.ITEM_UNIT,
                    ItemType = i.ITEM_TYPE,
                    IsActive = i.IS_ACTIVE
                }).ToListAsync();
            }
        }

        public async Task<bool> SaveItemAsync(ItemDto dto)
        {
            using (var context = new MesDbContext())
            {
                var entity = await context.Items.FindAsync(dto.ItemCode);

                if (entity == null) // 신규 등록
                {
                    entity = new ItemEntity
                    {
                        ITEM_CODE = dto.ItemCode,
                        CREATED_AT = DateTime.Now
                    };
                    context.Items.Add(entity);
                }

                // 값 업데이트
                entity.ITEM_NAME = dto.ItemName;
                entity.ITEM_SPEC = dto.ItemSpec;
                entity.ITEM_UNIT = dto.ItemUnit;
                entity.ITEM_TYPE = dto.ItemType;
                entity.IS_ACTIVE = dto.IsActive;
                entity.UPDATED_AT = DateTime.Now;

                return await context.SaveChangesAsync() > 0;
            }
        }

        public async Task<bool> DeleteItemAsync(string itemCode)
        {
            using (var context = new MesDbContext())
            {
                var entity = await context.Items.FindAsync(itemCode);
                if (entity == null) return false;

                context.Items.Remove(entity);
                return await context.SaveChangesAsync() > 0;
            }
        }

    }
}
