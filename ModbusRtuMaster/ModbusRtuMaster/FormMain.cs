using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using J2.Community.Serial.ModbusRTU;

namespace ModbusRtuMaster
{
    public partial class FormMain : Form
    {
        J2ModbusRTUMaster rtu;
        public FormMain()
        {
            InitializeComponent();
            rtu = new J2ModbusRTUMaster(this);
            rtu.Baudrate = 9600;
            rtu.PortName = "com4";
            rtu.Interval = 10;
            rtu.Timeout = 100;
            rtu.MonitorAddresses.Add(new ModbusAddress(1, 0x7000, ModbusAddressType.WORD, 1));
            rtu.MonitorAddresses.Add(new ModbusAddress(1, 0x1000, ModbusAddressType.BIT, 8));
            rtu.ValueChanged += new EventHandler(rtu_ValueChanged);
        }

        void rtu_ValueChanged(object sender, EventArgs e)
        {
            lbl7000.Text = rtu.Words[1][0x7000].ToString();
            lbl1000.Text = rtu.Bits[1][0x1000] ? "ON" : "OFF";
            lbl1001.Text = rtu.Bits[1][0x1001] ? "ON" : "OFF";
            lbl1002.Text = rtu.Bits[1][0x1002] ? "ON" : "OFF";
        }

        protected override void OnLoad(EventArgs e)
        {
            rtu.Start();
            base.OnLoad(e);
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            rtu.SetBit(1, 0x1001, true);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtu.SetBit(1, 0x1001, false);
        }
    }
}
