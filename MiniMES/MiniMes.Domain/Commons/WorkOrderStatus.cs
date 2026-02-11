using System;

namespace MiniMes.Domain.Commons
{
    // [1. 상태 이름표 모음] 
    // 프로그래머가 코드에서 보기 편하게 영어로 이름을 붙여둔 것입니다. (열거형)
    public enum WorkOrderStatus
    {
        Wait,       // 대기 (컴퓨터는 0으로 기억)
        Processing, // 진행 중 (컴퓨터는 1로 기억)
        Complete,   // 완료 (컴퓨터는 2로 기억)
        Cancel      // 취소 (컴퓨터는 3로 기억)
    }

    //콤보박스 key value값 데이터 모델
    public class StatusItem
    {
        public string DisplayName { get; set; } // 화면에 보여줄 글자 (Text)
        public string StatusCode { get; set; }  // 내부적으로 처리할 값 (Value)
    }

    // [2. 통역 도구함] 
    // 영어 이름표를 DB용 기호로 바꾸거나, 반대로 바꾸는 기능을 모아둔 곳입니다.
    public static class WorkOrderStatusExtensions
    {
        // --- 통역 1: 영어 이름표를 받아서 DB 기호(문자열)로 바꿔줍니다. ---
        // 예: WorkOrderStatus.Wait -> "W"
        public static string ToDbCode(this WorkOrderStatus status)
        {
            switch (status)
            {
                case WorkOrderStatus.Wait:
                    return "W"; // 대기는 DB에 'W'라고 저장해!
                case WorkOrderStatus.Processing:
                    return "P"; // 진행 중은 'P'라고 저장해!
                case WorkOrderStatus.Complete:
                    return "C"; // 완료는 'C'라고 저장해!
                case WorkOrderStatus.Cancel:
                    return "X"; // 취소는 'X'라고 저장해!
                default:
                    // 정의되지 않은 엉뚱한 이름이 들어오면 에러를 냅니다.
                    throw new ArgumentOutOfRangeException(nameof(status), status, "유효하지 않은 상태입니다.");
            }
        }

        // --- 통역 2: DB에 저장된 기호를 받아서 다시 영어 이름표로 바꿔줍니다. ---
        // 예: "W" -> WorkOrderStatus.Wait
        public static WorkOrderStatus FromDbCode(string dbCode)
        {
            // 혹시 소문자 'w'가 들어와도 대문자 'W'로 바꿔서 안전하게 검사합니다.
            switch (dbCode.ToUpper())
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
                    // DB에 이상한 글자가 들어있으면 기본값인 '대기(Wait)'로 취급합니다.
                    return WorkOrderStatus.Wait;
            }
        }

        
    }
}