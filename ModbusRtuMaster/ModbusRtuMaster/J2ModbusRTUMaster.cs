using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;
using System.Collections;
using System.Windows.Forms;

namespace J2.Community.Serial.ModbusRTU
{
    public class J2ModbusRTUMaster
    {
        #region Property
        #region PortName
        private string strPort = "COM1";
        public string PortName
        {
            get { return strPort; }
            set { strPort = value; }
        }
        #endregion
        #region Baudrate
        private int nBaudrate = 115200;
        public int Baudrate
        {
            get { return nBaudrate; }
            set { nBaudrate = value; }
        }
        #endregion
        #region Interval
        private int nInterval = 10;
        public int Interval
        {
            get { return nInterval; }
            set { nInterval = value; }
        }
        #endregion
        #region Timeout
        private int nTimeout = 100;
        public int Timeout
        {
            get { return nTimeout; }
            set { nTimeout = value; }
        }
        #endregion
        #region Bits
        private Dictionary<int, Dictionary<int, bool>> dicBits = new Dictionary<int, Dictionary<int, bool>>();
        [Browsable(false)]
        public Dictionary<int, Dictionary<int, bool>> Bits { get { return dicBits; } }
        #endregion
        #region Words
        private Dictionary<int, Dictionary<int, int>> dicWords = new Dictionary<int, Dictionary<int, int>>();
        [Browsable(false)]
        public Dictionary<int, Dictionary<int, int>> Words { get { return dicWords; } }
        #endregion
        #region Monitoring
        private List<ModbusAddress> lstAddrs = new List<ModbusAddress>();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<ModbusAddress> MonitorAddresses
        {
            get { return lstAddrs; }
        }
        #endregion
        #region Remark
        /*
        #region BitMemories
        private List<ModbusBitMemory> lstBitMemories = new List<ModbusBitMemory>();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<ModbusBitMemory> BitMemories
        {
            get { return lstBitMemories; }
        }
        #endregion
        #region WordMemories
        private List<ModbusWordMemory> lstWordMemories = new List<ModbusWordMemory>();
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<ModbusWordMemory> WordMemories
        {
            get { return lstWordMemories; }
        }
        #endregion
        */
        #endregion
        #endregion
        #region Member Variable

        private Queue<Work> WorkQueue = new Queue<Work>();
        private List<Work> ManualWorkList = new List<Work>();
        private List<Work> AutoWorkList = new List<Work>();
        private byte[] baResponse = new byte[1024 * 8];

        private SerialPort ser = new SerialPort();
        private Thread th;
        private bool IsRunning = false;
        #endregion
        #region Event
        public event EventHandler ValueChanged;
        public event EventHandler TimeoutError;
        public event EventHandler CRCError;
        #endregion
        #region Work
        internal class Work
        {
            public byte[] data;
            public int ResponseByteCount;
            public bool Repeat = false;
            public Work(byte[] data, int ResByCount)
            {
                this.data = data;
                this.ResponseByteCount = ResByCount;
            }
            public Work(byte[] data, int ResByCount, bool Repeat)
            {
                this.data = data;
                this.ResponseByteCount = ResByCount;
                this.Repeat = Repeat;
            }
        }
        #endregion
        Form frm;
        public J2ModbusRTUMaster() { }
        public J2ModbusRTUMaster(Form frm) { this.frm = frm; }
        public J2ModbusRTUMaster(Form frm, string Port, int Baurate) { this.frm = frm; this.PortName = Port; this.Baudrate = Baudrate; }
        

