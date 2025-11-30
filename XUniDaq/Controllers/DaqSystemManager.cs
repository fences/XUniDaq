using DevicesContext.Controllers;
using IcpDas.Daq.Analog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniDAQ_Ns;

namespace IcpDas.Daq.System
{
    #region Enums & EventArgs Models

    public enum BoardStatus
    {
        Stopped,
        Running
    }

    public class BoardStatusChangedEventArgs : EventArgs
    {
        public ushort BoardIndex { get; }
        public BoardStatus Status { get; }
        public DateTime Timestamp { get; }

        public BoardStatusChangedEventArgs(ushort index, BoardStatus status)
        {
            BoardIndex = index;
            Status = status;
            Timestamp = DateTime.Now;
        }
    }

    public class DaqErrorEventArgs : EventArgs
    {
        public string Source { get; }
        public ushort ErrorCode { get; }
        public string Message { get; }
        public string BoardName {  get; }
        public DateTime Timestamp { get; }

        public DaqErrorEventArgs(string source, ushort code, string message,string board="")
        {
            Source = source;
            ErrorCode = code;
            Message = message;
            Timestamp = DateTime.UtcNow;
            BoardName = board;
        }
    }

    public class BoardDetectedEventArgs : EventArgs
    {
        public ushort BoardIndex { get; }
        public string ModelName { get; }
        public int AnalogChannels { get; }
        public int DigitalChannels { get; }

        public BoardDetectedEventArgs(ushort index, string model, int aiCh, int dioCh)
        {
            BoardIndex = index;
            ModelName = model;
            AnalogChannels = aiCh;
            DigitalChannels = dioCh;
        }
    }

    public class BoardInfo
    {
        public ushort Index { get; set; }
        public string ModelName { get; set; }
        public int AI { get; set; }
        public int AO { get; set; }
        public int DI { get; set; }
        public int DO { get; set; }
        public int DIO { get; set; }
        public bool IsRunning { get; set; }

        public override string ToString()
        {
            return $"Board {Index}: {ModelName}";
        }
    }

    #endregion

    public sealed class DaqSystemManager : IDisposable
    {
        #region Static Driver Management
        private static readonly SemaphoreSlim _driverLock = new SemaphoreSlim(1, 1);
        private static int _driverRefCount = 0;
        private static bool _isDriverInitialized = false;
        private static ushort _staticTotalBoards = 0;
        #endregion

        #region Fields & Properties
        private bool _disposed = false;
        private readonly object _taskLock = new object();

        private readonly UniDAQ.IXUD_CARD_INFO[] _sCardInfo = new UniDAQ.IXUD_CARD_INFO[UniDAQ.MAX_BOARD_NUMBER];
        private readonly UniDAQ.IXUD_DEVICE_INFO[] _sDeviceInfo = new UniDAQ.IXUD_DEVICE_INFO[UniDAQ.MAX_BOARD_NUMBER];

        public List<BoardInfo> AvailableBoards { get; private set; } = new List<BoardInfo>();

        public Dictionary<ushort, AnalogInputManager> AnalogControllers { get; } = new Dictionary<ushort, AnalogInputManager>();
        public Dictionary<ushort, DigitalIoManager> DigitalControllers { get; } = new Dictionary<ushort, DigitalIoManager>();

        public bool IsDriverInitialized => _isDriverInitialized;

        private readonly Dictionary<ushort, Task> _loops = new Dictionary<ushort, Task>();
        private readonly Dictionary<ushort, CancellationTokenSource> _tokens = new Dictionary<ushort, CancellationTokenSource>();

        public int MaxErrorRetries { get; set; } = 0;
        public int RetryDelayMs { get; set; } = 2000;
        #endregion

        #region Events
        public event EventHandler<DaqErrorEventArgs> ErrorOccurred;
        public event EventHandler<BoardDetectedEventArgs> BoardDetected;
        public event EventHandler<string> StatusMessage;
        public event EventHandler<BoardStatusChangedEventArgs> BoardStatusChanged;
        #endregion

