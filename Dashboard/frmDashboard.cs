using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;

namespace Dashboard
{
    public partial class Dashboard : Form
    {
        //private enum ConnectMode : uint
        //{
        //    COMPORT = 0,
        //    TCP=1,
        //    UDP=2
        //}
        static short DeviceAddress = 1;

        Thread th_Daemon = new Thread(Daemon_thread);
    
        private static SerialPort m_SerialPort = new SerialPort();

        private static string sz_ip_address;
        private static string sz_port = "COM1";
        private static string sz_seialport;
        private static uint nud_Delay;
        private static int n_Band_Rate = 9600;
        private static uint n_connectmode;
        private static bool b_AutoConnection;
        private static bool b_Reconnection;

        
        static bool b_Connected;
        private bool ComSync = false;

        private const int ReadTimeout = 50;
        private const int WriteTimeout = 50;

        //public delegate void HandleInterfaceUpdataDelegate(byte[] msg);
        //private HandleInterfaceUpdataDelegate interfaceUpdataHandle;

        static MethodInvoker DisplayMsg=null;
        static MethodInvoker DisableMsg = null;

        public delegate void Data_UpdataDelegate(string msg);
        //private Data_UpdataDelegate Data_UpdataHandle = null;

        public Dashboard()
        {
            InitializeComponent();
            //tm_Daemon.Enabled = true;            
            CheckForIllegalCrossThreadCalls = false;
            if (DisplayMsg == null)
                DisplayMsg = new MethodInvoker(Display_Msg);//定义一个委托实例，该实例执行打开窗口代码
            if (DisableMsg == null)
                DisableMsg = new MethodInvoker(Disable_Msg);
            //if (Data_UpdataHandle == null)
            //    Data_UpdataHandle = new Data_UpdataDelegate(UpdateUI);
            //thread_run = true;
            th_Daemon.Start();                          // Run WriteY on the new thread
        }

        #region Open / Close Procedures
        public bool Serial_Open(string portName, int baudRate, int databits, Parity parity, StopBits stopBits)
        {
            //Ensure port isn't already opened:
            if (!m_SerialPort.IsOpen)
            {
                //Assign desired settings to the serial port:
                m_SerialPort.PortName = portName;
                m_SerialPort.BaudRate = baudRate;
                m_SerialPort.DataBits = databits;
                m_SerialPort.Parity = parity;
                m_SerialPort.StopBits = stopBits;
                //These timeouts are default and cannot be editted through the class at this point:
                m_SerialPort.ReadTimeout = 1000;
                m_SerialPort.WriteTimeout = 1000;

                try
                {
                    m_SerialPort.Open();
                    b_Connected = m_SerialPort.IsOpen;
                }
                catch (Exception err)
                {

                    return false;
                }

                return true;
            }
            else
            {

                return false;
            }
        }
        public bool Serial_Close()
        {
            //Ensure port is opened before attempting to close:
            if (m_SerialPort.IsOpen)
            {
                try
                {
                    m_SerialPort.Close();
                }
                catch (Exception err)
                {

                    return false;
                }

                return true;
            }
            else
            {

                return false;
            }
        }
        #endregion

        private void Display_Msg()
        {
            lbl_Msg.Visible = true;
            lbl_Msg.Text = "未连接通讯端口";
            LoadConfig();
        }

        private void Disable_Msg()
        {
            lbl_Msg.Visible = false;
        }

        static void Daemon_thread()
        {
            while (true)
            {
                if (!b_Connected)
                {                     
                    DisplayMsg.Invoke();
                }
                else
                {
                    //lbl_Msg.Visible = false;
                    DisableMsg.Invoke();

                    if (DeviceAddress < 3)
                    {
                        //1,2开关量设备
                        try
                        {
                            byte[] tempCMD = new byte[8];
                            tempCMD = Relay.CreateCMDByDebug(DeviceAddress, 0x10, 0x00, 0x00, 0x00, 0x00);
                            //SendCMDToDev(tempCMD);
                        }
                        catch { }
                    }

                    if (DeviceAddress == 3)
                    {
                        //温湿度传感器

                    }

                    if (DeviceAddress == 4)
                    {
                        //modbus设备

                    }
                }
            }
        }

