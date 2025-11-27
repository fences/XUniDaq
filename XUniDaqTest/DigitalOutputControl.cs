using IcpDas.Daq.WinForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;


namespace UiNew.Controls
{
 
    public class DigitalOutputRequestEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public bool NewValue { get; }
        public int Priority { get; }

        public DigitalOutputRequestEventArgs(string name, bool value, int priority)
        {
            ChannelName = name;
            NewValue = value;
            Priority = priority;
        }
    }

    public class DigitalOutputControl : UserControl
    {
        private FlowLayoutPanel flowPanel;
        private Dictionary<string, Label> _indicators = new Dictionary<string, Label>();


        public event EventHandler<DigitalOutputRequestEventArgs> WriteRequested;

        public DigitalOutputControl()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            this.Controls.Add(flowPanel);
        }


        public void InitializeChannels(Collection<DigitalOutputChannelConfig> channels)
        {
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();
            _indicators.Clear();

            if (channels == null) return;

            foreach (var ch in channels)
            {
           
                var lbl = CreateInteractiveIndicator(ch);
                flowPanel.Controls.Add(lbl);
                _indicators[ch.ChannelName] = lbl;
            }

            SetSystemStatus(false);

            flowPanel.ResumeLayout();
        }


        public void SetSystemStatus(bool isRunning)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetSystemStatus(isRunning)));
                return;
            }

            flowPanel.Enabled = isRunning;

            if (!isRunning)
            {
              
                foreach (var lbl in _indicators.Values)
                {
                    lbl.BackColor = Color.LightGray;
                    lbl.ForeColor = Color.DimGray;

                    lbl.Tag = (object)new ChannelState { Config = ((ChannelState)lbl.Tag).Config, IsOn = false };
                }
            }
            else
            {

                foreach (var lbl in _indicators.Values)
                {
                    var state = (ChannelState)lbl.Tag;
                    UpdateLabelVisuals(lbl, state.IsOn);
                }
            }
        }

        private Label CreateInteractiveIndicator(DigitalOutputChannelConfig config)
        {
          
            var state = new ChannelState { Config = config, IsOn = false };

            var lbl = new Label
            {
                Text = config.ToString(), 
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(100, 45), 
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = state,
                BackColor = Color.LightGray,
                ForeColor = Color.DimGray
            };

            lbl.Click += Lbl_Click;
            return lbl;
        }

        private void Lbl_Click(object sender, EventArgs e)
        {
            if (sender is Label lbl && lbl.Tag is ChannelState state)
            {
                bool newState = !state.IsOn;
                state.IsOn = newState;

          
                UpdateLabelVisuals(lbl, newState);

             
                WriteRequested?.Invoke(this, new DigitalOutputRequestEventArgs(
                    state.Config.ChannelName,
                    newState,
                    state.Config.Priority
                ));
            }
        }

        private void UpdateLabelVisuals(Label lbl, bool isOn)
        {
            if (isOn)
            {
                lbl.BackColor = Color.DodgerBlue;
                lbl.ForeColor = Color.White;
                lbl.BorderStyle = BorderStyle.Fixed3D;
            }
            else
            {
                lbl.BackColor = Color.LightGray; 
                lbl.ForeColor = Color.Black;
                lbl.BorderStyle = BorderStyle.FixedSingle;
            }
        }

  
        private class ChannelState
        {
            public DigitalOutputChannelConfig Config { get; set; }
            public bool IsOn { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;
    }
}