        public DaqSystemManager() { }


        #region Static Public Methods (New Feature)


        public static async Task<List<BoardInfo>> GetAvailableBoardsStaticAsync()
        {
            await _driverLock.WaitAsync();
            try
            {
                // 1. Initialize Driver if needed
                if (!_isDriverInitialized)
                {
                    ushort totalBoards = 0;
                    ushort rtn = UniDAQ.Ixud_DriverInit(ref totalBoards);

                    if (rtn != UniDAQ.Ixud_NoErr)
                    {
                        throw new Exception($"Failed to initialize UniDAQ driver. Error Code: {rtn}");
                    }

                    _isDriverInitialized = true;
                    _staticTotalBoards = totalBoards;
                }

                // 2. Scan boards using temporary local variables (Thread-Safe for static context)
                var results = new List<BoardInfo>();

                for (ushort i = 0; i < _staticTotalBoards; i++)
                {
                    UniDAQ.IXUD_DEVICE_INFO devInfo = new UniDAQ.IXUD_DEVICE_INFO();
                    UniDAQ.IXUD_CARD_INFO cardInfo = new UniDAQ.IXUD_CARD_INFO();
                    byte[] modeName = new byte[32];

                    ushort rtn = UniDAQ.Ixud_GetCardInfo(i, ref devInfo, ref cardInfo, modeName);

                    if (rtn == UniDAQ.Ixud_NoErr)
                    {
                        int length = Array.IndexOf(modeName, (byte)0);
                        if (length == -1) length = modeName.Length;
                        string model = Encoding.ASCII.GetString(modeName, 0, length).Trim();

                        results.Add(new BoardInfo
                        {
                            Index = i,
                            ModelName = model,
                            AI = cardInfo.wAIChannels,
                            AO = cardInfo.wAOChannels,
                            DI = cardInfo.wDIPorts, // Note: Sometimes logic needs wDIPorts * 8 or similar depending on definition, keeping as is.
                            DO = cardInfo.wDOPorts,
                            DIO = cardInfo.wDIOPorts,
                            IsRunning = false
                        });
                    }
                }

                return results;
            }
            finally
            {
                _driverLock.Release();
            }
        }

        #endregion

        #region Status Management
        public bool IsBoardRunning(short boardNo)
        {
            lock (_taskLock)
            {
                if (_loops.TryGetValue((ushort)boardNo, out Task task))
                {
                    return !task.IsCompleted;
                }
                return false;
            }
        }

        private void RaiseStatusChange(ushort boardNo, BoardStatus status)
        {
            var boardInfo = AvailableBoards.FirstOrDefault(b => b.Index == boardNo);
            if (boardInfo != null)
            {
                boardInfo.IsRunning = (status == BoardStatus.Running);
            }
            Task.Run(() => BoardStatusChanged?.Invoke(this, new BoardStatusChangedEventArgs(boardNo, status)));
        }
        #endregion