        private void btn_exit_Click(object sender, EventArgs e)
        {
            //thread_run = false;
            th_Daemon.Abort();
            Close();
        }

        /// <summary>
        /// 读入连接配置
        /// </summary>
        private void LoadConfig()
        {
            try
            {
                string iniFile = string.Empty;
                iniFile = System.Environment.CurrentDirectory + "\\config.ini";
                if (System.IO.File.Exists(iniFile))
                {
                    sz_ip_address = INIFile.ReadValue(iniFile, "CONNECTION", "DOMAIN");
                    sz_port = INIFile.ReadValue(iniFile, "CONNECTION", "PORT");

                    sz_seialport = INIFile.ReadValue(iniFile, "CONNECTION", "SerialPort");

                    nud_Delay = Convert.ToUInt32(INIFile.ReadValue(iniFile, "CONNECTION", "Delay"));
                    n_Band_Rate = Convert.ToInt32(INIFile.ReadValue(iniFile, "CONNECTION", "Baud"));
                    n_connectmode = Convert.ToUInt32(INIFile.ReadValue(iniFile, "CONNECTION", "CONNMODE"));

                    //if (n_connectmode == "0")
                    //{
                    //    rb_COM.Checked = true;
                    //}
                    //else
                    //    if (n_connectmode == "1")
                    //    {
                    //        rb_TCP.Checked = true;

                    //    }
                    //    else
                    //        if (n_connectmode == "2")
                    //        {
                    //            rb_UDP.Checked = true;

                    //        }
                    //        else
                    //            if (n_connectmode == "2")
                    //            {
                    //                rb_UDP.Checked = true;

                    //            }

                    b_AutoConnection = Convert.ToBoolean(INIFile.ReadValue(iniFile, "CONNECTION", "AutoConnection"));
                    if (b_AutoConnection)
                    {
                        //BeginListen();
                        Serial_Open(sz_seialport, n_Band_Rate, 8, Parity.None, StopBits.One);
                    }
                    b_Reconnection = Convert.ToBoolean(INIFile.ReadValue(iniFile, "CONNECTION", "Reconnection"));
                }
            }
            catch { }
        }

        //private bool BeginListen()
        //{

        //    //COMPORT = 0,
        //    //TCP=1,
        //    //UDP=2
        //    //if (n_connectmode == 1)
        //    //{
        //    //    try
        //    //    {
        //    //        if (TCP_Connect(txtIP.Text, txtPort.Text))
        //    //        {
        //    //            btnBeginListen.Enabled = false;
        //    //            btnEndListen.Enabled = true;
        //    //            EnableBtn(true);
        //    //            rbCOM.Enabled = false;
        //    //            rb_TCP.Enabled = false;
        //    //            rb_UDP.Enabled = false;
        //    //            txtPort.Enabled = false;
        //    //            txtIP.Enabled = false;
        //    //        }

        //    //    }
        //    //    catch (Exception err)
        //    //    {
        //    //        MessageBox.Show(err.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    //    }
        //    //}

        //    //if (n_connectmode == 2)
        //    //{
        //    //    UDPServer_Start(txtIP.Text, txtPort.Text);

        //    //    btnBeginListen.Enabled = false;
        //    //    btnEndListen.Enabled = true;
        //    //    EnableBtn(true);
        //    //    rbCOM.Enabled = false;
        //    //    rb_TCP.Enabled = false;
        //    //    rb_UDP.Enabled = false;
        //    //    txtPort.Enabled = false;
        //    //    txtIP.Enabled = false;
        //    //}

