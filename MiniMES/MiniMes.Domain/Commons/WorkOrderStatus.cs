using System;
using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;



namespace MiniMes.Domain.Commons

{



    // 작업 지시의 상태를 정의합니다.

    public enum WorkOrderStatus

    {

        Wait,       // 대기 ('W')

        Processing, // 진행 중 ('P')

        Complete,   // 완료 ('C')

        Cancel      // 취소 ('X')

    }



    // Enum과 DB 코드(문자열) 간 변환을 위한 확장 메서드

    public static class WorkOrderStatusExtensions

    {

        public static string ToDbCode(this WorkOrderStatus status)

        {

            switch (status)

            {

                case WorkOrderStatus.Wait:

                    return "W";

                case WorkOrderStatus.Processing:

                    return "P";

                case WorkOrderStatus.Complete:

                    return "C";

                case WorkOrderStatus.Cancel:

                    return "X";

                default:

                    throw new ArgumentOutOfRangeException(nameof(status), status, "유효하지 않은 상태입니다.");

            }

        }



        // 필요하다면 DB 코드를 Enum으로 변환하는 FromDbCode 메서드도 추가할 수 있습니다.

        // --- 2. DB 문자열 코드를 Enum으로 변환하는 FromDbCode 메서드 추가 ---

        public static WorkOrderStatus FromDbCode(string dbCode)

        {

            switch (dbCode.ToUpper()) // 대소문자 관계없이 처리하기 위해 대문자 변환

            {

                case "W":

                    return WorkOrderStatus.Wait;

                case "P":

                    return WorkOrderStatus.Processing;

                case "C":

                    return WorkOrderStatus.Complete;

                case "X":

                    return WorkOrderStatus.Cancel;

                default:

                    // 알 수 없는 코드는 기본값(Wait)으로 처리하거나 예외를 발생시킬 수 있습니다.

                    return WorkOrderStatus.Wait;

            }

        }

    }

}