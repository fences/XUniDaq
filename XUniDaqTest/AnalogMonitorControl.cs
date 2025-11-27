using System;
using System.Drawing;
using System.Windows.Forms;
using XModels;

public class AnalogMonitorControl : UserControl
{
    private DataGridView grid;
    private System.Windows.Forms.Timer _watchdogTimer;
    private DateTime _lastDataTime;
    private bool _isTimedOut = false; 
    private const int TimeoutThreshold = 200;

    public AnalogMonitorControl()
    {
        InitializeUI();
        InitializeWatchdog();
    }

    private void InitializeUI()
    {
        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.WhiteSmoke
        };

        grid.Columns.Add("ChName", "Channel Name");
        grid.Columns.Add("Value", "Voltage (V)");
        grid.Columns.Add("Raw", "Raw Value");

        grid.DefaultCellStyle.Font = new Font("Consolas", 10);
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        this.Controls.Add(grid);
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
            ResetDisplayToZero();
            _isTimedOut = true;
        }
    }

    private void ResetDisplayToZero()
    {
        grid.SuspendLayout();
        foreach (DataGridViewRow row in grid.Rows)
        {
            row.Cells[1].Value = "0.0000";
            row.Cells[2].Value = "0.0000";

            row.DefaultCellStyle.ForeColor = Color.Gray;
            row.DefaultCellStyle.SelectionForeColor = Color.Gray;
        }
        grid.ResumeLayout();
    }

    public void UpdateData(AnalogMultiChannelDataEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() => UpdateData(e)));
            return;
        }

 
        _lastDataTime = DateTime.Now;

        try
        {
            grid.SuspendLayout();


            if (_isTimedOut)
            {
                _isTimedOut = false;
                grid.DefaultCellStyle.ForeColor = Color.Black; 
                foreach (DataGridViewRow row in grid.Rows)
                {
                    row.DefaultCellStyle.ForeColor = Color.Black;
                }
            }

            if (grid.Rows.Count != e.Channels.Count)
            {
                grid.Rows.Clear();
                foreach (var ch in e.Channels)
                {
                    grid.Rows.Add(ch.Name, "0.0000", "0.0000");
                }
            }

            int lastSampleIndex = e.DataMatrix.GetLength(0) - 1;

            for (int i = 0; i < e.Channels.Count; i++)
            {
                grid.Rows[i].Cells[0].Value = e.Channels[i].Name;

                float val = e.DataMatrix[lastSampleIndex, i];
                float raw = e.RawDataMatrix[lastSampleIndex, i];

                grid.Rows[i].Cells[1].Value = val.ToString("F4");
                grid.Rows[i].Cells[2].Value = raw.ToString("F4");
            }
        }
        finally
        {
            grid.ResumeLayout();
        }
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