        #region Start/Stop
        public void Start()
        {
            IsRunning = true;

            #region Remark - Memory
            /*
            DicBits.Clear();
            DicWords.Clear();


            for (int i = 0; i < BitMemories.Count; i++)
            {
                ModbusBitMemory m = BitMemories[i];
                if (!DicBits.ContainsKey(m.Slave)) DicBits.Add(m.Slave, new Dictionary<int, ModbusBitMemory>());
                if (!DicBits[m.Slave].ContainsKey(m.Address)) DicBits[m.Slave].Add(m.Address, m);
            }
            for (int i = 0; i < WordMemories.Count; i++)
            {
                ModbusWordMemory m = WordMemories[i];
                if (!DicWords.ContainsKey(m.Slave)) DicWords.Add(m.Slave, new Dictionary<int, ModbusWordMemory>());
                if (!DicWords[m.Slave].ContainsKey(m.Address)) DicWords[m.Slave].Add(m.Address, m);
            }
            */
            #endregion

            #region Bits/Words
            Bits.Clear();
            for (int i = 0; i < MonitorAddresses.Count; i++)
            {
                ModbusAddress d = MonitorAddresses[i];
                if (d.AddressType == ModbusAddressType.BIT)
                    for (int addr = d.Address; addr < d.Address + d.Length; addr++)
                    {
                        if (!Bits.ContainsKey(d.Slave)) Bits.Add(d.Slave, new Dictionary<int, bool>());
                        if (!Bits[d.Slave].ContainsKey(addr)) Bits[d.Slave].Add(addr, false);
                    }
            }
            Words.Clear();
            for (int i = 0; i < MonitorAddresses.Count; i++)
            {
                ModbusAddress d = MonitorAddresses[i];
                if (d.AddressType == ModbusAddressType.WORD)
                    for (int addr = d.Address; addr < d.Address + d.Length; addr++)
                    {
                        if (!Words.ContainsKey(d.Slave)) Words.Add(d.Slave, new Dictionary<int, int>());
                        if (!Words[d.Slave].ContainsKey(addr)) Words[d.Slave].Add(addr, 0);
                    }
            }
            #endregion

            th = new Thread(new ThreadStart(Run));
            th.IsBackground = true;
            th.Start();
        }

        public void Stop()
        {
            IsRunning = false;
        }
        #endregion

        #region Remark
        /*
        private Dictionary<int, Dictionary<int, ModbusBitMemory>> DicBits = new Dictionary<int, Dictionary<int, ModbusBitMemory>>();
        private Dictionary<int, Dictionary<int, ModbusWordMemory>> DicWords = new Dictionary<int, Dictionary<int, ModbusWordMemory>>();
        */
        #endregion

