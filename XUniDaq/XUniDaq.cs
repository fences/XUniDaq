using DevicesContext.Controllers;
using IcpDas.Daq.Analog;
using IcpDas.Daq.System;
using IcpDas.Daq.WinForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml.Linq;
using UiNew;
using XModels;

namespace IcpDas.Daq.WinForms
{
   
    /// <summary>
    /// ICPDAS UniDAQ Controller Component.
    /// Manages a specific board index via the shared DaqSystemManager.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("AiDataReceived")]
    [DefaultProperty("BoardIndex")]
    [Description("Interface for IcpDas UniDAQ Boards. Manages configuration and data events.")]
    [ToolboxBitmap(typeof(XUniDaq), "XUniDaq.xdaq16.bmp")]
    public partial class XUniDaq : Component, ISupportInitialize, ICustomTypeDescriptor
    {
        #region Fields

        private SynchronizationContext _uiContext;

        // Configuration Backing Fields
        private short _boardIndex = -1;
        private string _boardName = "Not Selected"; // Default text
        private float _samplingRate = 10000f;
        private uint _dataCount = 200;
        private bool _autoStart = false;
        private bool _isHighGain = false;
     

        private Collection<AnalogChannelConfig> _AichannelConfigs;
        private Collection<DigitalInputChannelConfig> _DichannelConfigs;
        private Collection<DigitalOutputChannelConfig> _DochannelConfigs;

        // Runtime References to specific controllers for THIS board
        private AnalogInputManager _currentAiController;
        private DigitalIoManager _currentDioController;

        #endregion

        #region Constructor

