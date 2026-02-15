using Dapper; // Dapper 기능을 사용하기 위한 네임스페이스
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports; // 시리얼 통신을 위한 네임스페이스
using System.Linq;

namespace MiniMES.Infastructure.Services
{
    /// <summary>
    /// 설비(PLC)와 시리얼 통신을 담당하고 데이터를 처리하는 서비스
    /// </summary>
    public class SerialDeviceService : IDisposable
    {
        private SerialPort? _port;
        private readonly string? _connStr;
        private readonly IWorkOrderRepository? _repo;

        // 데이터가 처리되었을 때 UI(ViewModel)에 알리기 위한 이벤트
        public event Action? OnRefreshRequired;

        public SerialDeviceService(string connStr, IWorkOrderRepository repo)
        {
            _connStr = connStr;
            _repo = repo;
        }

        /// <summary>
        /// 시리얼 포트를 연결하고 데이터 수신 대기를 시작합니다.
        /// </summary>
        /// <param name="portName">연결할 포트명 (예: COM1)</param>
        public void Open(string portName)
        {
            try
            {
                if (_port != null && _port.IsOpen) _port.Close();

                _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);

                // 데이터를 받았을 때 실행될 이벤트 핸들러 등록
                _port.DataReceived += (s, e) => {
                    // 수신된 문자열 읽기 (예: "EQ01,1,0")
                    string data = _port.ReadExisting().Trim();
                    if (!string.IsNullOrEmpty(data))
                    {
                        HandleData(data);
                    }
                };

                _port.Open();
            }
            catch (Exception ex)
            {
                // 실무에서는 여기서 로그 파일에 에러를 기록합니다.
                throw new Exception($"포트 연결 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 수신된 데이터를 분석하여 DB에 저장하고 실적을 처리합니다.
        /// </summary>
        private void HandleData(string data)
        {
            using (var conn = new SqlConnection(_connStr))
            {
                // 1. Raw 로그 저장 (프로시저 호출 방식)
                var parts = data.Split(',');
                if (parts.Length < 3) return; // 데이터 형식이 맞지 않으면 종료

                string eqCode = parts[0];

                // SP_InsertEquipmentLog 프로시저 호출하여 LogId 받아오기
                var logId = conn.QuerySingle<long>(
                    "SP_InsertEquipmentLog",
                    new { EqCode = eqCode, Direction = "IN", RawData = data },
                    commandType: CommandType.StoredProcedure
                );

                // 2. Repository의 실적 처리 SP 호출 (WorkOrder 수량 업데이트 등)
                // parts[1]: 양품수량, parts[2]: 불량수량
                _repo.ProcessProduction(eqCode, logId, int.Parse(parts[1]), int.Parse(parts[2]), "SYSTEM");

                // 3. UI 갱신 알림 이벤트 발생 (메인 스레드 호출은 ViewModel에서 처리)
                OnRefreshRequired?.Invoke();
            }
        }

        /// <summary>
        /// 서비스 종료 시 포트를 닫아 메모리 누수를 방지합니다.
        /// </summary>
        public void Dispose()
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
                _port.Dispose();
            }
        }
    }
}