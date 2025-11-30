using XModels;

namespace XUniDaqTest
{
    partial class SampleForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SampleForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsbStart = new System.Windows.Forms.ToolStripButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.txtInfo = new System.Windows.Forms.RichTextBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtZero = new System.Windows.Forms.TextBox();
            this.txtRegresion = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.cbList = new System.Windows.Forms.ComboBox();
            this.btnRegression = new System.Windows.Forms.Button();
            this.btnZero = new System.Windows.Forms.Button();
            this.btnfilter = new System.Windows.Forms.Button();
            this.xUniDaq1 = new IcpDas.Daq.XDaq.XUniDaq(this.components);
            this.xUniDaq2 = new IcpDas.Daq.XDaq.XUniDaq(this.components);
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.xUniDaq1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xUniDaq2)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbStart});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(993, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsbStart
            // 
            this.tsbStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbStart.Image = ((System.Drawing.Image)(resources.GetObject("tsbStart.Image")));
            this.tsbStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbStart.Name = "tsbStart";
            this.tsbStart.Size = new System.Drawing.Size(98, 22);
            this.tsbStart.Text = "Start Acquisition";
            this.tsbStart.Click += new System.EventHandler(this.tsbStart_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel3, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtInfo, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel4, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 25);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32.25806F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32.25806F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35.48387F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(993, 613);
            this.tableLayoutPanel1.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(490, 191);
            this.panel1.TabIndex = 0;
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(499, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(491, 191);
            this.panel2.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(499, 200);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(491, 191);
            this.panel3.TabIndex = 2;
            // 
            // txtInfo
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.txtInfo, 2);
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.Location = new System.Drawing.Point(3, 397);
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.Size = new System.Drawing.Size(987, 213);
            this.txtInfo.TabIndex = 3;
            this.txtInfo.Text = "";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.groupBox1);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(3, 200);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(490, 191);
            this.panel4.TabIndex = 4;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtZero);
            this.groupBox1.Controls.Add(this.txtRegresion);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtFilter);
            this.groupBox1.Controls.Add(this.cbList);
            this.groupBox1.Controls.Add(this.btnRegression);
            this.groupBox1.Controls.Add(this.btnZero);
            this.groupBox1.Controls.Add(this.btnfilter);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(490, 191);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Ai Parametres";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(165, 169);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(145, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Y=x0 + ax + bx^2 + cx^3 + ...";
            // 
            // txtZero
            // 
            this.txtZero.Location = new System.Drawing.Point(168, 88);
            this.txtZero.Name = "txtZero";
            this.txtZero.Size = new System.Drawing.Size(149, 20);
            this.txtZero.TabIndex = 7;
            this.txtZero.Text = "0";
            // 
            // txtRegresion
            // 
            this.txtRegresion.Location = new System.Drawing.Point(168, 146);
            this.txtRegresion.Name = "txtRegresion";
            this.txtRegresion.Size = new System.Drawing.Size(149, 20);
            this.txtRegresion.TabIndex = 6;
            this.txtRegresion.Text = "0-0.5";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(85, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Ai Channels List:";
            // 
            // txtFilter
            // 
            this.txtFilter.Location = new System.Drawing.Point(168, 118);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(149, 20);
            this.txtFilter.TabIndex = 4;
            this.txtFilter.Text = "0";
            // 
            // cbList
            // 
            this.cbList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbList.FormattingEnabled = true;
            this.cbList.Location = new System.Drawing.Point(122, 35);
            this.cbList.Name = "cbList";
            this.cbList.Size = new System.Drawing.Size(195, 21);
            this.cbList.TabIndex = 3;
            this.cbList.SelectedIndexChanged += new System.EventHandler(this.cbList_SelectedIndexChanged);
            // 
            // btnRegression
            // 
            this.btnRegression.Location = new System.Drawing.Point(27, 144);
            this.btnRegression.Name = "btnRegression";
            this.btnRegression.Size = new System.Drawing.Size(135, 23);
            this.btnRegression.TabIndex = 2;
            this.btnRegression.Text = "Regression:";
            this.btnRegression.UseVisualStyleBackColor = true;
            this.btnRegression.Click += new System.EventHandler(this.btnRegression_Click);
            // 
            // btnZero
            // 
            this.btnZero.Location = new System.Drawing.Point(27, 86);
            this.btnZero.Name = "btnZero";
            this.btnZero.Size = new System.Drawing.Size(135, 23);
            this.btnZero.TabIndex = 1;
            this.btnZero.Text = "Ziro Offset";
            this.btnZero.UseVisualStyleBackColor = true;
            this.btnZero.Click += new System.EventHandler(this.btnZero_Click);
            // 
            // btnfilter
            // 
            this.btnfilter.Location = new System.Drawing.Point(27, 115);
            this.btnfilter.Name = "btnfilter";
            this.btnfilter.Size = new System.Drawing.Size(135, 23);
            this.btnfilter.TabIndex = 0;
            this.btnfilter.Text = "Average Filter Size:";
            this.btnfilter.UseVisualStyleBackColor = true;
            this.btnfilter.Click += new System.EventHandler(this.btnfilter_Click);
            // 
            // xUniDaq1
            // 
            this.xUniDaq1.AiChannels.Add(((IcpDas.Daq.XDaq.AnalogChannelConfig)(resources.GetObject("xUniDaq1.AiChannels"))));
            this.xUniDaq1.AiChannels.Add(((IcpDas.Daq.XDaq.AnalogChannelConfig)(resources.GetObject("xUniDaq1.AiChannels1"))));
            this.xUniDaq1.AiChannels.Add(((IcpDas.Daq.XDaq.AnalogChannelConfig)(resources.GetObject("xUniDaq1.AiChannels2"))));
            this.xUniDaq1.BoardIndex = ((short)(0));
            this.xUniDaq1.DataCount = ((uint)(200u));
            this.xUniDaq1.SamplingRate = 100000F;
            this.xUniDaq1.AiDataReceived += new System.EventHandler<XModels.AnalogMultiChannelDataEventArgs>(this.xUniDaq1_AiDataReceived);
            this.xUniDaq1.DiDataReceived += new System.EventHandler<XModels.DigitalFrameEventArgs>(this.xUniDaq1_DiDataReceived);
            this.xUniDaq1.ErrorOccurred += new System.EventHandler<IcpDas.Daq.DaqSystem.DaqErrorEventArgs>(this.xUniDaq1_ErrorOccurred);
            this.xUniDaq1.StatusUpdated += new System.EventHandler<string>(this.xUniDaq1_StatusUpdated);
            // 
            // xUniDaq2
            // 
            this.xUniDaq2.BoardIndex = ((short)(1));
            this.xUniDaq2.DiChannels.Add(((IcpDas.Daq.XDaq.DigitalInputChannelConfig)(resources.GetObject("xUniDaq2.DiChannels"))));
            this.xUniDaq2.DiChannels.Add(((IcpDas.Daq.XDaq.DigitalInputChannelConfig)(resources.GetObject("xUniDaq2.DiChannels1"))));
            this.xUniDaq2.DoChannels.Add(((IcpDas.Daq.XDaq.DigitalOutputChannelConfig)(resources.GetObject("xUniDaq2.DoChannels"))));
            this.xUniDaq2.DoChannels.Add(((IcpDas.Daq.XDaq.DigitalOutputChannelConfig)(resources.GetObject("xUniDaq2.DoChannels1"))));
            this.xUniDaq2.DiDataReceived += new System.EventHandler<XModels.DigitalFrameEventArgs>(this.xUniDaq2_DiDataReceived);
            this.xUniDaq2.ErrorOccurred += new System.EventHandler<IcpDas.Daq.DaqSystem.DaqErrorEventArgs>(this.xUniDaq2_ErrorOccurred);
            this.xUniDaq2.StatusUpdated += new System.EventHandler<string>(this.xUniDaq2_StatusUpdated);
            // 
            // SampleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(993, 638);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.toolStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SampleForm";
            this.Text = "Sample Form";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SampleForm_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.xUniDaq1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xUniDaq2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsbStart;
     
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.RichTextBox txtInfo;

        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.ComboBox cbList;
        private System.Windows.Forms.Button btnRegression;
        private System.Windows.Forms.Button btnZero;
        private System.Windows.Forms.Button btnfilter;
        private System.Windows.Forms.TextBox txtRegresion;
        private System.Windows.Forms.TextBox txtZero;
        private System.Windows.Forms.Label label2;
        private IcpDas.Daq.XDaq.XUniDaq xUniDaq1;
        private IcpDas.Daq.XDaq.XUniDaq xUniDaq2;
    }
}

