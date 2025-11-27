using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using XModels;


public class DigitalMonitorControl : UserControl
{
    private FlowLayoutPanel flowPanel;
    private Dictionary<string, Label> _indicators = new Dictionary<string, Label>();

    private System.Windows.Forms.Timer _watchdogTimer;
    private DateTime _lastDataTime;
    private bool _isTimedOut = false;
    private const int TimeoutThreshold = 200; 

    public DigitalMonitorControl()
    {
        InitializeUI();
        InitializeWatchdog();
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

    private void InitializeWatchdog()
    {
        _lastDataTime = DateTime.Now;
        _watchdogTimer = new System.Windows.Forms.Timer();
        _watchdogTimer.Interval = 50; 
        _watchdogTimer.Tick += WatchdogTimer_Tick;
        _watchdogTimer.Start();
    }

    private void WatchdogTimer_Tick(object sender, EventArgs e)
    {
        var elapsed = (DateTime.Now - _lastDataTime).TotalMilliseconds;

        if (elapsed > TimeoutThreshold && !_isTimedOut)
        {
            ResetIndicatorsToSafeState();
            _isTimedOut = true;
        }
    }

    private void ResetIndicatorsToSafeState()
    {
        if (flowPanel.Controls.Count == 0) return;

        flowPanel.SuspendLayout();
        foreach (var lbl in _indicators.Values)
        {
            // حالت خاموش و غیرفعال
            lbl.BackColor = Color.LightGray;
            lbl.ForeColor = Color.DimGray;
            lbl.BorderStyle = BorderStyle.FixedSingle; 
        }
        flowPanel.ResumeLayout();
    }

    public void UpdateData(DigitalFrameEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => UpdateData(e)));
            return;
        }

        _lastDataTime = DateTime.Now;
        _isTimedOut = false;

        flowPanel.SuspendLayout();

        foreach (var kvp in e.CurrentStates)
        {
            string chName = kvp.Key;
            bool state = kvp.Value;

            if (!_indicators.TryGetValue(chName, out Label lbl))
            {
                lbl = CreateIndicator(chName);
                flowPanel.Controls.Add(lbl);
                _indicators[chName] = lbl;
            }

            lbl.BackColor = state ? Color.LimeGreen : Color.LightGray;
            lbl.ForeColor = state ? Color.White : Color.Black;

            if (e.ChangedChannels != null && e.ChangedChannels.Contains(chName))
            {
                lbl.BorderStyle = BorderStyle.Fixed3D;
            }
            else
            {
                lbl.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        flowPanel.ResumeLayout();
    }

    private Label CreateIndicator(string text)
    {
        return new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(80, 40),
            Margin = new Padding(5),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        if (disposing && _watchdogTimer != null)
        {
            _watchdogTimer.Stop();
            _watchdogTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    private System.ComponentModel.IContainer components = null;
}