        //    if (n_connectmode == 0)
        //    {
        //        try
        //        {
        //            if (m_SerialPort == null)
        //            {
        //                m_SerialPort = new SerialPort(sz_seialport, n_Band_Rate, Parity.None, 8);
        //                m_SerialPort.ReceivedBytesThreshold = 8;
        //                m_SerialPort.ReadBufferSize = 8;
        //                m_SerialPort.WriteBufferSize = 8;
        //                m_SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);//DataReceived事件委托
        //                //读写时间超时 
        //                m_SerialPort.DtrEnable = true;
        //                m_SerialPort.RtsEnable = true;
        //                m_SerialPort.ReadTimeout = ReadTimeout;
        //                m_SerialPort.WriteTimeout = WriteTimeout;
        //            }
        //            m_SerialPort.Open();
        //            interfaceUpdataHandle = new HandleInterfaceUpdataDelegate(SetDevState);
        //        }
        //        catch
        //        {

        //        }

        //        b_Connected = m_SerialPort.IsOpen;
        //    }

        //    //if (b_Connected)
        //    //{
        //    //    byte[] m_SendCMD = new byte[8];
        //    //    m_SendCMD = Relay.GetConfig();

        //    //    CMDList.Add(m_SendCMD);
        //    //    tm_Debug.Enabled = true;
        //    //}

        //    return b_Connected;
        //}

        ////DataReceived事件委托方法
        //private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    // if (ReceiveEventFlag) return;
        //    try
        //    {
        //        //Thread.Sleep(120);
        //        //if (BAUD_RATE == 57600)
        //        {
        //            byte[] dn = new byte[8];
        //            byte[] ds = new byte[m_SerialPort.BytesToRead];
        //            int i = 0;
        //            //int count = serialPort.BytesToRead / 8;

        //            if (m_SerialPort.BytesToRead == 11)
        //            {
        //                m_SerialPort.Read(ds, 0, m_SerialPort.BytesToRead);
        //                this.Invoke(interfaceUpdataHandle, ds);
        //            }
        //            else
        //            {
        //                //for (i = 0; i < count; i++)
        //                {
        //                    m_SerialPort.Read(dn, i * 8, 8);
        //                    this.Invoke(interfaceUpdataHandle, dn);
        //                }
        //            }
        //        }
        //        //else
        //        //{
        //        //    byte[] ds = new byte[serialPort.BytesToRead];
        //        //    int len = serialPort.Read(ds, 0, serialPort.BytesToRead);
        //        //    if (len >= 8) this.Invoke(interfaceUpdataHandle, ds);

        //        //}
        //        //serialPort.DiscardInBuffer();
        //        //serialPort.DiscardOutBuffer(); 
        //    }
        //    catch
        //    {
        //        //EndListen();
        //    }
        //}

        /// <summary>
        /// 变更设备状态
        /// </summary>
        /// <param name="statemsg"></param>
        private void SetDevState(byte[] statemsg)
        {
            //string hex = BitConverter.ToString(statemsg);
            //lbl_Receive.Text = hex;
            //fdebug.AddInfo(hex);

                if (statemsg[2] == 0x11)//湿度类 DHT11
                {
                    //statemsg[4]  湿度
                    //statemsg[6]  温度
                    //item.Text = item.Title + "\r\n湿度：" + (statemsg[6] - deviation_hm).ToString();
                    //item.msg = statemsg;
                    //decimal tv = Convert.ToDecimal(statemsg[6].ToString());

                }
                if (statemsg[2] == 0x12)//温度类 DS18B20
                {
                    //statemsg[5]  温度小数
                    //statemsg[6]  温度整数
                    //item.Text = item.Title + "\r\n温度：" + (statemsg[6] - deviation_temp).ToString() + "." + statemsg[5].ToString();
                    //item.msg = statemsg;
                    //decimal tv = Convert.ToDecimal(statemsg[6].ToString() + "." + statemsg[5].ToString());
                }


        }

        private void BTN_ONLine(object sender, EventArgs e)
        {
            short DevAdd = 1;
            int u_Line = (byte)Convert.ToUInt16(((Button)sender).Text);
            if (u_Line > 16)
            {
                DevAdd = 2;
                u_Line = u_Line - 16;
            }
            try
            {
                byte[] tempCMD = new byte[8];
                tempCMD = Relay.CreateCMDByDebug(DevAdd, 0x12, 0x00, 0x00, 0x00, u_Line);
                WriteCMDData(tempCMD);
            }
            catch { }
        }