        #region Run
        private void Run()
        {
            ser.PortName = PortName;
            ser.BaudRate = Baudrate;
            ser.ReadTimeout = Timeout;
            ser.WriteTimeout = Timeout;
            ser.Open();

            try
            {
                while (IsRunning)
                {
                    if (ManualWorkList.Count > 0 || WorkQueue.Count > 0)
                    {
                        Work w = null;
                        bool bRepeat = true;
                        int nTimeoutCount = 0;

                        #region GetWork
                        if (ManualWorkList.Count > 0)
                        {
                            w = ManualWorkList[0];
                            ManualWorkList.RemoveAt(0);
                        }
                        else w = WorkQueue.Dequeue();
                        #endregion

                        while (bRepeat)
                        {
                            if (w != null)
                            {
                                #region Send
                                try
                                {
                                    ser.DiscardInBuffer();
                                    ser.DiscardOutBuffer();
                                    ser.Write(w.data, 0, w.data.Length);
                                    ser.BaseStream.Flush();
                                }
                                catch (TimeoutException) { }
                                #endregion

                                #region Receive
                                DateTime nPrev = DateTime.Now;
                                int gap = 0, nRecv = 0, nLen = 0;
                                bRepeat = w.Repeat;

                                if (w.data[0] != 0)//Broadcast
                                {
                                    #region Read
                                    while (nRecv < w.ResponseByteCount)
                                    {
                                        try
                                        {
                                            nLen = ser.Read(baResponse, nRecv, w.ResponseByteCount);
                                            nRecv += nLen;
                                        }
                                        catch (TimeoutException) { }
                                        catch (System.IO.IOException) { }
                                        gap = Convert.ToInt32((DateTime.Now - nPrev).TotalMilliseconds);
                                        if (gap >= Timeout) break;
                                        if (nRecv == w.ResponseByteCount) break;
                                    }
                                    #endregion
                                    if (gap < nTimeout)
                                    {
                                        try
                                        {
                                            #region Proc
                                            byte crcHi = 0, crcLo = 0;
                                            GetCRC(baResponse, nRecv - 2, ref crcHi, ref crcLo);
                                            if (crcHi == baResponse[nRecv - 2] && crcLo == baResponse[nRecv - 1])
                                            {
                                                int Slave = baResponse[0];
                                                int SendAddr = Convert.ToInt32((w.data[2] << 8) | (w.data[3]));
                                                ModbusFunction Func = (ModbusFunction)baResponse[1];
                                                switch (Func)
                                                {
                                                    case ModbusFunction.BITREAD_F1:
                                                    case ModbusFunction.BITREAD_F2:
                                                        {
                                                            int ByteCount = baResponse[2];
                                                            byte[] baData = new byte[ByteCount];
                                                            Array.Copy(baResponse, 3, baData, 0, ByteCount);
                                                            BitArray ba = new BitArray(baData);

                                                            #region Datas
                                                            bool bChanged = false;
                                                            for (int i = SendAddr; i < SendAddr + ba.Count; i++)
                                                                if (Bits.ContainsKey(Slave) && Bits[Slave].ContainsKey(i))
                                                                    if (Bits[Slave][i] != ba[i - SendAddr])
                                                                    {
                                                                        Bits[Slave][i] = ba[i - SendAddr];
                                                                        bChanged = true;
                                                                    }

                                                            if (bChanged && ValueChanged != null)
                                                            {
                                                                if (frm != null) frm.Invoke(ValueChanged, new object[] { this, null });
                                                                else ValueChanged.Invoke(this, null);
                                                            }
                                                            #endregion
                                                            #region Remark
                                                            /*
                                                        for (int i = SendAddr; i < SendAddr + ba.Count; i++)
                                                            if (DicBits.ContainsKey(Slave) && DicBits[Slave].ContainsKey(i))
                                                                DicBits[Slave][i].Value = ba[i - SendAddr];
                                                        */
                                                            #endregion
                                                        }
                                                        break;
                                                    case ModbusFunction.WORDREAD_F3:
                                                    case ModbusFunction.WORDREAD_F4:
                                                        {
                                                            int ByteCount = baResponse[2];
                                                            int[] Datas = new int[ByteCount / 2];
                                                            for (int i = 0; i < Datas.Length; i++)
                                                                Datas[i] = Convert.ToUInt16(baResponse[3 + (i * 2)] << 8 | baResponse[4 + (i * 2)]);

                                                            #region Datas
                                                            bool bChanged = false;
                                                            for (int i = SendAddr; i < SendAddr + Datas.Length; i++)
                                                                if (Words.ContainsKey(Slave) && Words[Slave].ContainsKey(i))
                                                                    if (Words[Slave][i] != Datas[i - SendAddr])
                                                                    {
                                                                        Words[Slave][i] = Datas[i - SendAddr];
                                                                        bChanged = true;
                                                                    }

                                                            if (bChanged && ValueChanged != null)
                                                            {
                                                                if (frm != null) frm.Invoke(ValueChanged, new object[] { this, null });
                                                                else ValueChanged.Invoke(this, null);
                                                            }
                                                            #endregion
                                                            #region Remark
                                                            /*
                                                        for (int i = SendAddr; i < SendAddr + Datas.Length; i++)
                                                            if (DicWords.ContainsKey(Slave) && DicWords[Slave].ContainsKey(i))
                                                                DicWords[Slave][i].Value = Datas[i - SendAddr];
                                                        */
                                                            #endregion
                                                        }
                                                        break;
                                                    case ModbusFunction.BITWRITE_F5:
                                                    case ModbusFunction.WORDWRITE_F6:
                                                        {
                                                            int StartAddress = (baResponse[2] << 8) | baResponse[3];
                                                            int Data = (baResponse[4] << 8) | baResponse[5];
                                                        }
                                                        break;
                                                    case ModbusFunction.MULTIBITWRITE_F15:
                                                    case ModbusFunction.MULTIWORDWRITE_F16:
                                                        {
                                                            int StartAddress = (baResponse[2] << 8) | baResponse[3];
                                                            int Length = (baResponse[4] << 8) | baResponse[5];
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                if (CRCError != null)
                                                {
                                                    if (frm != null) frm.Invoke(CRCError, new object[] { this, null });
                                                    else CRCError.Invoke(this, null);
                                                }
                                            }
                                            #endregion
                                            bRepeat = false;
                                        }
                                        catch (System.OverflowException) { }
                                    }
                                    else
                                    {
                                        #region Timeout
                                        if (TimeoutError != null)
                                        {
                                            if (frm != null) frm.Invoke(TimeoutError, new object[] { this, null });
                                            else TimeoutError.Invoke(this, null);
                                        }

                                        nTimeoutCount++;
                                        if (nTimeoutCount > 3)
                                        {
                                            nTimeoutCount = 0;
                                            bRepeat = false;
                                        }
                                        #endregion
                                    }
                                }
                                else bRepeat = false;
                                #endregion
                            }
                        }
                    }
                    else
                    {
                        //for (int i = 0; i < AutoWorkList.Count; i++) WorkQueue.Enqueue(AutoWorkList[i]);
                        for (int i = 0; i < MonitorAddresses.Count; i++) WorkQueue.Enqueue(MonitorAddresses[i].GetWork());
                    }
                    Thread.Sleep(Interval);
                }
            }
            catch (ObjectDisposedException) { }

            ser.Close();
        }
        #endregion

        #region CRC LRC CHECK
        public static void GetCRC(byte[] pby, int nSize, ref byte byFirstReturn, ref byte bySecondReturn)
        {
            #region Table
            byte[] auchCRCHi =  {0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                                 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,0x40};

            byte[] auchCRCLo = { 0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
                                 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
                                 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
                                 0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                                 0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
                                 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
                                 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
                                 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                                 0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
                                 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
                                 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
                                 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                                 0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
                                 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
                                 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
                                 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                                 0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,0x40};
            #endregion
            int uIndex;
            byte uchCRCHi = 0xff;
            byte uchCRCLo = 0xff;
            for (int i = 0; i < nSize; i++)
            {
                uIndex = uchCRCHi ^ pby[i];
                uchCRCHi = (byte)(uchCRCLo ^ auchCRCHi[uIndex]);
                uchCRCLo = auchCRCLo[uIndex];
            }
            int CRC = (uchCRCHi << 8) | uchCRCLo;
            byFirstReturn = (byte)(CRC / 256);
            bySecondReturn = (byte)(CRC % 256);
            pby = null;
        }
        public static void GetCRC(List<byte> pby, int startindex, int nSize, ref byte byFirstReturn, ref byte bySecondReturn)
        {
            #region Table
            byte[] auchCRCHi =  {0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
                                 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01,
                                 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,
                                 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
                                 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01,
                                 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                                 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81,0x40};

            byte[] auchCRCLo = { 0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4,
                                 0x04, 0xCC, 0x0C, 0x0D, 0xCD, 0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
                                 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A, 0x1E, 0xDE, 0xDF, 0x1F, 0xDD,
                                 0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                                 0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7,
                                 0x37, 0xF5, 0x35, 0x34, 0xF4, 0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
                                 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29, 0xEB, 0x2B, 0x2A, 0xEA, 0xEE,
                                 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                                 0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2,
                                 0x62, 0x66, 0xA6, 0xA7, 0x67, 0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
                                 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68, 0x78, 0xB8, 0xB9, 0x79, 0xBB,
                                 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                                 0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91,
                                 0x51, 0x93, 0x53, 0x52, 0x92, 0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
                                 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B, 0x99, 0x59, 0x58, 0x98, 0x88,
                                 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                                 0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80,0x40};
            #endregion
            int uIndex;
            byte uchCRCHi = 0xff;
            byte uchCRCLo = 0xff;
            for (int i = startindex; i < startindex + nSize; i++)
            {
                uIndex = uchCRCHi ^ pby[i];
                uchCRCHi = (byte)(uchCRCLo ^ auchCRCHi[uIndex]);
                uchCRCLo = auchCRCLo[uIndex];
            }
            int CRC = (uchCRCHi << 8) | uchCRCLo;
            byFirstReturn = (byte)(CRC / 256);
            bySecondReturn = (byte)(CRC % 256);
            pby = null;
        }
        #endregion

        #region DevideShort
        private void DevideShort(int value, ref byte high, ref byte low)
        {
            high = (byte)((value & 0xFF00) >> 8);
            low = (byte)(value & 0xFF);
        }
        #endregion

        #region Command
        #region None
        #region Manual
        internal void ManualBitRead_F1(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x01;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            ManualWorkList.Add(new Work(data, nResCount + 5));
            data = null;
        }
        internal void ManualBitRead_F2(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x02;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            ManualWorkList.Add(new Work(data, nResCount + 5));
            data = null;
        }
        internal void ManualWordRead_F3(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x03;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            ManualWorkList.Add(new Work(data, Length * 2 + 5));
            data = null;
        }
        internal void ManualWordRead_F4(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x04;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            ManualWorkList.Add(new Work(data, Length * 2 + 5));
            data = null;
        }
        internal void ManualBitWrite(int Slave, int StartAddr, bool Value)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value ? 0xFF00 : 0x0000, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x05;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            ManualWorkList.Add(new Work(data, 8));
            data = null;
        }
        internal void ManualWordWrite(int Slave, int StartAddr, int Value)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x06;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            ManualWorkList.Add(new Work(data, 8));
            data = null;
        }
        internal void ManualMultiBitWrite(int Slave, int StartAddr, bool[] Value)
        {
            int Length = Value.Length / 8;
            Length += (Value.Length % 8 == 0) ? 0 : 1;

            byte[] data = new byte[9 + Length];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x0F;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Length);

