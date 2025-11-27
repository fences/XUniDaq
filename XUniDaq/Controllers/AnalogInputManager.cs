using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UniDAQ_Ns;
using XModels;

// using XUniDaq.Models; // Enable this if models are in a separate namespace

namespace IcpDas.Daq.Analog
{
    /// <summary>
    /// Manages analog input operations for a specific DAQ board.
    /// Operates as a passive worker controlled by the DaqSystemManager.
    /// </summary>
    public class AnalogInputManager : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when valid data is received and processed from multiple channels.
        /// </summary>
        public event EventHandler<AnalogMultiChannelDataEventArgs> MultiChannelDataReceived;

        /// <summary>
        /// Fired when a hardware or processing error occurs. 
        /// Note: Exceptions are also thrown to the main manager for control flow.
        /// </summary>
        public event EventHandler<AnalogErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// Fired when a read cycle completes, providing the elapsed time in milliseconds.
        /// </summary>
        public event EventHandler<long> ReadCycleCompleted;

        #endregion

        #region Fields & Properties

        private const int PARALLEL_THRESHOLD = 1000;

        // Thread-safe lock for modifying channel lists while reading
        private readonly ReaderWriterLockSlim _channelLock = new ReaderWriterLockSlim();
        private readonly List<AnalogInputChannel> _channels = new List<AnalogInputChannel>();

        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// The hardware board index assigned by the driver.
        /// </summary>
        public ushort BoardNo { get; }

        /// <summary>
        /// Card configuration type (e.g., 0 for Normal, 1 for HighGain).
        /// </summary>
        public ushort CardType { get; set; }

        /// <summary>
        /// Sampling rate in Hz.
        /// </summary>
        public float SamplingRate { get; set; } = 1000f;

        /// <summary>
        /// Number of samples to read per channel per cycle.
        /// </summary>
        public uint DataCount { get; set; } = 256;