        private void BTN_OFFLine(object sender, EventArgs e)
        {
            short DevAdd = 1;
            int u_Line = (byte)Convert.ToUInt16(((Button)sender).Text);
            if (u_Line > 16)
            {
                DevAdd = 2;
                u_Line = u_Line - 16;
            }
            try
            {
                byte[] tempCMD = new byte[8];
                tempCMD = Relay.CreateCMDByDebug(DevAdd, 0x11, 0x00, 0x00, 0x00, u_Line);
                WriteCMDData(tempCMD);
            }
            catch { }
        }

        private void WriteCMDData(byte[] m_SendCMD)
        {
            ComSync = true;
            try
            {
                //tm_Daemon.Enabled = false;
                if (n_connectmode == 0)
                {
                    Thread.Sleep(100);

                    try
                    {
                        if (m_SerialPort.IsOpen)
                        {
                            //Clear in/out buffers:
                            m_SerialPort.DiscardOutBuffer();
                            m_SerialPort.DiscardInBuffer();

                            m_SerialPort.Write(m_SendCMD, 0, m_SendCMD.Length);
                        }
                    }
                    catch
                    {
                        //EndListen();
                    }
                }
                //if (n_connectmode == 1)
                //{
                //    clientSendMsg(m_SendCMD);
                //}

                //if (n_connectmode == 2)
                //{
                //    UDP_Client_Send(m_SendCMD);

                //}
                //tm_Daemon.Enabled = true;
            }
            catch { }
            ComSync = false;
        }