            for (int i = 0; i < Length; i++)
            {
                byte val = 0;
                int nTemp = 0;
                for (int j = (i * 8); j < Value.Length && j < (i * 8) + 8; j++)
                {
                    if (Value[j])
                        val |= Convert.ToByte(Math.Pow(2, nTemp));
                    nTemp++;
                }
                data[7 + i] = val;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8));
            Value = null;
            data = null;
        }
        internal void ManualMultiWordWrite(int Slave, int StartAddr, int[] Value)
        {
            byte[] data = new byte[9 + (Value.Length * 2)];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x10;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Value.Length * 2);

            for (int i = 0; i < Value.Length; i++)
            {
                byte valHi = 0, valLow = 0;
                DevideShort(Value[i], ref valHi, ref valLow);
                data[7 + (i * 2)] = valHi;
                data[8 + (i * 2)] = valLow;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8));
            Value = null;
            data = null;
        }
        #endregion
        #region Auto
        internal void AutoBitRead_F1(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x01;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            AutoWorkList.Add(new Work(data, nResCount + 5));
            data = null;
        }
        internal void AutoBitRead_F2(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x02;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            AutoWorkList.Add(new Work(data, nResCount + 5));
            data = null;
        }
        internal void AutoWordRead_F3(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x03;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            AutoWorkList.Add(new Work(data, Length * 2 + 5));
            data = null;
        }
        internal void AutoWordRead_F4(int Slave, int StartAddr, int Length)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x04;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            AutoWorkList.Add(new Work(data, Length * 2 + 5));
            data = null;
        }
        internal void AutoBitWrite(int Slave, int StartAddr, bool Value)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value ? 0xFF00 : 0x0000, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x05;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            AutoWorkList.Add(new Work(data, 8));
            data = null;
        }
        internal void AutoWordWrite(int Slave, int StartAddr, int Value)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x06;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            AutoWorkList.Add(new Work(data, 8));
            data = null;
        }
        internal void AutoMultiBitWrite(int Slave, int StartAddr, bool[] Value)
        {
            int Length = Value.Length / 8;
            Length += (Value.Length % 8 == 0) ? 0 : 1;

            byte[] data = new byte[9 + Length];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x0F;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Length);

            for (int i = 0; i < Length; i++)
            {
                byte val = 0;
                int nTemp = 0;
                for (int j = (i * 8); j < Value.Length && j < (i * 8) + 8; j++)
                {
                    if (Value[j])
                        val |= Convert.ToByte(Math.Pow(2, nTemp));
                    nTemp++;
                }
                data[7 + i] = val;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8));
            Value = null;
            data = null;
        }
        internal void AutoMultiWordWrite(int Slave, int StartAddr, int[] Value)
        {
            byte[] data = new byte[9 + (Value.Length * 2)];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x10;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Value.Length * 2);

            for (int i = 0; i < Value.Length; i++)
            {
                byte valHi = 0, valLow = 0;
                DevideShort(Value[i], ref valHi, ref valLow);
                data[7 + (i * 2)] = valHi;
                data[8 + (i * 2)] = valLow;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            AutoWorkList.Add(new Work(data, 8));
            Value = null;
            data = null;
        }
        #endregion
        #region SetBit
        public void SetBit(int Slave, int Address, bool Value)
        {
            if (IsRunning) ManualBitWrite(Slave, Address, Value);
        }
        public void SetBits(int Slave, int Address, bool[] Values)
        {
            if (IsRunning) ManualMultiBitWrite(Slave, Address, Values);
        }
        public void SetWord(int Slave, int Address, int Value)
        {
            if (IsRunning) ManualWordWrite(Slave, Address, Value);
        }
        public void SetWords(int Slave, int Address, int[] Values)
        {
            if (IsRunning) ManualMultiWordWrite(Slave, Address, Values);
        }
        #endregion
        #endregion
        #region Repeat
        #region Manual
        internal void ManualBitRead_F1(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x01;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            ManualWorkList.Add(new Work(data, nResCount + 5, Repeat));
            data = null;
        }
        internal void ManualBitRead_F2(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x02;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            ManualWorkList.Add(new Work(data, nResCount + 5, Repeat));
            data = null;
        }
        internal void ManualWordRead_F3(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x03;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            ManualWorkList.Add(new Work(data, Length * 2 + 5, Repeat));
            data = null;
        }
        internal void ManualWordRead_F4(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x04;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            ManualWorkList.Add(new Work(data, Length * 2 + 5, Repeat));
            data = null;
        }
        internal void ManualBitWrite(int Slave, int StartAddr, bool Value, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value ? 0xFF00 : 0x0000, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x05;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            ManualWorkList.Add(new Work(data, 8, Repeat));
            data = null;
        }
        internal void ManualWordWrite(int Slave, int StartAddr, int Value, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x06;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            ManualWorkList.Add(new Work(data, 8, Repeat));
            data = null;
        }
        internal void ManualMultiBitWrite(int Slave, int StartAddr, bool[] Value, bool Repeat)
        {
            int Length = Value.Length / 8;
            Length += (Value.Length % 8 == 0) ? 0 : 1;

            byte[] data = new byte[9 + Length];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x0F;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Length);

            for (int i = 0; i < Length; i++)
            {
                byte val = 0;
                int nTemp = 0;
                for (int j = (i * 8); j < Value.Length && j < (i * 8) + 8; j++)
                {
                    if (Value[j])
                        val |= Convert.ToByte(Math.Pow(2, nTemp));
                    nTemp++;
                }
                data[7 + i] = val;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8, Repeat));
            Value = null;
            data = null;
        }
        internal void ManualMultiWordWrite(int Slave, int StartAddr, int[] Value, bool Repeat)
        {
            byte[] data = new byte[9 + (Value.Length * 2)];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x10;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Value.Length * 2);

            for (int i = 0; i < Value.Length; i++)
            {
                byte valHi = 0, valLow = 0;
                DevideShort(Value[i], ref valHi, ref valLow);
                data[7 + (i * 2)] = valHi;
                data[8 + (i * 2)] = valLow;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8, Repeat));
            Value = null;
            data = null;
        }
        #endregion
        #region Auto
        internal void AutoBitRead_F1(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x01;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            AutoWorkList.Add(new Work(data, nResCount + 5, Repeat));
            data = null;
        }
        internal void AutoBitRead_F2(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x02;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            int nResCount = Length / 8;
            if (Length % 8 != 0) nResCount++;
            AutoWorkList.Add(new Work(data, nResCount + 5, Repeat));
            data = null;
        }
        internal void AutoWordRead_F3(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x03;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            AutoWorkList.Add(new Work(data, Length * 2 + 5, Repeat));
            data = null;
        }
        internal void AutoWordRead_F4(int Slave, int StartAddr, int Length, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x04;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;

            AutoWorkList.Add(new Work(data, Length * 2 + 5, Repeat));
            data = null;
        }
        internal void AutoBitWrite(int Slave, int StartAddr, bool Value, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value ? 0xFF00 : 0x0000, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x05;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            AutoWorkList.Add(new Work(data, 8, Repeat));
            data = null;
        }
        internal void AutoWordWrite(int Slave, int StartAddr, int Value, bool Repeat)
        {
            byte[] data = new byte[8];
            byte addrHi = 0, addrLow = 0;
            byte valHi = 0, valLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value, ref valHi, ref valLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x06;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = valHi;
            data[5] = valLow;

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[6] = crcHi;
            data[7] = crcLo;
            AutoWorkList.Add(new Work(data, 8, Repeat));
            data = null;
        }
        internal void AutoMultiBitWrite(int Slave, int StartAddr, bool[] Value, bool Repeat)
        {
            int Length = Value.Length / 8;
            Length += (Value.Length % 8 == 0) ? 0 : 1;

            byte[] data = new byte[9 + Length];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x0F;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Length);

            for (int i = 0; i < Length; i++)
            {
                byte val = 0;
                int nTemp = 0;
                for (int j = (i * 8); j < Value.Length && j < (i * 8) + 8; j++)
                {
                    if (Value[j])
                        val |= Convert.ToByte(Math.Pow(2, nTemp));
                    nTemp++;
                }
                data[7 + i] = val;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            ManualWorkList.Add(new Work(data, 8, Repeat));
            Value = null;
            data = null;
        }
        internal void AutoMultiWordWrite(int Slave, int StartAddr, int[] Value, bool Repeat)
        {
            byte[] data = new byte[9 + (Value.Length * 2)];
            byte addrHi = 0, addrLow = 0;
            byte lenHi = 0, lenLow = 0;
            byte crcHi = 0xff, crcLo = 0xff;

            DevideShort(StartAddr, ref addrHi, ref addrLow);
            DevideShort(Value.Length, ref lenHi, ref lenLow);

            data[0] = Convert.ToByte(Slave);
            data[1] = 0x10;
            data[2] = addrHi;
            data[3] = addrLow;
            data[4] = lenHi;
            data[5] = lenLow;
            data[6] = Convert.ToByte(Value.Length * 2);

            for (int i = 0; i < Value.Length; i++)
            {
                byte valHi = 0, valLow = 0;
                DevideShort(Value[i], ref valHi, ref valLow);
                data[7 + (i * 2)] = valHi;
                data[8 + (i * 2)] = valLow;
            }

            GetCRC(data, data.Length - 2, ref crcHi, ref crcLo);
            data[data.Length - 2] = crcHi;
            data[data.Length - 1] = crcLo;
            AutoWorkList.Add(new Work(data, 8, Repeat));
            Value = null;
            data = null;
        }
        #endregion
        #region SetBit
        public void SetBit(int Slave, int Address, bool Value, bool Repeat)
        {
            if (IsRunning) ManualBitWrite(Slave, Address, Value, Repeat);
        }
        public void SetBits(int Slave, int Address, bool[] Values, bool Repeat)
        {
            if (IsRunning) ManualMultiBitWrite(Slave, Address, Values, Repeat);
        }
        public void SetWord(int Slave, int Address, int Value, bool Repeat)
        {
            if (IsRunning) ManualWordWrite(Slave, Address, Value, Repeat);
        }
        public void SetWords(int Slave, int Address, int[] Values, bool Repeat)
        {
            if (IsRunning) ManualMultiWordWrite(Slave, Address, Values, Repeat);
        }
        #endregion
        #endregion
        #endregion
    }

    #region Enumeration
    #region ModbusAddressType
    public enum ModbusAddressType { BIT, WORD }
    #endregion
    #region ModbusFunction
    public enum ModbusFunction
    {
        BITREAD_F1 = 1,
        BITREAD_F2 = 2,
        WORDREAD_F3 = 3,
        WORDREAD_F4 = 4,
        BITWRITE_F5 = 5,
        WORDWRITE_F6 = 6,
        MULTIBITWRITE_F15 = 15,
        MULTIWORDWRITE_F16 = 16
    }
    #endregion
    #endregion

    #region MonitorAddress
    public class ModbusAddress
    {
        public int Slave { get; set; }
        public int Address { get; set; }
        public ModbusAddressType AddressType { get; set; }
        public int Length { get; set; }

        public ModbusAddress() { }
        public ModbusAddress(int Slave, int Address, ModbusAddressType AddressType, int Length)
        {
            this.Slave = Slave;
            this.Address = Address;
            this.AddressType = AddressType;
            this.Length = Length;
        }

        internal J2ModbusRTUMaster.Work GetWork()
        {
            List<byte> lst = new List<byte>();
            int nResCount = 0;

            if (AddressType == ModbusAddressType.BIT)
            {
                #region Bit
                lst.Add(Convert.ToByte(Slave));
                lst.Add(0x01);
                lst.Add((byte)((Address & 0xFF00) >> 8));
                lst.Add((byte)(Address & 0xFF));
                lst.Add((byte)((Length & 0xFF00) >> 8));
                lst.Add((byte)(Length & 0xFF));
                byte crcHi = 0xff, crcLo = 0xff;
                J2ModbusRTUMaster.GetCRC(lst, 0, lst.Count, ref crcHi, ref crcLo);
                lst.Add(crcHi);
                lst.Add(crcLo);

                nResCount = Length / 8;
                if (Length % 8 != 0) nResCount++;
                nResCount += 5;
                #endregion
            }
            else if (AddressType == ModbusAddressType.WORD)
            {
                #region Word
                lst.Add(Convert.ToByte(Slave));
                lst.Add(0x03);
                lst.Add((byte)((Address & 0xFF00) >> 8));
                lst.Add((byte)(Address & 0xFF));
                lst.Add((byte)((Length & 0xFF00) >> 8));
                lst.Add((byte)(Length & 0xFF));
                byte crcHi = 0xff, crcLo = 0xff;
                J2ModbusRTUMaster.GetCRC(lst, 0, lst.Count, ref crcHi, ref crcLo);
                lst.Add(crcHi);
                lst.Add(crcLo);

                nResCount = Length * 2 + 5;
                #endregion
            }

            J2ModbusRTUMaster.Work ret = new J2ModbusRTUMaster.Work(lst.ToArray(), nResCount);
            return ret;
        }

        internal J2ModbusRTUMaster.Work GetTcpWork()
        {
            List<byte> lst = new List<byte>();
            int nResCount = 0;

            if (AddressType == ModbusAddressType.BIT)
            {
                #region Bit
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(6);
                lst.Add(Convert.ToByte(Slave));
                lst.Add(0x01);
                lst.Add((byte)((Address & 0xFF00) >> 8));
                lst.Add((byte)(Address & 0xFF));
                lst.Add((byte)((Length & 0xFF00) >> 8));
                lst.Add((byte)(Length & 0xFF));

                nResCount = Length / 8;
                if (Length % 8 != 0) nResCount++;
                nResCount += 9;
                #endregion
            }
            else if (AddressType == ModbusAddressType.WORD)
            {
                #region Word
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(0);
                lst.Add(6);
                lst.Add(Convert.ToByte(Slave));
                lst.Add(0x03);
                lst.Add((byte)((Address & 0xFF00) >> 8));
                lst.Add((byte)(Address & 0xFF));
                lst.Add((byte)((Length & 0xFF00) >> 8));
                lst.Add((byte)(Length & 0xFF));

                nResCount = Length * 2 + 9;
                #endregion
            }

            J2ModbusRTUMaster.Work ret = new J2ModbusRTUMaster.Work(lst.ToArray(), nResCount);
            return ret;
        }
    }
    #endregion
}