        public XUniDaq()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                _ = InitializeDesignerDriverAsync();
            }
        }

        public XUniDaq(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                _ = InitializeDesignerDriverAsync();
            }
        }

        private async Task InitializeDesignerDriverAsync()
        {
            try
            {
                // Ensure the driver is loaded so we can fetch board names
                bool success = await DaqServiceLocator.Instance.InitializeDriverAsync();

                if (success)
                {
                    // Once driver is loaded, try to find the name for the currently set index
                    UpdateBoardNameInternal();

                    // Refresh property grid to show the name
                    TypeDescriptor.Refresh(this);
                }
            }
            catch
            {
                // Design-time suppression
            }
        }

        private void InitializeComponent()
        {
            _uiContext = SynchronizationContext.Current;
            _AichannelConfigs = new Collection<AnalogChannelConfig>();
            _DichannelConfigs = new Collection<DigitalInputChannelConfig>();
            _DochannelConfigs = new Collection<DigitalOutputChannelConfig>();
        }

        #endregion

        #region Properties (Design Time)

        [Category("DAQ Configuration")]
        [Description("Collection of Analog Input channels.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<AnalogChannelConfig> AiChannels => _AichannelConfigs;

        [Category("DAQ Configuration")]
        [Description("Collection of Digital Input channels.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<DigitalInputChannelConfig> DiChannels => _DichannelConfigs;

        [Category("DAQ Configuration")]
        [Description("Collection of Digital Output channels.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<DigitalOutputChannelConfig> DoChannels => _DochannelConfigs;

        [DefaultValue(typeof(short), "-1")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("DAQ Configuration")]
        [Description("Select the target DAQ board. Click (...) to scan or select from list.")]
        [Editor(typeof(BoardSelectorEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(BoardDisplayConverter))]
        [RefreshProperties(RefreshProperties.All)]
        public short BoardIndex
        {
            get => _boardIndex;
            set
            {
                if (_boardIndex != value)
                {
                    _boardIndex = value;

                    // Try to update name immediately if driver is available
                    UpdateBoardNameInternal();

                    if (DesignMode)
                    {
                        TypeDescriptor.Refresh(this);
                    }
                    else
                    {
                        // Runtime: Re-initialize if index changes
                        InitializeAndStartAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        [Category("DAQ Configuration")]
        [Description("The model name of the selected board (Read Only).")]
        [ReadOnly(true)] // Prevent user from typing manually
        public string BoardName
        {
            get => _boardName;
        }

        [Category("DAQ Configuration")]
        [Description("Sampling frequency (Hz).")]
        [DefaultValue(10000f)]
        public float SamplingRate
        {
            get => _samplingRate;
            set => _samplingRate = value;
        }

        [Category("DAQ Configuration")]
        [Description("Number of samples per block.")]
        [DefaultValue(200)]
        public uint DataCount
        {
            get => _dataCount;
            set => _dataCount = value;
        }

        [Category("DAQ Configuration")]
        [Description("Enable High Gain mode (if supported).")]
        [DefaultValue(false)]
        public bool HighGain
        {
            get => _isHighGain;
            set => _isHighGain = value;
        }

        /*
        [Category("Behavior")]
        [Description("Start acquisition automatically after initialization.")]
        [DefaultValue(false)]
        public bool AutoStart
        {
            get => _autoStart;
            set => _autoStart = value;
        }
        */
        #endregion

        #region Internal Helpers

        private void UpdateBoardNameInternal()
        {
            // 1. Check for valid index
            if (_boardIndex < 0)
            {
                _boardName = "Not Selected";
                return;
            }

            var sys = DaqServiceLocator.Instance;

            // 2. We can only get the name if the driver has been scanned
            if (sys.IsDriverInitialized && sys.AvailableBoards != null)
            {
                var board = sys.AvailableBoards.FirstOrDefault(b => b.Index == _boardIndex);
                if (board != null)
                {
                    _boardName = board.ModelName;
                }
                else
                {
                    _boardName = $"Unknown (ID: {_boardIndex})";
                }
            }
            else
            {
                // Driver not ready yet (e.g. first load in designer before async init finishes)
                _boardName = "Loading...";
            }
        }

        #endregion

        #region Events

        [Category("DAQ Events")]
        public event EventHandler<AnalogMultiChannelDataEventArgs> AiDataReceived;

        [Category("DAQ Events")]
        public event EventHandler<DigitalFrameEventArgs> DiDataReceived;

        [Category("DAQ Events")]
        public event EventHandler<DaqErrorEventArgs> ErrorOccurred;

        [Category("DAQ Events")]
        public event EventHandler<string> StatusUpdated;

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the driver, creates controllers for this board, and applies configuration.
        /// Call this in Form_Load.
        /// </summary>
        public async Task InitializeAndStartAsync()
        {
            if (DesignMode) return;

            try
            {
                ReportStatus("Initializing System...");

                var sys = DaqServiceLocator.Instance;

                // 1. Initialize Driver (if not already done by another control)
                if (!sys.IsDriverInitialized)
                {
                    bool success = await sys.InitializeDriverAsync();
                    if (!success) throw new Exception("Driver failed to initialize.");
                }

                // Update name for Runtime display
                UpdateBoardNameInternal();

                // 2. Create Controllers for this specific Board Index
                // This ensures AnalogControllers[_boardIndex] exists.
                var info = sys.AvailableBoards.FirstOrDefault(ab => ab.Index == _boardIndex);
                sys.CreateController((ushort)_boardIndex, info, _isHighGain);

                // 3. Retrieve References
                if (sys.AnalogControllers.TryGetValue((ushort)_boardIndex, out var ai))
                {
                    _currentAiController = ai;
                    ConfigureAnalog(_currentAiController);

                    // Wire Events
                    _currentAiController.MultiChannelDataReceived -= OnBackendAiReceived;
                    _currentAiController.MultiChannelDataReceived += OnBackendAiReceived;
                    _currentAiController.ErrorOccurred -= OnBackendAiError;
                    _currentAiController.ErrorOccurred += OnBackendAiError;
                }
                else
                {
                    ReportStatus($"Warning: No Analog controller created for Board {(ushort)_boardIndex}");
                }

                if (sys.DigitalControllers.TryGetValue((ushort)_boardIndex, out var dio))
                {
                    _currentDioController = dio;
                    ConfigureDigital(_currentDioController);

                    // Wire Events
                    _currentDioController.FrameReceived -= OnBackendDiReceived;
                    _currentDioController.FrameReceived += OnBackendDiReceived;
                    _currentDioController.ErrorOccurred -= OnBackendDiError;
                    _currentDioController.ErrorOccurred += OnBackendDiError;
                }

                ReportStatus("Configuration applied.");

                // 4. Auto Start
                if (_autoStart)
                {
                    Start();
                }
            }
            catch (Exception ex)
            {
                ReportStatus($"Init Error: {ex.Message}");
                ErrorOccurred?.Invoke(this, new DaqErrorEventArgs("XUniDaq", 0, ex.Message));
            }
        }

        /// <summary>
        /// Starts the acquisition loop for this board.
        /// </summary>
        public void Start()
        {
            if (_currentAiController != null)
            {
                _currentAiController.SamplingRate = _samplingRate;
                _currentAiController.DataCount = _dataCount;
            }

            DaqServiceLocator.Instance.Start((ushort)_boardIndex);
            ReportStatus($"Board {_boardIndex} Started.");
        }

        /// <summary>
        /// Updates params and starts.
        /// </summary>
        public void Start(float sampleRate, uint dataSize)
        {
            _samplingRate = sampleRate;
            _dataCount = dataSize;
            Start();
        }

        /// <summary>
        /// Stops the acquisition loop for this board.
        /// </summary>
        public void Stop()
        {
            DaqServiceLocator.Instance.Stop((ushort)_boardIndex);
            ReportStatus($"Board {_boardIndex} Stopped.");
        }

        /// <summary>
        /// Writes to a specific digital output.
        /// </summary>
        public void WriteDigitalPort(string name, bool value, int priority)
        {
            if (_currentDioController == null) return;
            _currentDioController.EnqueueOutput(name, value, priority);
        }

        // --- Manual Configuration Methods ---

        public void AddManualAiChannel(string name, int index, VoltageRange range, int movingAverageWindow = 0, double[] regressionCoeffs = null, float zeroOffset = 0)
        {
            // Add to design collection
            _AichannelConfigs.Add(new AnalogChannelConfig { ChannelName = name, PhysicalIndex = index, Range = range ,
                FilterWindowSize = movingAverageWindow, ZeroOffset = zeroOffset, Regression = regressionCoeffs});

            // Apply runtime
            if (_currentAiController != null)
                _currentAiController.AddChannel(name, index, range,movingAverageWindow, regressionCoeffs,zeroOffset);
        }

        public void AddManualDiChannel(string name, int port, int index, bool invert = false)
        {
            _DichannelConfigs.Add(new DigitalInputChannelConfig { ChannelName = name, PortIndex = port, PhysicalIndex = index, Invert = invert });

            if (_currentDioController != null)
                _currentDioController.AddInput(name, index, (ushort)port, invert);
        }

        public void AddManualDoChannel(string name, int port, int index)
        {
            _DochannelConfigs.Add(new DigitalOutputChannelConfig { ChannelName = name, PortIndex = port, PhysicalIndex = index });

            if (_currentDioController != null)
                _currentDioController.AddOutput(name, index, (ushort)port);
        }

        #endregion

        #region Internal Configuration Logic

        private void ConfigureAnalog(AnalogInputManager ai)
        {
            ai.ClearAllChannels();
            ai.SamplingRate = _samplingRate;
            ai.DataCount = _dataCount;

            foreach (var cfg in _AichannelConfigs)
            {
                ai.AddChannel(
                    cfg.ChannelName,
                    cfg.PhysicalIndex,
                    cfg.Range,
                    cfg.FilterWindowSize,
                    cfg.Regression,
                    cfg.ZeroOffset
                );


            }
        }

    

        public void UpdateRegression(string AiName, double[] RegressionValue)
        {
            _currentAiController.UpdateRegression(AiName, RegressionValue);
            var item =  this.AiChannels.FirstOrDefault(a=>a.ChannelName == AiName);
            if (item != null)
            { 
                item.Regression = RegressionValue;
            }

        }
        public void UpdateRegression(int AiIndex, double[] RegressionValue)
        {
            _currentAiController.UpdateRegression(AiIndex, RegressionValue);
            var item = this.AiChannels.FirstOrDefault(a => a.PhysicalIndex == AiIndex);
            if (item != null)
            {
                item.Regression = RegressionValue;
            }
        }
        public void UpdateZeroOffset(string AiName, float ZeroOffset)
        {
            _currentAiController.UpdateZero(AiName, ZeroOffset);
            var item = this.AiChannels.FirstOrDefault(a => a.ChannelName == AiName);
            if (item != null)
            {
                item.ZeroOffset = ZeroOffset;
            }
        }
        public void UpdateZeroOffset(int AiIndex, float ZeroOffset)
        {
            _currentAiController.UpdateZero(AiIndex, ZeroOffset);
            var item = this.AiChannels.FirstOrDefault(a => a.PhysicalIndex == AiIndex);
            if (item != null)
            {
                item.ZeroOffset = ZeroOffset;
            }
        }

        public void UpdateFilterWindow(string AiName, uint WindowSize)
        {
            _currentAiController.UpdateFilter(AiName, (int)WindowSize);
            var item = this.AiChannels.FirstOrDefault(a => a.ChannelName == AiName);
            if (item != null)
            {
                item.FilterWindowSize = (int)WindowSize;
            }
        }
        public void UpdateFilterWindow(int AiIndex, uint WindowSize)
        {
            _currentAiController.UpdateFilter(AiIndex, (int)WindowSize);
            var item = this.AiChannels.FirstOrDefault(a => a.PhysicalIndex == AiIndex);
            if (item != null)
            {
                item.FilterWindowSize = (int)WindowSize;
            }

        }

        private void ConfigureDigital(DigitalIoManager dio)
        {
            dio.ClearAllInput();
            dio.ClearAllOutput();

            foreach (var cfg in _DichannelConfigs)
            {
                dio.AddInput(cfg.ChannelName, cfg.PhysicalIndex, (ushort)cfg.PortIndex, cfg.Invert);
            }

            foreach (var cfg in _DochannelConfigs)
            {
                dio.AddOutput(cfg.ChannelName, cfg.PhysicalIndex, (ushort)cfg.PortIndex);
            }
        }

        #endregion

        #region Event Marshalling (Background -> UI Thread)

        private void OnBackendAiReceived(object sender, AnalogMultiChannelDataEventArgs e)
        {
            PostToUi(() => AiDataReceived?.Invoke(this, e));
        }

        private void OnBackendAiError(object sender, AnalogErrorEventArgs e)
        {
            var args = new DaqErrorEventArgs(e.Source, (ushort)e.ErrorCode, e.Message, _boardName);
            PostToUi(() => ErrorOccurred?.Invoke(this, args));
        }

        private void OnBackendDiReceived(object sender, DigitalFrameEventArgs e)
        {
            PostToUi(() => DiDataReceived?.Invoke(this, e));
        }

        private void OnBackendDiError(object sender, DigitalErrorEventArgs e)
        {
            var args = new DaqErrorEventArgs("Digital", 0, e.Message,_boardName);
            PostToUi(() => ErrorOccurred?.Invoke(this, args));
        }

        private void ReportStatus(string msg)
        {
            PostToUi(() => StatusUpdated?.Invoke(this, _boardName + "->" +  msg));
        }

        private void PostToUi(Action action)
        {
            if (_uiContext != null) _uiContext.Post(_ => action(), null);
            else action();
        }

        #endregion

        #region Component Lifecycle

        public void BeginInit() { /* Not used currently */ }
        public void EndInit()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {

                var task = InitializeAndStartAsync();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        var ex = t.Exception?.InnerException;
                        PostToUi(() => ErrorOccurred?.Invoke(this, new DaqErrorEventArgs("Init", 0, ex?.Message)));
                    }
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clean up subscriptions for this specific component
                if (_currentAiController != null)
                {
                    _currentAiController.MultiChannelDataReceived -= OnBackendAiReceived;
                    _currentAiController.ErrorOccurred -= OnBackendAiError;
                }
                if (_currentDioController != null)
                {
                    _currentDioController.FrameReceived -= OnBackendDiReceived;
                    _currentDioController.ErrorOccurred -= OnBackendDiError;
                }
            }
            base.Dispose(disposing);
        }


        #endregion

        #region Static Method 
        public static async Task<List<BoardInfo>> GetSystemBoardsAsync()
        {
            return await DaqSystemManager.GetAvailableBoardsStaticAsync();
        }
        #endregion

        #region ICustomTypeDescriptor Implementation (Logic to Hide Properties)

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
        public string GetClassName() => TypeDescriptor.GetClassName(this, true);
        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var baseProps = TypeDescriptor.GetProperties(this, attributes, true);
            var filteredProps = new List<PropertyDescriptor>();
            BoardInfo selectedBoardInfo = null;

            try
            {
                var sys = DaqServiceLocator.Instance;
                if (sys.IsDriverInitialized)
                {
                    selectedBoardInfo = sys.AvailableBoards.FirstOrDefault(b => b.Index == _boardIndex);
                }
            }
            catch { }

            foreach (PropertyDescriptor pd in baseProps)
            {
                if (selectedBoardInfo == null)
                {
                    filteredProps.Add(pd);
                    continue;
                }

                // Hide Analog properties if board has no AI
                if (pd.Name == nameof(AiChannels) && selectedBoardInfo.AI == 0) continue;
                if (pd.Name == nameof(HighGain) && selectedBoardInfo.AI == 0) continue;
                if (pd.Name == nameof(SamplingRate) && selectedBoardInfo.AI == 0) continue;
                if (pd.Name == nameof(DataCount) && selectedBoardInfo.AI == 0) continue;
                if (pd.Name == nameof(AiDataReceived) && selectedBoardInfo.AI == 0) continue;

                // Hide Digital properties if board has no DI/DO/DIO
                bool hasDig = (selectedBoardInfo.DI > 0 || selectedBoardInfo.DIO > 0 || selectedBoardInfo.DO > 0);

                if (pd.Name == nameof(DiChannels) && !hasDig) continue;
                if (pd.Name == nameof(DiDataReceived) && !hasDig) continue;
                if (pd.Name == nameof(DoChannels) && !hasDig) continue;

                filteredProps.Add(pd);
            }

            return new PropertyDescriptorCollection(filteredProps.ToArray());
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);
        }

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        #endregion
    }

    #region Global Service Locator (Singleton Wrapper)

    /// <summary>
    /// A simple singleton wrapper to hold the shared instance of DaqSystemManager.
    /// This ensures the driver is initialized only once for the whole application.
    /// </summary>
    public static class DaqServiceLocator
    {
        private static readonly Lazy<DaqSystemManager> _lazyInstance =
            new Lazy<DaqSystemManager>(() => new DaqSystemManager());

        public static DaqSystemManager Instance => _lazyInstance.Value;
    }

    #endregion

    #region Helper Classes (Design Time)

    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class AnalogChannelConfig
    {
        public override string ToString() => string.IsNullOrEmpty(ChannelName) ? $"Channel {PhysicalIndex}" : $"{ChannelName} (Idx:{PhysicalIndex})";

        [Category("Settings"), Description("Logical name (e.g., 'Pressure')."), DefaultValue("Ch0")]
        public string ChannelName { get; set; } = "Ch0";

        [Category("Hardware"), Description("Physical channel index (0, 1, 2...)."), DefaultValue(0)]
        public int PhysicalIndex { get; set; } = 0;

        [Category("Hardware"), Description("Input voltage range."), DefaultValue(VoltageRange.Bipolar_10V)]
        public VoltageRange Range { get; set; } = VoltageRange.Bipolar_10V;

        [Category("Signal Processing"), Description("Moving Average filter size (0=off)."), DefaultValue(0)]
        public int FilterWindowSize { get; set; } = 0;

        [Category("Zero Calibration"), Description("Zero drift correction."), DefaultValue(0f)]
        public float ZeroOffset { get; set; } = 0f;

        [Category("Regression Calibration"), Description("Calibration correction.")]
        public double[] Regression { get; set; } = null;
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class DigitalInputChannelConfig
    {
        public override string ToString() => string.IsNullOrEmpty(ChannelName) ? $"DI {PortIndex}-{PhysicalIndex}" : $"{ChannelName}";

        [Category("Settings"), DefaultValue("DI0")]
        public string ChannelName { get; set; } = "DI0";

        [Category("Hardware"), DefaultValue(0)]
        public int PhysicalIndex { get; set; } = 0;

        [Category("Hardware"), DefaultValue(0)]
        public int PortIndex { get; set; } = 0;

        [Category("Logic"), DefaultValue(false)]
        public bool Invert { get; set; } = false;
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    [Serializable]
    public class DigitalOutputChannelConfig
    {
        public override string ToString() => string.IsNullOrEmpty(ChannelName) ? $"DO {PortIndex}-{PhysicalIndex}" : $"{ChannelName}";

        [Category("Settings"), DefaultValue("DO0")]
        public string ChannelName { get; set; } = "DO0";

        [Category("Hardware"), DefaultValue(0)]
        public int PhysicalIndex { get; set; } = 0;

        [Category("Hardware"), DefaultValue(0)]
        public int PortIndex { get; set; } = 0;

        [Category("Priority"), DefaultValue(0)]
        public int Priority { get; set; } = 0;
    }

    #endregion
}

namespace UiNew
{
    #region Design Time Editor (Board Selector)

    public class BoardSelectorEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService edSvc = (IWindowsFormsEditorService)provider?.GetService(typeof(IWindowsFormsEditorService));
            if (edSvc != null)
            {
                short currentVal = value is short u ? u : (short)0;

                using (var form = new BoardSelectorDialog(currentVal))
                {
                    if (edSvc.ShowDialog(form) == DialogResult.OK)
                    {
                        return form.SelectedBoardIndex;
                    }
                }
            }
            return value;
        }
    }

    internal class BoardDisplayConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var sys = DaqServiceLocator.Instance;

            if (sys.IsDriverInitialized && sys.AvailableBoards.Any())
            {
                var indices = sys.AvailableBoards.Select(b => b.Index).ToList();
                return new StandardValuesCollection(indices);
            }

            return new StandardValuesCollection(new short[] { 0, 1 });
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is short index)
            {
                var sys = DaqServiceLocator.Instance;
                if (sys.IsDriverInitialized)
                {
                    var board = sys.AvailableBoards.FirstOrDefault(b => b.Index == index);
                    if (board != null)
                    {
                        return $"[{index}] {board.ModelName}";
                    }
                }
                return $"Board ID: {index} (Not Scanned)";
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string)) return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();

                if (short.TryParse(text, out short directResult))
                {
                    return directResult;
                }

                var bracketMatch = Regex.Match(text, @"^\[(\d+)\]");
                if (bracketMatch.Success)
                {
                    if (short.TryParse(bracketMatch.Groups[1].Value, out short bracketResult))
                    {
                        return bracketResult;
                    }
                }
                var idMatch = Regex.Match(text, @"Board ID:\s*(\d+)");
                if (idMatch.Success)
                {
                    if (short.TryParse(idMatch.Groups[1].Value, out short idResult))
                    {
                        return idResult;
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class BoardSelectorDialog : Form
    {
        public short SelectedBoardIndex { get; private set; }

        private DataGridView _grid;
        private Button _btnScan;
        private Button _btnCancel;
        private Label _lblStatus;

        public BoardSelectorDialog(short currentIndex)
        {
            SelectedBoardIndex = currentIndex;
            InitializeUi();
        }

        private void InitializeUi()
        {
            this.Text = "Select DAQ Board";
            this.Size = new Size(650, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            _btnScan = new Button { Text = "Scan Hardware", Location = new Point(12, 12), Size = new Size(120, 30) };
            _btnScan.Click += async (s, e) => await ScanBoardsAsync();

            _lblStatus = new Label { Text = "Ready.", Location = new Point(140, 18), AutoSize = true };

            _grid = new DataGridView
            {
                Location = new Point(12, 50),
                Size = new Size(610, 250),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _grid.Columns.Add("Idx", "ID");
            _grid.Columns.Add("Model", "Model");
            _grid.Columns.Add("AI", "AI");
            _grid.Columns.Add("AO", "AO");
            _grid.Columns.Add("DI", "DI");
            _grid.Columns.Add("DO", "DO");

            _grid.CellDoubleClick += (s, e) => SelectAndClose();

            var btnOk = new Button { Text = "Select", DialogResult = DialogResult.OK, Location = new Point(460, 315), Size = new Size(80, 30) };
            btnOk.Click += (s, e) => SelectAndClose();

            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(545, 315), Size = new Size(80, 30) };

            this.Controls.AddRange(new Control[] { _btnScan, _lblStatus, _grid, btnOk, _btnCancel });
            this.AcceptButton = btnOk;
        }

        private void SelectAndClose()
        {
            if (_grid.SelectedRows.Count > 0)
            {
                if (short.TryParse(_grid.SelectedRows[0].Cells[0].Value?.ToString(), out short idx))
                {
                    SelectedBoardIndex = idx;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private async Task ScanBoardsAsync()
        {
            _btnScan.Enabled = false;
            _lblStatus.Text = "Scanning...";
            _grid.Rows.Clear();

            try
            {
                var sys = DaqServiceLocator.Instance;

                // Ensure driver is initialized
                if (!sys.IsDriverInitialized)
                {
                    await sys.InitializeDriverAsync();
                }

                var boards = sys.AvailableBoards;

                if (boards == null || boards.Count == 0)
                {
                    _lblStatus.Text = "No boards found.";
                    MessageBox.Show("No boards detected via UniDAQ driver.", "Scan Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    foreach (var b in boards)
                    {
                        _grid.Rows.Add(b.Index, b.ModelName, b.AI, b.AO, b.DI, b.DO);
                    }
                    _lblStatus.Text = $"Found {boards.Count} board(s).";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Error.";
                // Fallback for Design Time when DLL might fail loading
                _grid.Rows.Add(0, "SIMULATION (Error: " + ex.Message + ")", 16, 2, 16, 16);
            }
            finally
            {
                _btnScan.Enabled = true;
            }
        }
    }

    #endregion
}