        private void btn_FullyON_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] tempCMD = new byte[8];
                tempCMD = Relay.CreateCMDByDebug(DeviceAddress, 0x13, 0x00, 0x00, 0xFF, 0xFF);
                WriteCMDData(tempCMD);
            }
            catch { }
        }

        private void btn_FullyOFF_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] tempCMD = new byte[8];
                tempCMD = Relay.CreateCMDByDebug(DeviceAddress, 0x13, 0x00, 0x00, 0x00, 0x00);
                WriteCMDData(tempCMD);
            }
            catch { }
        }

        private void btn_RESTART_Click(object sender, EventArgs e)
        {
            ExitWindows.Reboot();
        }

        bool WriteModbusData(MBRTU_Master ModbusRtu, byte SlaveAddr, ushort ModbusAddr, string datatype, string sValue)
        {
            ComSync = true;
            //modbus 写延时
            Thread.Sleep(100);
            ushort ntype = (ushort)(ModbusAddr / 10000);
            ushort naddr = (ushort)(ModbusAddr % 10000);
            bool bok = false;
            naddr = (ushort)(naddr - 1);
            switch (ntype)
            {
                case 0://DO
                    short[] coils = new short[1];
                    if (sValue.Equals("1"))
                    {
                        coils[0] = 1;
                    }
                    else
                    {
                        coils[0] = 0;
                    }
                    bok = ModbusRtu.SendFc5(SlaveAddr, naddr, coils[0]);
                    break;
                case 4://AO
                    if (datatype.Equals("uint16"))
                    {
                        ushort[] registerhold = new ushort[1];
                        registerhold[0] = ushort.Parse(sValue);
                        bok = ModbusRtu.SendFc16(SlaveAddr, naddr, 1, registerhold);
                    }
                    else if (datatype.Equals("int16"))
                    {
                        ushort[] registerhold = new ushort[1];
                        registerhold[0] = (ushort)short.Parse(sValue);
                        bok = ModbusRtu.SendFc16(SlaveAddr, naddr, 1, registerhold);
                    }
                    else if (datatype.Equals("float"))
                    {
                        ushort[] registerhold = new ushort[2];
                        int intValue = int.Parse(sValue);
                        registerhold[1] = (ushort)(intValue >> 16);
                        registerhold[0] = (ushort)intValue;
                        bok = ModbusRtu.SendFc16(SlaveAddr, naddr, 2, registerhold);
                    }
                    else if (datatype.Equals("uint32"))
                    {
                        ushort[] registerhold = new ushort[2];
                        int intValue = int.Parse(sValue);
                        registerhold[1] = (ushort)(intValue >> 16);
                        registerhold[0] = (ushort)intValue;
                        bok = ModbusRtu.SendFc16(SlaveAddr, naddr, 2, registerhold);
                    }
                    else if (datatype.Equals("int32"))
                    {
                        ushort[] registerhold = new ushort[2];
                        int intValue = int.Parse(sValue);
                        registerhold[1] = (ushort)(intValue >> 16);
                        registerhold[0] = (ushort)intValue;
                        bok = ModbusRtu.SendFc16(SlaveAddr, naddr, 2, registerhold);
                    }
                    break;
            }
            ComSync = false;
            return bok;
        }
        void ReadModbusData(MBRTU_Master ModbusRtu, byte SlaveAddr, ushort ModbusAddr, string datatype, ushort nNumber, ref string[] sValue)
        {
            if (ComSync)
            {
                return;
            }
            ushort ntype = (ushort)((ModbusAddr / 10000));
            ushort naddr = (ushort)((ModbusAddr % 10000));
            naddr = (ushort)(naddr - 1);
            switch (ntype)
            {
                case 0://DO
                    ushort[] coils = new ushort[nNumber];
                    bool bcoils = ModbusRtu.SendFc1(SlaveAddr, naddr, nNumber, ref coils);
                    if (bcoils)
                    {
                        for (int i = 0; i < nNumber; i++)
                        {
                            sValue[i] = coils[i].ToString();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < nNumber; i++)
                        {
                            sValue[i] = "0";
                        }
                    }
                    break;
                case 1://DI
                    ushort[] dis = new ushort[nNumber];
                    bool bdis = ModbusRtu.SendFc2(SlaveAddr, naddr, nNumber, ref dis);
                    if (bdis)
                    {
                        for (int i = 0; i < nNumber; i++)
                        {
                            sValue[i] = dis[i].ToString();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < nNumber; i++)
                        {
                            sValue[i] = "0";
                        }
                    }
                    break;
                case 4://AO
                    if (datatype.Equals("uint16"))
                    {
                        ushort[] registerhold = new ushort[nNumber];
                        bool bhold = ModbusRtu.SendFc3(SlaveAddr, naddr, nNumber, ref registerhold);
                        if (bhold)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = registerhold[i].ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("int16"))
                    {
                        ushort[] registerhold = new ushort[nNumber];
                        bool bhold = ModbusRtu.SendFc3(SlaveAddr, naddr, nNumber, ref registerhold);
                        if (bhold)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = ((short)registerhold[i]).ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("float"))
                    {
                        ushort[] registerhold = new ushort[2 * nNumber];
                        bool bhold = ModbusRtu.SendFc3(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerhold);
                        if (bhold)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                int intValue = (int)registerhold[i * 2 + 1];
                                intValue <<= 16;
                                intValue += (int)registerhold[i * 2 + 0];
                                sValue[i] = BitConverter.ToSingle(BitConverter.GetBytes(intValue), 0).ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("int32"))
                    {
                        ushort[] registerhold = new ushort[2 * nNumber];
                        bool bhold = ModbusRtu.SendFc3(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerhold);
                        if (bhold)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                int intValue = (int)registerhold[2 * i + 1];
                                intValue <<= 16;
                                intValue += (int)registerhold[2 * i + 0];
                                sValue[i] = intValue.ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("uint32"))
                    {
                        ushort[] registerhold = new ushort[2 * nNumber];
                        bool bhold = ModbusRtu.SendFc3(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerhold);
                        if (bhold)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                UInt32 intValue = (UInt32)registerhold[2 * i + 1];
                                intValue <<= 16;
                                intValue += (UInt32)registerhold[2 * i + 0];
                                sValue[i] = intValue.ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    break;

                case 3://AI
                    if (datatype.Equals("uint16"))
                    {
                        ushort[] registerinput = new ushort[nNumber];
                        bool binput = ModbusRtu.SendFc4(SlaveAddr, naddr, nNumber, ref registerinput);
                        if (binput)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = registerinput[i].ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("int16"))
                    {
                        ushort[] registerinput = new ushort[nNumber];
                        bool binput = ModbusRtu.SendFc4(SlaveAddr, naddr, nNumber, ref registerinput);
                        if (binput)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = ((short)registerinput[i]).ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("float"))
                    {
                        ushort[] registerinput = new ushort[2 * nNumber];
                        bool binput = ModbusRtu.SendFc4(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerinput);
                        if (binput)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                int intValue = (int)registerinput[2 * i + 1];
                                intValue <<= 16;
                                intValue += (int)registerinput[2 * i + 0];
                                sValue[i] = BitConverter.ToSingle(BitConverter.GetBytes(intValue), 0).ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("int32"))
                    {
                        ushort[] registerinput = new ushort[2 * nNumber];
                        bool binput = ModbusRtu.SendFc4(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerinput);
                        if (binput)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                int intValue = (int)registerinput[2 * i + 1];
                                intValue <<= 16;
                                intValue += (int)registerinput[2 * i + 0];
                                sValue[i] = intValue.ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    else if (datatype.Equals("uint32"))
                    {
                        ushort[] registerinput = new ushort[2 * nNumber];
                        bool binput = ModbusRtu.SendFc4(SlaveAddr, naddr, (ushort)(2 * nNumber), ref registerinput);
                        if (binput)
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                UInt32 intValue = (UInt32)registerinput[2 * i + 1];
                                intValue <<= 16;
                                intValue += (UInt32)registerinput[2 * i + 0];
                                sValue[i] = intValue.ToString();
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nNumber; i++)
                            {
                                sValue[i] = "0";
                            }
                        }
                    }
                    break;
            }

        }

        private void GrayBtn(short address)
        {
            if (address == 1)//1号板
            {
                st_1.BackColor = Color.Gray;
                st_2.BackColor = Color.Gray;
                st_3.BackColor = Color.Gray;
                st_4.BackColor = Color.Gray;
                st_5.BackColor = Color.Gray;
                st_6.BackColor = Color.Gray;
                st_7.BackColor = Color.Gray;
                st_8.BackColor = Color.Gray;

                st_9.BackColor = Color.Gray;
                st_10.BackColor = Color.Gray;
                st_11.BackColor = Color.Gray;
                st_12.BackColor = Color.Gray;
                st_13.BackColor = Color.Gray;
                st_14.BackColor = Color.Gray;
                st_15.BackColor = Color.Gray;
                st_16.BackColor = Color.Gray;
            }
            if (address == 2)//2号板
            {
                st_17.BackColor = Color.Gray;
                st_18.BackColor = Color.Gray;
                st_19.BackColor = Color.Gray;
                st_20.BackColor = Color.Gray;
                st_21.BackColor = Color.Gray;
                st_22.BackColor = Color.Gray;
                st_23.BackColor = Color.Gray;
                st_24.BackColor = Color.Gray;

                st_25.BackColor = Color.Gray;
                st_26.BackColor = Color.Gray;
                st_27.BackColor = Color.Gray;
                st_28.BackColor = Color.Gray;
                st_29.BackColor = Color.Gray;
                st_30.BackColor = Color.Gray;
                st_31.BackColor = Color.Gray;
                st_32.BackColor = Color.Gray;
            }
        }
        private void GetResponse(short address)
        {
            byte[] tempCMD = new byte[8];
            tempCMD = Relay.CreateCMDByDebug(address, 0x10, 0x00, 0x00, 0x00, 0x00);
            WriteCMDData(tempCMD);



            //int timeout=Environment.TickCount;
            //while(((Environment.TickCount-timeout)<100)&&(m_SerialPort.BytesToRead<8))
            //{
            //    if (m_SerialPort.BytesToRead ==8)
            //    {
            //        m_SerialPort.Read(response, 0, m_SerialPort.BytesToRead);
            //    }
            //}

            //byte[] response = new byte[m_SerialPort.BytesToRead];
            //m_SerialPort.Read(response, 0, m_SerialPort.BytesToRead);

            //There is a bug in .Net 2.0 DataReceived Event that prevents people from using this
            //event as an interrupt to handle data (it doesn't fire all of the time).  Therefore
            //we have to use the ReadByte command for a fixed length as it's been shown to be reliable.
            int timeout = Environment.TickCount;
            byte[] response = new byte[8];
            for (int i = 0; i < response.Length; i++)
            {
                if (Environment.TickCount - timeout > 100)
                {
                    GrayBtn(address);
                    return;
                }
                response[i] = (byte)(m_SerialPort.ReadByte());
            }
            if (response.Length == 8)
            {
                int sum = 0;
                for (int i = 0; i <= 6; i++)
                {
                    sum = sum + response[i];
                }
                if ((response[0] == 0x22) && (response[2] == 0x10) && (response[7] == (byte)(sum % 256)))
                {
                    if (response[1] == 1)//1号板
                    {
                        if (Convert.ToBoolean(response[6] & 0x01)) st_1.BackColor = Color.Green; else st_1.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x02)) st_2.BackColor = Color.Green; else st_2.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x04)) st_3.BackColor = Color.Green; else st_3.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x08)) st_4.BackColor = Color.Green; else st_4.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x10)) st_5.BackColor = Color.Green; else st_5.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x20)) st_6.BackColor = Color.Green; else st_6.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x40)) st_7.BackColor = Color.Green; else st_7.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x80)) st_8.BackColor = Color.Green; else st_8.BackColor = Color.Red;

                        if (Convert.ToBoolean(response[5] & 0x01)) st_9.BackColor = Color.Green; else st_9.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x02)) st_10.BackColor = Color.Green; else st_10.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x04)) st_11.BackColor = Color.Green; else st_11.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x08)) st_12.BackColor = Color.Green; else st_12.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x10)) st_13.BackColor = Color.Green; else st_13.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x20)) st_14.BackColor = Color.Green; else st_14.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x40)) st_15.BackColor = Color.Green; else st_15.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x80)) st_16.BackColor = Color.Green; else st_16.BackColor = Color.Red;
                    }
                    if (response[1] == 2)//2号板
                    {
                        if (Convert.ToBoolean(response[6] & 0x01)) st_17.BackColor = Color.Green; else st_17.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x02)) st_18.BackColor = Color.Green; else st_18.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x04)) st_19.BackColor = Color.Green; else st_19.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x08)) st_20.BackColor = Color.Green; else st_20.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x10)) st_21.BackColor = Color.Green; else st_21.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x20)) st_22.BackColor = Color.Green; else st_22.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x40)) st_23.BackColor = Color.Green; else st_23.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[6] & 0x80)) st_24.BackColor = Color.Green; else st_24.BackColor = Color.Red;

                        if (Convert.ToBoolean(response[5] & 0x01)) st_25.BackColor = Color.Green; else st_25.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x02)) st_26.BackColor = Color.Green; else st_26.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x04)) st_27.BackColor = Color.Green; else st_27.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x08)) st_28.BackColor = Color.Green; else st_28.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x10)) st_29.BackColor = Color.Green; else st_29.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x20)) st_30.BackColor = Color.Green; else st_30.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x40)) st_31.BackColor = Color.Green; else st_31.BackColor = Color.Red;
                        if (Convert.ToBoolean(response[5] & 0x80)) st_32.BackColor = Color.Green; else st_32.BackColor = Color.Red;
                    }
                }
                else
                {
                    GrayBtn(address);
                }
            }
        }

        /// <summary>
        /// 守护进程
        /// </summary>        
        private void tm_Daemon_Tick(object sender, EventArgs e)
        {
            if (!b_Connected)
            {
                lbl_Msg.Visible = true;
                lbl_Msg.Text = "未连接通讯端口";
                LoadConfig();
            }
            else
            {
                lbl_Msg.Visible = false;
                if (DeviceAddress < 3)
                {
                    //1,2开关量设备
                    try
                    {
                        GetResponse(DeviceAddress);

                    }
                    catch { }
                }

                //if (DeviceAddress == 3)
                //{
                //    //温湿度传感器

                //}

                if (DeviceAddress == 3)
                {
                    //modbus设备

                }

                DeviceAddress++;
            }
        }
    }
}
