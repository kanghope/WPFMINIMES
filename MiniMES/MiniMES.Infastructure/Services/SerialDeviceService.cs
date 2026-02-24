using Dapper; // Dapper 기능을 사용하기 위한 네임스페이스
using MiniMes.Infrastructure.Interfaces;
using MiniMES.Infastructure.interfaces;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO.Ports; // 시리얼 통신을 위한 네임스페이스
using System.Linq;
using System.Windows;
using MiniMES.Infrastructure.Auth;

namespace MiniMES.Infastructure.Services
{
    /// <summary>
    /// 설비(PLC)와 시리얼 통신을 담당하고 데이터를 처리하는 서비스
    /// </summary>
    public class SerialDeviceService : IDisposable
    {
        private SerialPort? _port;
        private System.Timers.Timer? _reconnectTimer; // [추가] 재연결 감시용 타이머
        private string? _lastPortName;  // [추가] 마지막으로 시도한 포트명 저장
        private int _lastBaudRate;      // [추가] 마지막으로 시도한 속도 저장

        private readonly string? _connStr = ConfigurationManager.ConnectionStrings["MesConnection"].ConnectionString;
        private readonly IWorkOrderRepository? _repo;

        // [추가] 연결 상태가 변했을 때 UI에 알리기 위한 이벤트
        public event Action<bool>? OnConnectionStatusChanged;

        // 데이터가 처리되었을 때 UI(ViewModel)에 알리기 위한 이벤트
        //event: 외부에서 이 이벤트를 마음대로 호출하지 못하게 막고, 오직 +=(구독)와 -=(해지)만 가능하도록 제한하는 안전장치입니다.
        //Action<bool>: 이 이벤트가 발생할 때 bool 데이터 하나를 실어서 보내겠다는 약속(델리게이트)입니다.
        public event Action<bool>? OnRefreshRequired;

        // [추가] 설비가 가동 시작(START) 신호를 보냈을 때 발생할 이벤트
        public event Action<string>? OnDeviceStarted;

        // [추가] 설비로부터 종료 신호를 받았을 때 발생시킬 이벤트
        public event Action<string>? OnWorkFinishedByDevice;

        // [추가] UI에 원본 데이터를 실시간으로 전달하기 위한 이벤트
        // ViewModel에서 이 이벤트를 구독하여 리스트박스에 로그를 남깁니다.
        public event Action<string>? OnDataReceived;


        public bool IsOpen => _port?.IsOpen ?? false;
        public SerialDeviceService()
        {
           
        }
        public SerialDeviceService(IWorkOrderRepository repo)
        {
            _repo = repo;

            // [추가] 재연결 타이머 초기화 (5초마다 체크)
            _reconnectTimer = new System.Timers.Timer(5000);
            _reconnectTimer.Elapsed += async (s, e) => await CheckConnection();
            _reconnectTimer.AutoReset = true;
        }

        /// <summary>
        /// 시리얼 포트를 연결하고 데이터 수신 대기를 시작합니다.
        /// </summary>
        /// <param name="portName">연결할 포트명 (예: COM1)</param>
        public void Open(string portName, int baudRate = 9600)
        {
            try
            {
                //if (_port != null && _port.IsOpen) _port.Close();
                Close(); // 기존 포트 정리

                _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    NewLine = "\r\n", // PLC 전송 규약에 맞춤
                    ReadTimeout = 5000,
                    WriteTimeout = 5000,
                    // [추가] 버퍼 크기를 늘려 데이터 유실 방지
                    ReadBufferSize = 4096
                };
                /*
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
                */
                _port.DataReceived += SerialPort_DataReceived;
                _port.Open();

                _reconnectTimer?.Start(); // 연결 성공 시 타이머 시작
                OnConnectionStatusChanged?.Invoke(true); // 연결 성공 알림
            }
            catch (Exception ex)
            {
                // 실무: Log4Net 또는 Serilog 사용 권장
                _reconnectTimer?.Start(); // 실패해도 타이머는 돌려서 계속 재시도
                OnConnectionStatusChanged?.Invoke(false);
                throw new Exception($"[PLC] {portName} 연결 실패: {ex.Message}");
            }
        }

        // [핵심 추가] 연결 상태 체크 및 재연결 시도
        private async Task CheckConnection()
        {
            // 포트가 없거나 닫혀 있는 경우
            if (_port == null || !_port.IsOpen)
            {
                OnConnectionStatusChanged?.Invoke(false);
                Console.WriteLine($"[DEBUG] 통신 끊김 감지. 재연결 시도 중... ({_lastPortName})");

                try
                {
                    // 다시 Open 시도
                    if (!string.IsNullOrEmpty(_lastPortName))
                    {
                        Open(_lastPortName, _lastBaudRate);
                    }
                }
                catch
                {
                    // 재시도 실패 시 다음 타이머 주기에 다시 시도
                }
            }
        }

