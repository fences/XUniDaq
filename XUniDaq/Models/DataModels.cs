using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace XModels
{

    #region AI Enum & Helper Types

    /// <summary>
    /// Standard UniDAQ Voltage Ranges mapped to their Config Codes.
    /// Use this enum in AddChannel().
    /// </summary>
    public enum VoltageRange : ushort
    {
        [Description("Bipolar ±10V")] Bipolar_10V = 0,
        [Description("Bipolar ±20V")] Bipolar_20V = 23,
        [Description("Bipolar ±5V")] Bipolar_5V = 1,
        [Description("Bipolar ±2.5V")] Bipolar_2_5V = 2,
        [Description("Bipolar ±1.25V")] Bipolar_1_25V = 3,
        [Description("Bipolar ±0.625V")] Bipolar_0_625V = 4,
        [Description("Bipolar ±0.3125V")] Bipolar_0_3125V = 5,
        [Description("Bipolar ±0.5V")] Bipolar_0_5V = 6,
        [Description("Bipolar ±0.05V")] Bipolar_0_05V = 7,
        [Description("Bipolar ±0.005V")] Bipolar_0_005V = 8,
        [Description("Bipolar ±1V")] Bipolar_1V = 9,
        [Description("Bipolar ±0.1V")] Bipolar_0_1V = 10,
        [Description("Bipolar ±0.01V")] Bipolar_0_01V = 11,
        [Description("Bipolar ±0.001V")] Bipolar_0_001V = 12,
        [Description("Unipolar 0–20V")] Unipolar_20V = 13,
        [Description("Unipolar 0–10V")] Unipolar_10V = 14,
        [Description("Unipolar 0–5V")] Unipolar_5V = 15,
        [Description("Unipolar 0–2.5V")] Unipolar_2_5V = 16,
        [Description("Unipolar 0–1.25V")] Unipolar_1_25V = 17,
        [Description("Unipolar 0–0.625V")] Unipolar_0_625V = 18,
        [Description("Unipolar 0–1V")] Unipolar_1V = 19,
        [Description("Unipolar 0–0.1V")] Unipolar_0_1V = 20,
        [Description("Unipolar 0–0.01V")] Unipolar_0_01V = 21,
        [Description("Unipolar 0–0.001V")] Unipolar_0_001V = 22
    }

    public static class EnumExtensions
    {
        // Helper to get the Description string for UI (optional usage)
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }

    public enum AnalogState { Stopped = 0, Running = 1 }

    public class AnalogInputChannel
    {
        public string Name { get; }
        public int Index { get; }
        public ushort ConfigCode { get; }
        public double[] Regression { get; set; }
        public float Zero { get; set; }
        public SimpleMovingAverage Filter { get; private set; }

        public AnalogInputChannel(string name, int index, ushort configCode, double[] regression, float zero)
        {
            Name = name;
            Index = index;
            ConfigCode = configCode;
            Regression = regression;
            Zero = zero;
        }

        public void SetFilter(int windowSize) => Filter = new SimpleMovingAverage(windowSize);
    }

    public class SimpleMovingAverage
    {
        private readonly float[] _buffer;
        private int _ptr;
        private float _sum;
        private int _count;

        public SimpleMovingAverage(int windowSize)
        {
            if (windowSize > 1)
                _buffer = new float[windowSize];
            else
                _buffer = null;
        }

        public float Next(float val)
        {
            if (_buffer == null)
                return val;
            else
            {
                _sum -= _buffer[_ptr];
                _sum += val;
                _buffer[_ptr] = val;
                _ptr = (_ptr + 1) % _buffer.Length;
                if (_count < _buffer.Length) _count++;
                return _sum / _count;
            }
        }
    }

    public class AnalogDataEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public int ChannelIndex { get; }
        public float[] Data { get; }
        public float[] RawData { get; }
        public AnalogDataEventArgs(string name, int idx, float[] data, float[] raw)
        {
            ChannelName = name; ChannelIndex = idx; Data = data; RawData = raw;
        }
    }

    public class AnalogMatrix
    {
        public float[] DataMatrix { get; set; }
        public float[] RawDataMatrix { get; set; }
    }

    public class AnalogMultiChannelDataEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public IReadOnlyList<AnalogInputChannel> Channels { get; }
        public float[,] DataMatrix { get; }
        public float[,] RawDataMatrix { get; }

        private readonly Dictionary<string, int> _channelNameIndexMap;

        public AnalogMultiChannelDataEventArgs(
            IReadOnlyList<AnalogInputChannel> channels,
            float[,] dataMatrix,
            float[,] rawDataMatrix)
        {
            Channels = channels ?? throw new ArgumentNullException(nameof(channels));
            DataMatrix = dataMatrix ?? throw new ArgumentNullException(nameof(dataMatrix));
            RawDataMatrix = rawDataMatrix ?? throw new ArgumentNullException(nameof(rawDataMatrix));
            Timestamp = DateTime.Now;

            _channelNameIndexMap = new Dictionary<string, int>(channels.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < channels.Count; i++)
            {
                _channelNameIndexMap[channels[i].Name] = i;
            }
        }

        public AnalogMatrix GetChannelData(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= Channels.Count)
                throw new ArgumentOutOfRangeException(nameof(channelIndex));

            int dataCount = DataMatrix.GetLength(0);
            var matrix = new AnalogMatrix
            {
                DataMatrix = new float[dataCount],
                RawDataMatrix = new float[dataCount]
            };

            for (int i = 0; i < dataCount; i++)
            {
                matrix.DataMatrix[i] = DataMatrix[i, channelIndex];
                matrix.RawDataMatrix[i] = RawDataMatrix[i, channelIndex];
            }
            return matrix;
        }

        public AnalogMatrix GetChannelData(string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");

            return GetChannelData(index);
        }

        public float GetValue(int sampleIndex, string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");
            return DataMatrix[sampleIndex, index];
        }

        public float GetRawValue(int sampleIndex, string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");
            return RawDataMatrix[sampleIndex, index];
        }

        public float GetValue(int sampleIndex, int channelIndex) => DataMatrix[sampleIndex, channelIndex];
        public float GetRawValue(int sampleIndex, int channelIndex) => RawDataMatrix[sampleIndex, channelIndex];
    }

    public static class UniDaqError
    {
         public static readonly Dictionary<int, string> UniDaqErrors = new Dictionary<int, string>{
    { 0, "Correct" },
    { 1, "Open driver error" },
    { 2, "Plug & Play error" },
    { 3, "The driver was not open" },
    { 4, "Receive driver version error" },
    { 5, "Board number error" },
    { 6, "No board found" },
    { 7, "Board Mapping error" },
    { 8, "Digital input/output mode setting error" },
    { 9, "Invalid address" },
    { 10, "Invalid size" },
    { 11, "Invalid port number" },
    { 12, "This board model is not supported" },
    { 13, "This function is not supported" },
    { 14, "Invalid channel number" },
    { 15, "Invalid value" },
    { 16, "Invalid mode" },
    { 17, "Timeout while receiving analog input status" },
    { 18, "Timeout error" },
    { 19, "Configuration code table index not found" },
    { 20, "ADC controller timeout" },
    { 21, "PCI table index not found" },
    { 22, "Invalid setting value" },
    { 23, "Memory allocation error" },
    { 24, "Interrupt event installation error" },
    { 25, "Interrupt IRQ installation error" },
    { 26, "Interrupt IRQ removal error" },
    { 27, "Error clearing interrupt count" },
    { 28, "System buffer retrieval error" },
    { 29, "Event creation error" },
    { 30, "Resolution not supported" },
    { 31, "Thread creation error" },
    { 32, "Thread timeout error" },
    { 33, "FIFO overflow error" },
    { 34, "FIFO timeout error" },
    { 35, "Get interrupt installation status" },
    { 36, "Get system buffer status" },
    { 37, "Set buffer count error" },
    { 38, "Set buffer info error" },
    { 39, "Card ID not found" },
    { 40, "Event thread error" },
    { 41, "Auto-create event error" },
    { 42, "Register thread error" },
    { 43, "Search event error" },
    { 44, "FIFO reset error" },
    { 45, "Invalid EEPROM block" },
    { 46, "Invalid EEPROM address" },
    { 47, "Acquire spin lock error" },
    { 48, "Release spin lock error" },
    { 49, "Analog input setting error" },
    { 50, "Invalid channel number" },
    { 51, "Invalid model number" },
    { 52, "Map address setting error" },
    { 53, "Map address releasing error" },
    { 54, "Invalid memory offset" },
    { 55, "Shared memory open failed" },
    { 56, "Invalid data count" },
    { 57, "EEPROM writing error" },
    { 58, "CardIO error" },
    { 59, "MemoryIO error" },
    { 60, "Set scan channel error" },
    { 61, "Set scan config error" },
    { 62, "Get MMIO map status" }
};
    }

    public class AnalogErrorEventArgs : EventArgs
    {
        public string Source { get; }
        public int ChannelIndex { get; }
        public int ErrorCode { get; }
        public string Message { get; }
        public AnalogErrorEventArgs(string src, int idx, int code, string msg)
        {
            Source = src; ChannelIndex = idx; ErrorCode = code; Message = msg;
        }


    }
    #endregion

    #region DIO Enum $ Helpr Types



    public enum DaqState
    {
        Idle,
        Running,
        Stopped
    }

    // ----------------------------------------------------
    // Digital Channel Models
    // ----------------------------------------------------
    public abstract class DigitalChannelBase
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public ushort Port { get; set; }
        public ushort BoardNo { get; set; }

        protected DigitalChannelBase(string name, int index, ushort port, ushort boardNo)
        {
            Name = name;
            Index = index;
            Port = port;
            BoardNo = boardNo;
        }
    }

    public class DigitalInputChannel : DigitalChannelBase
    {
        public bool Invert { get; set; }
        public bool State { get; set; }

        public DigitalInputChannel(string name, int index, ushort port, ushort boardNo, bool invert = false)
            : base(name, index, port, boardNo)
        {
            Invert = invert;
        }
    }

    public class DigitalOutputChannel : DigitalChannelBase
    {
        public bool State { get; set; }

        public DigitalOutputChannel(string name, int index, ushort port, ushort boardNo)
            : base(name, index, port, boardNo) { }
    }

    // ----------------------------------------------------
    // Digital Helpers
    // ----------------------------------------------------
    internal class OutputCommand
    {
        public DigitalOutputChannel Channel { get; set; }
        public bool Value { get; set; }
        public int Priority { get; set; }
    }

    internal class PriorityComparer : IComparer<OutputCommand>
    {
        public int Compare(OutputCommand x, OutputCommand y)
        {
            return y.Priority.CompareTo(x.Priority);
        }
    }

    // ----------------------------------------------------
    // Digital Events
    // ----------------------------------------------------


    public class DigitalFrameEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public Dictionary<string, bool> CurrentStates { get; }
        public List<string> ChangedChannels { get; }

        public DigitalFrameEventArgs(Dictionary<string, bool> currentStates, List<string> changedChannels)
        {
            CurrentStates = currentStates;
            ChangedChannels = changedChannels;
            Timestamp = DateTime.Now;
        }
    }

    public class DigitalErrorEventArgs : EventArgs
    {
        public string Source { get; }
        public ushort ErrorCode { get; }
        public string Message { get; }

        public DigitalErrorEventArgs(string src, ushort code, string msg)
        {
            Source = src;
            ErrorCode = code;
            Message = msg;
        }
    }



    #endregion
}

