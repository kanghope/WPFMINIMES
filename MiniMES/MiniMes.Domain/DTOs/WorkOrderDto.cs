using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;

using MiniMes.Domain.Commons; // WorkOrderStatus Enum 참조

namespace MiniMes.Domain.DTOs

{

    public class WorkOrderDto

    {

        public int Id { get; set; }



        public string ItemCode { get; set; }

        public int Quantity { get; set; }



        // UI에 보여줄 "가공된" 값 (Entity에 없는 속성)

        public string DisplayStatus

        {

            get

            {

                // Status(W, P, C)에 따라 한글로 변환하여 반환

                switch (Status)

                {

                    case "W":

                        return "대기";

                    case "P":

                        return "진행중";

                    case "C":

                        return "완료";

                    case "X":

                        return "취소";

                    default:

                        return "알수없음";

                }
                ;

            }

        }



        // 상태 코드를 Enum으로 변환하는 헬퍼 속성 (ViewModel에서 상태 변경 시 사용)

        public WorkOrderStatus StatusEnum

        {

            //get => WorkOrderStatusExtensions.FromDbCode(this.Status);

            get { return WorkOrderStatusExtensions.FromDbCode(this.Status); }

        }



        public string Status { get; set; } // 원본 상태값





    }

}