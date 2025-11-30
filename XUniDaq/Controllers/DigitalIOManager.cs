using System;
using System.Collections.Generic;
using System.Linq;

using UniDAQ_Ns;
using XModels;



namespace IcpDas.Daq.Digital
{
    public class DigitalIoManager : IDisposable
    {
        #region Events

        public event EventHandler<DigitalFrameEventArgs> FrameReceived; 
        public event EventHandler<DigitalErrorEventArgs> ErrorOccurred;
        #endregion

        #region Fields
        private readonly List<DigitalInputChannel> _inputs = new List<DigitalInputChannel>();
        private readonly List<DigitalOutputChannel> _outputs = new List<DigitalOutputChannel>();

        private readonly Dictionary<string, bool> _lastInputStates = new Dictionary<string, bool>();
        private readonly object _queueLock = new object();
        private readonly List<OutputCommand> _commandQueue = new List<OutputCommand>();
        #endregion

        #region Properties
        public ushort BoardNo { get; private set; }
        public bool IsConnected { get; private set; }
        #endregion

        #region Constructor
        public DigitalIoManager(ushort boardNo)
        {
            BoardNo = boardNo;
        }
        #endregion

        #region Configuration Methods
        public void AddInput(string name, int index, ushort port, bool invert = false)
        {
            if (_inputs.Any(x => x.Name == name)) 
                throw new ArgumentException("Duplicate Input Name");
            _inputs.Add(new DigitalInputChannel(name, index, port, BoardNo, invert));
        }

        public void ClearAllInput()
        {
            _inputs.Clear();
            _lastInputStates.Clear();
        }

        public void AddOutput(string name, int index, ushort port)
        {
            if (_outputs.Any(x => x.Name == name)) 
                throw new ArgumentException("Duplicate Output Name");
            _outputs.Add(new DigitalOutputChannel(name, index, port, BoardNo));
        }

        public void ClearAllOutput()
        {
            _outputs.Clear();
        }
        #endregion

        #region Output Enqueuing (Thread-Safe)
        public void EnqueueOutput(string name, bool value, int priority = 0)
        {
            var ch = _outputs.Find(x => x.Name == name);
            if (ch == null) return;

            var cmd = new OutputCommand() { Channel = ch, Value = value, Priority = priority };

            lock (_queueLock)
            {
                _commandQueue.Add(cmd);
                _commandQueue.Sort(new PriorityComparer()); 
            }
        }


        public void SetAllOutputsImmediate(bool state)
        {
            lock (_queueLock) { _commandQueue.Clear(); }

            var outputsByPort = _outputs.GroupBy(x => x.Port);

            foreach (var group in outputsByPort)
            {
                ushort port = group.Key;
                uint currentPortValue = 0;

                foreach (var ch in group)
                {
                    if (state) currentPortValue |= (1u << ch.Index);
                    else currentPortValue &= ~(1u << ch.Index);
                    ch.State = state;
                }

                ushort rtn = UniDAQ.Ixud_WriteDO(BoardNo, port, currentPortValue);
                if (rtn != UniDAQ.Ixud_NoErr)
                {
                    HandleUniDaqError("SetAllOutputs", rtn); // Throws Exception
                }
            }
        }
        #endregion

        #region Core Cycle Methods (Called by DaqSystemManager)


        public void ProcessInputs()
        {
            if (_inputs.Count == 0) return;

            var ports = _inputs.GroupBy(ch => ch.Port).Select(g => g.Key).ToList();
            var currentSnapshot = new Dictionary<string, bool>();
            var changedChannels = new List<string>();

            foreach (var port in ports)
            {
                uint diVal = 0;
                ushort rtn = UniDAQ.Ixud_ReadDI(BoardNo, port, ref diVal);

                if (rtn != UniDAQ.Ixud_NoErr)
                {
                    HandleUniDaqError($"ReadDI_Port{port}", rtn);
                }

                var portChannels = _inputs.Where(ch => ch.Port == port);
                foreach (var ch in portChannels)
                {
                    bool rawBit = ((diVal & (1u << ch.Index)) != 0);
                    bool finalState = ch.Invert ? !rawBit : rawBit;

                    ch.State = finalState;
                    currentSnapshot[ch.Name] = finalState;

                    bool isNew = !_lastInputStates.ContainsKey(ch.Name);
                    if (isNew || _lastInputStates[ch.Name] != finalState)
                    {
                        _lastInputStates[ch.Name] = finalState;
                        changedChannels.Add(ch.Name);

                    }
                }
            }

            FrameReceived?.Invoke(this, new DigitalFrameEventArgs(currentSnapshot, changedChannels));
            IsConnected = true;
        }


        public void ProcessOutputs()
        {
            List<OutputCommand> batchCommands;

            lock (_queueLock)
            {
                if (_commandQueue.Count == 0) return;
                batchCommands = new List<OutputCommand>(_commandQueue);
                _commandQueue.Clear();
            }

            foreach (var cmd in batchCommands)
            {
                ushort bitVal = (ushort)(cmd.Value ? 1 : 0);
                ushort rtn = UniDAQ.Ixud_WriteDOBit(cmd.Channel.BoardNo,
                                                    cmd.Channel.Port,
                                                    (ushort)cmd.Channel.Index,
                                                    bitVal);

                if (rtn == UniDAQ.Ixud_NoErr)
                {
                    cmd.Channel.State = cmd.Value;
                }
                else
                {
                    HandleUniDaqError($"WriteDOBit_{cmd.Channel.Name}", rtn);
                }
            }
        }

        #endregion

        #region Helper Methods

        private void HandleUniDaqError(string source, ushort code)
        {
            string msg = $"Error {code} in {source}";
            ErrorOccurred?.Invoke(this, new DigitalErrorEventArgs(source, code, msg));
            throw new Exception(msg);
        }

        public void Dispose()
        {
        }
        #endregion
    }


}
