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
            rtu.Baudrate = 9600;        //통신속도
            rtu.PortName = "com4";      //통신포트
            rtu.Interval = 10;          //통신간격
            rtu.Timeout = 100;          //타임아웃시간

            //모니터할 영역 지정     new ModbusAddress(국번, 주소, 영역타입, 개수)
            rtu.MonitorAddresses.Add(new ModbusAddress(1, 0x7000, ModbusAddressType.WORD, 1));
            rtu.MonitorAddresses.Add(new ModbusAddress(1, 0x1000, ModbusAddressType.BIT, 8));

            //모니터하는 영역의 값이 변경시 발생하는 이벤트
            rtu.ValueChanged += new EventHandler(rtu_ValueChanged);

            //통신시작
            rtu.Start();
        }

        void rtu_ValueChanged(object sender, EventArgs e)
        {
            //모니터링한 값은 Words속성과 Bits속성에 있으며 접근 방법은 rtu[국번][주소] 
            lbl7000.Text = rtu.Words[1][0x7000].ToString();
            lbl1000.Text = rtu.Bits[1][0x1000] ? "ON" : "OFF";
            lbl1001.Text = rtu.Bits[1][0x1001] ? "ON" : "OFF";
            lbl1002.Text = rtu.Bits[1][0x1002] ? "ON" : "OFF";
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            //0x1001번지 값을 Set 
            rtu.SetBit(1, 0x1001, true);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            //0x1001번지 값을 Clear
            rtu.SetBit(1, 0x1001, false);
        }
    }
}
