using DevExpress.XtraPrinting.Native;
using IcpDas.Daq.Service;
using IcpDas.Daq.System;
using IcpDas.Daq.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UiNew.Controls;
using UniDAQ_Ns;
using XModels;

namespace XUniDaqTest
{
    public partial class SampleForm : Form
    {
        public SampleForm()
        {
            InitializeComponent();
        }

        private AnalogMonitorControl analogMonitor;
        private DigitalMonitorControl digitalMonitor;
        private DigitalOutputControl digitalOutputControl;

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeLayout();

            if (xUniDaq1 != null) 
                xUniDaq1.UpdateRegression(0, new double[] { 0.0, 0.5 });

            if (xUniDaq2 != null && digitalOutputControl != null)
            {
                digitalOutputControl.InitializeChannels(xUniDaq2.DoChannels);
                digitalOutputControl.WriteRequested += DigitalOutputControl_WriteRequested;
            }

            foreach(var ch in xUniDaq1.AiChannels)
                cbList.Items.Add(ch.ChannelName);
        }

        private void DigitalOutputControl_WriteRequested(object sender, DigitalOutputRequestEventArgs e)
        {

            if (xUniDaq2 != null)
            {
                xUniDaq2.WriteDigitalPort(e.ChannelName, e.NewValue, e.Priority);
            }
        }

        private void InitializeLayout()
        {
            analogMonitor = new AnalogMonitorControl { Dock = DockStyle.Fill };
            digitalMonitor = new DigitalMonitorControl { Dock = DockStyle.Fill };
            digitalOutputControl = new DigitalOutputControl { Dock = DockStyle.Fill };

            GroupBox gbAnalog = new GroupBox { Text = "Analog Inputs :" + xUniDaq1.BoardName, Dock = DockStyle.Fill };
            gbAnalog.Controls.Add(analogMonitor);

            GroupBox gbDigital = new GroupBox { Text = "Digital Input :" + xUniDaq2.BoardName, Dock = DockStyle.Fill };
            gbDigital.Controls.Add(digitalMonitor);

            GroupBox gbDigitalOut = new GroupBox { Text = "Digital Output :" + xUniDaq2.BoardName, Dock = DockStyle.Fill };
            gbDigitalOut.Controls.Add(digitalOutputControl);

            panel1.Controls.Add(gbAnalog);
            panel2.Controls.Add(gbDigital);
            panel3.Controls.Add(gbDigitalOut);
        }

        private void tsbStart_Click(object sender, EventArgs e)
        {

            if (this.IsDisposed) return;

            if (!DaqServiceLocator.Instance.IsBoardRunning(xUniDaq1.BoardIndex))
            {
                xUniDaq1.Start();
                xUniDaq2.Start();
                if (digitalOutputControl != null && !digitalOutputControl.IsDisposed)
                    digitalOutputControl.SetSystemStatus(true);
                tsbStart.Text = "Stop Acquisition";
            }
            else
            {
                xUniDaq1.Stop();
                xUniDaq2.Stop();
                if (digitalOutputControl != null && !digitalOutputControl.IsDisposed)
                    digitalOutputControl.SetSystemStatus(false);
                tsbStart.Text = "Start Acquisition";
            }
        }


        private void SampleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                if (xUniDaq1 != null)
                {
                    xUniDaq1.AiDataReceived -= xUniDaq1_AiDataReceived;
                    xUniDaq1.StatusUpdated -= xUniDaq1_StatusUpdated;
                    xUniDaq1.ErrorOccurred -= xUniDaq1_ErrorOccurred;
                    xUniDaq1.Stop(); 
                }

                if (xUniDaq2 != null)
                {
                    xUniDaq2.DiDataReceived -= xUniDaq2_DiDataReceived;
                    xUniDaq2.StatusUpdated -= xUniDaq2_StatusUpdated;
                    xUniDaq2.ErrorOccurred -= xUniDaq2_ErrorOccurred;
                    xUniDaq2.Stop();
                }