        // [추가] 데이터 수신 부분을 별도 메서드로 분리 (안정성)
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            if (!sp.IsOpen) return;
            try
            {
                // [보완] 읽기 전 읽을 데이터가 있는지 확인
                while (sp.BytesToRead > 0)
                {
                    string data = sp.ReadLine().Trim(); // 여기서 \r\n을 기다림
                    if (!string.IsNullOrEmpty(data))
                    {
                        // [핵심 추가] 수신된 원본 데이터를 구독자(ViewModel)에게 즉시 알림
                        // 비동기로 처리하여 데이터 분석(HandleData)과 UI 로그 출력이 동시에 일어나게 합니다.
                        OnDataReceived?.Invoke(data);
                        //Task.Run(() => HandleData(data));
                        // [수정] 분석 및 저장을 수행하고, 완료 후 UI에 알림
                        // Task.Run을 사용하여 비동기로 실행해야 UI 스레드가 안 막힙니다.
                        Task.Run(async () => {
                            HandleData(data); // 기존 메서드 호출

                            // [추가] 데이터 처리가 끝났으므로 대시보드에게 새로고침하라고 신호를 보냅니다.
                            //"나 방금 새로운 생산 실적을 DB에 넣었어! 화면을 새로고침해야 해!
                            OnRefreshRequired?.Invoke(true);
                        });
                    }
                }
            }
            catch (TimeoutException)
            {// ReadLine 중에 엔터가 안 오면 발생함. 로그만 남기고 무시하여 스레드를 살려둠.
                Console.WriteLine("[DEBUG] 시리얼 읽기 타임아웃 발생 (엔터 신호 미수신)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 데이터 수신 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 수신된 데이터를 분석하여 DB에 저장하고 실적을 처리합니다.
        /// </summary>
        private void HandleData(string data)
        {
            var parts = data.Split(',');
            if (parts.Length < 2) return;

            string eqCode = parts[0];
            string msgType = parts[1]; // 전문 구분 (QTY, END 등)

            try
            {
                switch (msgType)
                {
                    case "START": // 예: EQ01,START
                                  // 1. 단순 로깅 (파일이나 콘솔)
                        Console.WriteLine($"[LOG] {eqCode} 설비가 가동 준비를 마쳤습니다.");
                        MessageBox.Show($"[LOG] {eqCode} 설비가 가동 준비를 마쳤습니다.");
                        // 2. ViewModel에 알림 (UI 표시용)
                        OnDeviceStarted?.Invoke(eqCode);
                        break;
                    case "QTY": // 예: EQ01,QTY,1,0
                        if (parts.Length < 4) return;
                        ProcessProduction(eqCode, data, parts[2], parts[3]);
                        break;

                    case "END": // 예: EQ01,END
                                // [핵심] ViewModel에게 이 설비의 작업이 끝났음을 알림
                        OnWorkFinishedByDevice?.Invoke(eqCode);
                        break;

                    case "ERR": // 예: EQ01,ERR,05
                        string errCode = parts.Length > 2 ? parts[2] : "Unknown";
                        // 알람 로그 등을 DB에 저장하는 로직을 여기에 추가 가능
                        break;
                }
            }
            catch (Exception ex)
            {
                // DB 입력 실패 시 로그 및 현장 알림 로직 필요
                // 시리얼 수신 스레드에서 발생하는 에러가 UI를 죽이지 않도록 로그만 남깁니다.
                System.Diagnostics.Debug.WriteLine($"[DB Error] 오류 발생: {ex.Message}");
            }
           

        }

        // 기존 DB 저장 로직을 메서드로 분리 (가독성)
        private async void ProcessProduction(string eqCode, string rawData, string okQty, string ngQty)
        {
            if (!int.TryParse(okQty, out int ok) || !int.TryParse(ngQty, out int ng))
            {
                // 로그: 데이터 형식이 잘못됨
                return;
            }

            try
            {
                // 1. 현재 세션에서 아이디 가져오기 (없으면 "SYSTEM")
                string currentUserId = !string.IsNullOrEmpty(MiniMES.Infrastructure.Auth.UserSession.UserId)
                                       ? UserSession.UserId
                                       : "SYSTEM";
                string currentStatus = "";
                using (var conn = new SqlConnection(_connStr))
                {
                    await conn.OpenAsync(); // 비동기 연결 권장

                    // 1. Raw 로그 저장 (Dapper 비동기 호출)
                    var logId = await conn.QuerySingleAsync<long>(
                        "SP_InsertEquipmentLog",
                        new { EqCode = eqCode, Direction = "IN", RawData = rawData },
                        commandType: CommandType.StoredProcedure
                    );

                    // 2. Repository 호출 (await 추가)
                    if (_repo != null)
                    {
                        currentStatus = await _repo.ProcessProduction(eqCode, logId, ok, ng, currentUserId);
                    }

                    bool flag = currentStatus == "C" ? true : false;
                    
                    // 3. UI 갱신 (보통 ViewModel의 이벤트이므로 UI 스레드 동기화는 ViewModel에서 처리)
                    OnRefreshRequired?.Invoke(flag);
                }
            }
            catch (Exception ex)
            {
                // 시리얼 수신 스레드에서 발생하는 에러가 UI를 죽이지 않도록 로그만 남깁니다.
                System.Diagnostics.Debug.WriteLine($"[DB Error] 실적 저장 실패: {ex.Message}");
            }
        }
        public void Close()
        {
            _reconnectTimer?.Stop(); // 닫을 때는 타이머도 정지
            if (_port != null)
            {
                _port.DataReceived -= SerialPort_DataReceived;
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
                _port = null;
            }
            OnConnectionStatusChanged?.Invoke(false);
        }
        /// <summary>
        /// 서비스 종료 시 포트를 닫아 메모리 누수를 방지합니다.
        /// </summary>
        public void Dispose()
        {
            Close();
            /*
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
                _port.Dispose();
            }*/
        }
    }
}