using MiniMes.Domain.DTOs;
using MiniMes.Domain.Entities;
using MiniMes.Infrastructure.Data;
using MiniMES.Infastructure.interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace MiniMES.Infastructure.Services
{
    public class StockRepository : IStockRepository
    {
        

        public StockRepository()
        {
          
        }

        // 1. 전체 재고 현황 조회 (Left Join 적용)
        public async Task<IEnumerable<StockDto>> GetStockListAsync(string searchText)
        {
            using (var context = new MesDbContext())
            {
                var query = from i in context.Items
                            join s in context.Stocks on i.ITEM_CODE equals s.ItemCode into ps
                            from s in ps.DefaultIfEmpty()
                                // [수정 포인트] 완제품(FG)인 품목은 입고 대상에서 제외
                            where i.ITEM_TYPE != "FG"
                            select new StockDto {
                                ItemCode = i.ITEM_CODE,
                                ItemName = i.ITEM_NAME,
                                ItemUnit = i.ITEM_UNIT,
                                CurrentQty = s != null ? s.CurrentQty : 0,
                                // s가 null이면 품목 생성일, 있으면 재고 업데이트일 표시
                                UpdatedAt = s != null ? s.UPDATED_AT : i.UPDATED_AT,
                                InboundQty = 0 // 초기 입력값 0
                            };

                // 검색어가 있는 경우에만 필터링
                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(x => (x.ItemCode.Contains(searchText) ||
                                             x.ItemName.Contains(searchText)) );
                }

                // 3) 데이터 반환 (조회 성능을 위해 정렬 추가 권장)
                return await query.OrderBy(x => x.ItemCode).ToListAsync();
            }
        }

        // 2. 재고 입고 처리 (Upsert 로직)
        public async Task UpdateStockInboundAsync(string itemCode, decimal qty)
        {
            using (var context = new MesDbContext())
            {
                // 기존 재고 확인
                var stock = await context.Stocks.FirstOrDefaultAsync(s => s.ItemCode == itemCode);

                if (stock == null)
                {
                    // 재고 레코드가 없으면 신규 생성 (INSERT)
                    var newStock = new StockEntity
                    {
                        ItemCode = itemCode,
                        CurrentQty = qty,
                        //CREATED_AT = DateTime.Now,
                        //UPDATED_AT = DateTime.Now
                        // CREATED_BY 등은 로그인 세션 정보를 받아와서 넣는 것이 좋습니다.
                    };
                    context.Stocks.Add(newStock);
                }
                else
                {
                    // 기존 재고가 있으면 수량 가산 (UPDATE)
                    stock.CurrentQty += qty;
                    //stock.UPDATED_AT = DateTime.Now;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