        /// <summary>
        /// Enables parallel processing for data calculations (useful for high channel counts).
        /// </summary>
        public bool UseParallel { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the AnalogInputManager.
        /// </summary>
        /// <param name="boardNo">The board index.</param>
        /// <param name="isHighGain">Set to true if the board is in high-gain mode.</param>
        public AnalogInputManager(ushort boardNo, bool isHighGain = false)
        {
            BoardNo = boardNo;
            CardType = isHighGain ? (ushort)1 : (ushort)0;
            InitializeCard();
        }

        /// <summary>
        /// Gets the list of currently configured channels.
        /// </summary>
        public IReadOnlyList<AnalogInputChannel> Channels => _channels;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new analog channel configuration.
        /// </summary>
        /// <param name="name">User-friendly name.</param>
        /// <param name="index">Physical channel index.</param>
        /// <param name="range">Voltage range configuration.</param>
        /// <param name="movingAverageWindow">Window size for filtering (0 to disable).</param>
        /// <param name="regressionCoeffs">Polynomial coefficients for calibration.</param>
        /// <param name="zeroOffset">Zero offset adjustment.</param>
        public void AddChannel(string name, int index, VoltageRange range,
            int movingAverageWindow = 0, double[] regressionCoeffs = null, float zeroOffset = 0)
        {
            _channelLock.EnterWriteLock();
            try
            {
                ushort configCode = (ushort)range;
                var channel = new AnalogInputChannel(name, index, configCode, regressionCoeffs, zeroOffset);

                if (movingAverageWindow > 1)
                    channel.SetFilter(movingAverageWindow);

                _channels.Add(channel);
            }
            finally { _channelLock.ExitWriteLock(); }
        }

        /// <summary>
        /// Clears all configured channels.
        /// </summary>
        public void ClearAllChannels()
        {
            _channelLock.EnterWriteLock();
            try { _channels.Clear(); }
            finally { _channelLock.ExitWriteLock(); }
        }


        public void UpdateZero(string AiName, float Zero)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Name == AiName);
                ch.Zero = Zero;

            }
            finally { _channelLock.ExitWriteLock(); }
        }
        public void UpdateZero(int AiIndex, float Zero)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Index == AiIndex);
                ch.Zero = Zero;

            }
            finally { _channelLock.ExitWriteLock(); }
        }

        public void UpdateRegression(string AiName, double[] Regression)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Name == AiName);
                ch.Regression = Regression;

            }
            finally { _channelLock.ExitWriteLock(); }
        }
        public void UpdateRegression(int AiIndex, double[] Regression)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Index == AiIndex);
                ch.Regression = Regression;

            }
            finally { _channelLock.ExitWriteLock(); }
        }
        public void UpdateFilter(string AiName, int WindowSize)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Name == AiName);
                ch.SetFilter(WindowSize);   

            }
            finally { _channelLock.ExitWriteLock(); }
        }
        public void UpdateFilter(int AiIndex, int WindowSize)
        {
            _channelLock.EnterWriteLock();
            try
            {
                var ch = _channels.FirstOrDefault(c => c.Index == AiIndex);
                ch.SetFilter(WindowSize);

            }
            finally { _channelLock.ExitWriteLock(); }
        }



        #endregion

        #region Core Logic (Single Cycle)

        /// <summary>
        /// Configures the hardware board initially.
        /// </summary>
        private void InitializeCard()
        {
            // Initialization is done once. If it fails, the object creation should fail
            if (BoardNo < 0 || BoardNo > 15)
            {
                throw new Exception($"Analog Init Failed on Board {BoardNo}.");
            }
            else
            {
                ushort rtn = UniDAQ.Ixud_ConfigAI(BoardNo, 2, 2048, CardType, 0);

                if (rtn == UniDAQ.Ixud_NoErr)
                {
                    _isInitialized = true;
                }
                else
                {
                    _isInitialized = false;
                    // Throw exception so DaqManager knows initialization failed immediately
                    throw new Exception($"Analog Init Failed on Board {BoardNo}. Code: {rtn}");
                }
            }
        }



        /// <summary>
        /// Executes a single hardware read cycle (Start Scan -> Get Buffer -> Stop).
        /// This method is called by DaqSystemManager inside the main loop.
        /// </summary>
        /// <exception cref="Exception">Thrown if hardware operations fail, triggering a system restart logic in the Manager.</exception>
        public void ReadSingleCycle()
        {
            if (!_isInitialized) throw new InvalidOperationException("Analog board not initialized.");

            ushort[] chList, confList;
            AnalogInputChannel[] channelsSnapshot;

            // 1. Snapshot channel configurations (Thread Safe)
            _channelLock.EnterReadLock();
            try
            {
                if (_channels.Count == 0) return; // No channels configured, do nothing

                chList = _channels.Select(c => (ushort)c.Index).ToArray();
                confList = _channels.Select(c => c.ConfigCode).ToArray();
                channelsSnapshot = _channels.ToArray();
            }
            finally { _channelLock.ExitReadLock(); }

            float[] flatBuffer = new float[DataCount * chList.Length];
            float[,] matrixBuffer = new float[DataCount, chList.Length];
            float[,] rawMatrixBuffer = new float[DataCount, chList.Length];

            var sw = Stopwatch.StartNew();

            // 2. Hardware Operations
            // Note: We do not catch exceptions here (or we rethrow them) so DaqManager detects the failure.
            if (PerformHardwareOperations(chList, confList, flatBuffer))
            {
                // 3. Data Processing
                bool parallel = UseParallel && DataCount > PARALLEL_THRESHOLD;
                ProcessMultiChannel(channelsSnapshot, flatBuffer, matrixBuffer, rawMatrixBuffer, parallel);

                sw.Stop();
                ReadCycleCompleted?.Invoke(this, sw.ElapsedMilliseconds);
            }
            else
            {
                // If hardware operation returned false (usually HandleUniDaqError throws before reaching here)
                throw new Exception("Hardware operation returned false status.");
            }
        }

        /// <summary>
        /// Performs the low-level UniDAQ API calls.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PerformHardwareOperations(ushort[] channelList, ushort[] configList, float[] flatBuffer)
        {
            // Start Scan
            ushort rtn = UniDAQ.Ixud_StartAIScan(BoardNo, (ushort)channelList.Length, channelList, configList, SamplingRate, DataCount);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                HandleUniDaqError("StartAIScan", rtn); // This throws exception
                return false;
            }

            // Get Buffer
            rtn = UniDAQ.Ixud_GetAIBuffer(BoardNo, DataCount * (uint)channelList.Length, flatBuffer);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                UniDAQ.Ixud_StopAI(BoardNo); // Try to stop cleanly
                HandleUniDaqError("GetAIBuffer", rtn);
                return false;
            }

            // Stop Scan
            rtn = UniDAQ.Ixud_StopAI(BoardNo);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                HandleUniDaqError("StopAI", rtn);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles errors from the UniDAQ driver by firing an event and throwing an exception.
        /// </summary>
        private void HandleUniDaqError(string method, ushort code)
        {
            // Assuming UniDaqErrors is a dictionary mapping codes to strings
            string msg = UniDaqError.UniDaqErrors.TryGetValue(code, out var m) ? m : "Unknown Error";
            string fullMsg = $"[{method}] Error {code}: {msg}";

            // Fire event for UI/Logging
            ErrorOccurred?.Invoke(this, new AnalogErrorEventArgs(method, -1, code, fullMsg));

            // Throw exception for Central Manager Logic (to trigger retry mechanism)
            throw new Exception(fullMsg);
        }

        /// <summary>
        /// Processes raw buffer data into matrix format, applying filters and regression.
        /// </summary>
        private void ProcessMultiChannel(AnalogInputChannel[] channels, float[] flatBuffer,
            float[,] matrix, float[,] rawMatrix, bool parallel)
        {
            int chCount = channels.Length;

            // Define the body of the loop for processing
            Action<int> body = i =>
            {
                for (int ch = 0; ch < chCount; ch++)
                {
                    float raw = flatBuffer[i * chCount + ch];
                    var channel = channels[ch];

                    // Apply Moving Average Filter
                    float processed = channel.Filter != null ? channel.Filter.Next(raw) : raw;

                    rawMatrix[i, ch] = processed;

                    // Apply Linear Regression and Zero Offset
                    matrix[i, ch] = Fit(processed, channel.Regression) - channel.Zero;
                }
            };

            if (parallel) Parallel.For(0, (int)DataCount, body);
            else for (int i = 0; i < DataCount; i++) body(i);

            MultiChannelDataReceived?.Invoke(this, new AnalogMultiChannelDataEventArgs(channels, matrix, rawMatrix));
        }

        /// <summary>
        /// Calculates the polynomial value using Horner's method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fit(double value, double[] coeffs)
        {
            if (coeffs == null || coeffs.Length == 0) return (float)value;

            // Horner's method for efficient polynomial evaluation
            double result = coeffs[coeffs.Length - 1];
            for (int i = coeffs.Length - 2; i >= 0; i--)
                result = result * value + coeffs[i];
            return (float)result;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _channelLock?.Dispose();
                if (_isInitialized)
                {
                    // Ensure hardware is stopped upon disposal
                    UniDAQ.Ixud_StopAI(BoardNo);
                }
            }
            _disposed = true;
        }

        #endregion
    }




    /// <summary>
    /// Simple Moving Average implementation.
    /// </summary>
    public class SimpleMovingAverage
    {
        private readonly int _k;
        private readonly float[] _values;
        private int _index = 0;
        private float _sum = 0;
        private int _count = 0;

        public SimpleMovingAverage(int k)
        {
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
            _k = k;
            _values = new float[k];
        }

        public float Next(float nextVal)
        {
            _sum -= _values[_index];
            _sum += nextVal;
            _values[_index] = nextVal;
            _index = (_index + 1) % _k;
            if (_count < _k) _count++;
            return _sum / _count;
        }
    }

}