        #region Driver Initialization
        public async Task<bool> InitializeDriverAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DaqSystemManager));

            await _driverLock.WaitAsync(cancellationToken);

            try
            {
                if (!_isDriverInitialized)
                {
                    ushort totalBoards = 0;
                    ushort rtn = UniDAQ.Ixud_DriverInit(ref totalBoards);

                    if (rtn != UniDAQ.Ixud_NoErr)
                    {
                        RaiseError("DriverInit", rtn, "Failed to initialize UniDAQ driver.");
                        return false;
                    }

                    _isDriverInitialized = true;
                    StatusMessage?.Invoke(this, $"Driver Initialized. Boards Found: {totalBoards}");

                    AvailableBoards.Clear();

                    for (ushort i = 0; i < totalBoards; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DetectBoard(i);
                    }
                }

                _driverRefCount++;
                return true;
            }
            catch (Exception ex)
            {
                RaiseError("DriverInit", 0, ex.Message);
                return false;
            }
            finally
            {
                _driverLock.Release();
            }
        }

        private void DetectBoard(ushort boardIndex)
        {
            byte[] modeName = new byte[32];
            ushort rtn = UniDAQ.Ixud_GetCardInfo(boardIndex, ref _sDeviceInfo[boardIndex], ref _sCardInfo[boardIndex], modeName);

            if (rtn != UniDAQ.Ixud_NoErr)
            {
                RaiseError("GetCardInfo", rtn, $"Failed to read Board {boardIndex}");
                return;
            }

            //string model = Encoding.Default.GetString(modeName).TrimEnd(new char[] {' ','\0' });
            int length = Array.IndexOf(modeName, (byte)0);
            if (length == -1) length = modeName.Length;
            string model = Encoding.ASCII.GetString(modeName, 0, length).Trim();

            AvailableBoards.Add(new BoardInfo
            {
                Index = boardIndex,
                ModelName = model,
                AI = _sCardInfo[boardIndex].wAIChannels,
                AO = _sCardInfo[boardIndex].wAOChannels,
                DI = _sCardInfo[boardIndex].wDIPorts,
                DO = _sCardInfo[boardIndex].wDOPorts,
                DIO = _sCardInfo[boardIndex].wDIOPorts,
                IsRunning = false
            });

            BoardDetected?.Invoke(this, new BoardDetectedEventArgs(boardIndex, model, _sCardInfo[boardIndex].wAIChannels, _sCardInfo[boardIndex].wDIPorts + _sCardInfo[boardIndex].wDIOPorts));
        }
        #endregion

        #region Controller Creation

        // UPDATED: Checks BoardInfo to strictly create only needed controllers
        public void CreateController(ushort boardNo, BoardInfo bInfo, bool isHighGain = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DaqSystemManager));

            // 1. Create Analog Controller ONLY if the board has AI channels
            if (bInfo.AI > 0)
            {
                if (!AnalogControllers.ContainsKey(boardNo))
                {
                    try
                    {
                        var ai = new AnalogInputManager(boardNo, isHighGain);
                        AnalogControllers.Add(boardNo, ai);
                        StatusMessage?.Invoke(this, $"Analog Controller created for Board {boardNo}.");
                    }
                    catch (Exception ex)
                    {
                        RaiseError("InitAnalog", 0, ex.Message);
                    }
                }
            }

            // 2. Create Digital Controller ONLY if the board has any Digital I/O ports
            if (bInfo.DI > 0 || bInfo.DO > 0 || bInfo.DIO > 0)
            {
                if (!DigitalControllers.ContainsKey(boardNo))
                {
                    try
                    {
                        var dio = new DigitalIoManager(boardNo);
                        DigitalControllers.Add(boardNo, dio);
                        StatusMessage?.Invoke(this, $"Digital Controller created for Board {boardNo}.");
                    }
                    catch (Exception ex)
                    {
                        RaiseError("InitDigital", 0, ex.Message);
                    }
                }
            }
        }

        #endregion

        #region Multi-Board Loop Management

        public void Start(ushort boardNo)
        {
            lock (_taskLock)
            {
                if (_loops.ContainsKey(boardNo) && !_loops[boardNo].IsCompleted)
                    return; // Already running

                // UPDATED: Safely retrieve controllers. They might be null if not supported by hardware.
                AnalogControllers.TryGetValue(boardNo, out AnalogInputManager analogCtrl);
                DigitalControllers.TryGetValue(boardNo, out DigitalIoManager digitalCtrl);

                // UPDATED: Only error if NEITHER exists.
                if (analogCtrl == null && digitalCtrl == null)
                {
                    RaiseError("SystemStart", 0, $"No initialized controllers found for Board {boardNo}. Check hardware capabilities.");
                    return;
                }

                if (_tokens.ContainsKey(boardNo))
                {
                    _tokens[boardNo].Dispose();
                    _tokens.Remove(boardNo);
                }

                var cts = new CancellationTokenSource();
                _tokens[boardNo] = cts;

                // Pass the potentially null controllers to the loop
                var task = Task.Factory.StartNew(
                    () => Mainloop(boardNo, analogCtrl, digitalCtrl, cts.Token),
                    cts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();

                _loops[boardNo] = task;

                StatusMessage?.Invoke(this, $"DAQ Board {boardNo} Started.");
                RaiseStatusChange(boardNo, BoardStatus.Running);
            }
        }

        public void Stop(ushort boardNo)
        {
            lock (_taskLock)
            {
                if (!_tokens.ContainsKey(boardNo)) return;
                try { _tokens[boardNo].Cancel(); } catch (ObjectDisposedException) { }
                StatusMessage?.Invoke(this, $"DAQ Board {boardNo} Stopping...");
            }
        }

        #endregion

        #region Main Loop (Worker)

        // UPDATED: Accepts nullable controllers to handle Digital-Only or Analog-Only boards
        private async Task Mainloop(
            ushort boardNo,
            AnalogInputManager analog, // Can be null
            DigitalIoManager digital,  // Can be null
            CancellationToken token)
        {
            int errorCount = 0;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_disposed) break;

                    // 1. Process Analog (If Supported)
                    if (analog != null)
                    {
                        analog.ReadSingleCycle();
                    }

                    // 2. Process Digital (If Supported)
                    if (digital != null)
                    {
                        digital.ProcessInputs();
                        digital.ProcessOutputs();
                    }

                    errorCount = 0;

                    // Small delay
                    await Task.Delay(5, token);
                }
            }
            catch (OperationCanceledException) { /* Graceful exit */ }
            catch (ObjectDisposedException) { /* Disposed */ }
            catch (Exception ex)
            {
                errorCount++;
                RaiseError($"MainLoop[{boardNo}]", 0, ex.Message);

                if (MaxErrorRetries != 0 && errorCount > MaxErrorRetries)
                {
                    RaiseError($"MainLoop[{boardNo}]", 0, "Max retries reached. Stopping loop.");
                }
                else
                {
                    try { await Task.Delay(RetryDelayMs, token); } catch { }
                }
            }
            finally
            {
                RaiseStatusChange(boardNo, BoardStatus.Stopped);
                StatusMessage?.Invoke(this, $"Loop for Board {boardNo} finalized.");
            }
        }

        #endregion

        #region Disposal
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                lock (_taskLock)
                {
                    foreach (var kv in _tokens) try { kv.Value.Cancel(); } catch { }
                }

                var activeTasks = _loops.Values.Where(t => !t.IsCompleted).ToArray();
                if (activeTasks.Length > 0)
                {
                    try { Task.WaitAll(activeTasks, 3000); } catch { }
                }

                foreach (var ai in AnalogControllers.Values) ai.Dispose();
                foreach (var dio in DigitalControllers.Values) dio.Dispose();

                AnalogControllers.Clear();
                DigitalControllers.Clear();
                _tokens.Clear();
                _loops.Clear();

                _driverLock.Wait();
                try
                {
                    _driverRefCount--;
                    if (_driverRefCount <= 0 && _isDriverInitialized)
                    {
                        UniDAQ.Ixud_DriverClose();
                        _isDriverInitialized = false;
                        _driverRefCount = 0;
                        StatusMessage?.Invoke(this, "Driver Closed safely.");
                    }
                }
                finally
                {
                    _driverLock.Release();
                    _driverLock.Dispose();
                }
            }
        }
        #endregion

        #region Helpers
        private void RaiseError(string source, ushort code, string message)
        {
            Task.Run(() => ErrorOccurred?.Invoke(this, new DaqErrorEventArgs(source, code, message)));
        }
        #endregion
    }
}