                if (digitalOutputControl != null)
                {
                    digitalOutputControl.WriteRequested -= DigitalOutputControl_WriteRequested;
                }
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine("Error during closing: " + ex.Message);
            }
        }

        private void xUniDaq2_DiDataReceived(object sender, DigitalFrameEventArgs e)
        {

            if (this.IsDisposed || digitalMonitor == null || digitalMonitor.IsDisposed) return;
            if (digitalMonitor.InvokeRequired)
            {
                digitalMonitor.BeginInvoke(new Action(() => digitalMonitor.UpdateData(e)));
            }
            else
            {
                digitalMonitor.UpdateData(e);
            }
        }

        private void xUniDaq1_AiDataReceived(object sender, AnalogMultiChannelDataEventArgs e)
        {
            if (this.IsDisposed || analogMonitor == null || analogMonitor.IsDisposed) return;

            if (analogMonitor.InvokeRequired)
            {
                analogMonitor.BeginInvoke(new Action(() => analogMonitor.UpdateData(e)));
            }
            else
            {
                analogMonitor.UpdateData(e);
            }
        }

        private void xUniDaq1_DiDataReceived(object sender, DigitalFrameEventArgs e)
        {

        }

        private void xUniDaq2_StatusUpdated(object sender, string e)
        {
            if (this.IsDisposed || txtInfo.IsDisposed) return;

            this.BeginInvoke(new Action(() =>
            {
                if (!txtInfo.IsDisposed) txtInfo.AppendText( e + Environment.NewLine);
            }));
        }

        private void xUniDaq2_ErrorOccurred(object sender, DaqErrorEventArgs e)
        {
            if (this.IsDisposed || txtInfo.IsDisposed) return;

            this.BeginInvoke(new Action(() =>
            {
                if (!txtInfo.IsDisposed) txtInfo.AppendText(e.BoardName + "->" + e.Message + Environment.NewLine);
            }));
        }

        private void xUniDaq1_ErrorOccurred(object sender, DaqErrorEventArgs e)
        {
            if (this.IsDisposed || txtInfo.IsDisposed) return;

            this.BeginInvoke(new Action(() =>
            {
                if (!txtInfo.IsDisposed) txtInfo.AppendText(e.BoardName + "->" + e + Environment.NewLine);
            }));
        }

        private void xUniDaq1_StatusUpdated(object sender, string e)
        {
            if (this.IsDisposed || txtInfo.IsDisposed) return;

            this.BeginInvoke(new Action(() =>
            {
                if (!txtInfo.IsDisposed) txtInfo.AppendText(e + Environment.NewLine);
            }));
        }

        private void btnZero_Click(object sender, EventArgs e)
        {
            if (float.TryParse(txtZero.Text, out float val))
            {
                if (!string.IsNullOrEmpty(cbList.Text))
                    xUniDaq1.UpdateZeroOffset(cbList.Text, val);
            }
        }

        private void btnfilter_Click(object sender, EventArgs e)
        {
            if (uint.TryParse(txtFilter.Text, out uint val))
            {

                if (!string.IsNullOrEmpty(cbList.Text))
                    xUniDaq1.UpdateFilterWindow(cbList.Text, val);
            }
        }

        private void btnRegression_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtRegresion.Text))
            {
                xUniDaq1.UpdateRegression(cbList.Text, ParseStringToDoubles(txtRegresion.Text));
            }
            else
                xUniDaq1.UpdateRegression(cbList.Text, null);
        }

        private  double[] ParseStringToDoubles(string input)
        {
          
            if (string.IsNullOrWhiteSpace(input))
            {
                if (!string.IsNullOrEmpty(cbList.Text))
                    return new double[0]; 
            }


            char[] delimiters = new char[] { ' ', ',', '-' };
            string[] parts = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            List<double> resultList = new List<double>();

            foreach (var part in parts)
            { 
                if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                {
                    resultList.Add(number);
                }
            }
            return resultList.ToArray();
        }
        private string ConvertDoublesToString(double[] numbers, string separator = "-")
        {
            if (numbers == null || numbers.Length == 0)
            {
                return string.Empty;
            }

            var stringArray = numbers.Select(n => n.ToString(CultureInfo.InvariantCulture));

            return string.Join(separator, stringArray);
        }

        private void cbList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(cbList.Text))
            {
               var item =  xUniDaq1.AiChannels.FirstOrDefault(a=>a.ChannelName == cbList.Text);
                if (item != null)
                {

                    txtZero.Text = item.ZeroOffset.ToString();
                    txtFilter.Text = item.FilterWindowSize.ToString();
                    txtRegresion.Text = ConvertDoublesToString(item.Regression);
                }
                
            }
        }
    }
}
