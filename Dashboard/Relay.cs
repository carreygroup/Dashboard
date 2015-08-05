#region License
//
//****************************************************************************
// *
// * Copyright (c) Caesar LAB. All Rights Reserved.
// *
// * This software is the confidential and proprietary information of Caesar LAB ("Confidential Information").  
// * You shall not disclose such Confidential Information and shall use it only in
// * accordance with the terms of the license agreement you entered into with Caesar LAB.
// *
// * CAESAR LAB MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY OF THE
// * SOFTWARE, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// * IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// * PURPOSE, OR NON-INFRINGEMENT. CRELAB SHALL NOT BE LIABLE FOR ANY DAMAGES
// * SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR DISTRIBUTING
// * THIS SOFTWARE OR ITS DERIVATIVES.
// *
// * Original Author: Caesar LAB
// * Last checked in by $Author$
// * $Id$
// */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.Ports;

namespace Dashboard
{
    public enum RelayType : uint
    {
        /// <summary>
        /// 开
        /// </summary>
        ON =0x12,
        /// <summary>
        /// 关
        /// </summary>
        OFF =0x11,
        /// <summary>
        /// 位控制
        /// </summary>
        BYTECTRL=0x13,
        ///
        /// 
        /// 
        GROUPON=0x15,
        GROUPOFF=0x14,
        /// <summary>
        /// 查询
        /// </summary>
        INQUIRE=0x10,

        FLASH=0x17,
        INCHING=0x18,
        OFFONINCHING = 0x09,
        QUERYREG=0x0A
    }

    /// <summary>
    /// 
    /// </summary>
    public class Relay
    {
        public static uint ProtocolHead = 0x55; //协议头

        private static string PORT_NAME = "COM1";
        private static short Address;
        private static int Line;
        private static RelayType type;

        private static SerialPort serialPort = null;        
        private const int BAUD_RATE = 9600;

        private const int ReadTimeout = 50;
        private const int WriteTimeout = 50;

        /// <summary>
        /// 开关切换
        /// </summary>
        /// <param name="_PORT_NAME">端口</param>
        /// <param name="_Address">地址</param>
        /// _Line 第几路  _type  操作类型
        public static void Switch(string _PORT_NAME, short _Address, int _Line, RelayType _type)
        {
            PORT_NAME = _PORT_NAME;
            Address = _Address;
            Line = _Line;
            type = _type;

            Thread thread = new Thread(new ThreadStart(Switch));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        //DataReceived事件委托方法
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(120);
            int DataLength = serialPort.BytesToRead;
            int i = 0;
            StringBuilder sb = new StringBuilder();
            while (i < DataLength)
            {
                byte[] ds = new byte[1024];
                int len = serialPort.Read(ds, 0, 1024);
                sb.Append(Encoding.ASCII.GetString(ds, 0, len));
                i += len;
            }
        }

        private static void Switch()
        {
            try
            {
                if (serialPort == null)
                {
                    serialPort = new SerialPort(PORT_NAME, BAUD_RATE, Parity.None, 8);
                    serialPort.ReceivedBytesThreshold = 4;
                    serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);//DataReceived事件委托
                    //读写时间超时            
                    serialPort.ReadTimeout = ReadTimeout;
                    serialPort.WriteTimeout = WriteTimeout;
                    serialPort.Open();
                }

                if (serialPort.IsOpen)
                {
                    serialPort.Write(CreateCMD(Address, Line, type), 0, 8);
                }
            }
            catch
            {
            }
        }
        //创建命令
        public static byte[] CreateCMDByFlash(short _Address, int _Line, int times)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)RelayType.FLASH;
            command[3] = 0;
            command[4] = (byte)times;
            command[5] = (byte)(_Line >> 8);
            command[6] = (byte)_Line;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }
        //创建命令
        public static byte[] CreateCMDByInching(short _Address, int _Line, int times)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)RelayType.INCHING;
            command[3] = 0;
            command[4] = 0;
            command[5] = (byte)times;
            command[6] = (byte)_Line;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }

        //创建命令
        public static byte[] CreateCMDByDebug(short _Address,int Option,int data1,int data2,int data3,int data4)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)Option;
            command[3] = (byte)data1;
            command[4] = (byte)data2;
            command[5] = (byte)data3;
            command[6] = (byte)data4;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }

        //创建命令
        public static byte[] CreateCMDByOFFInching(short _Address, int _Line, int times)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)RelayType.OFFONINCHING;
            command[3] = 0;
            command[4] = 0;
            command[5] = (byte)times;
            command[6] = (byte)_Line;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }

        //创建命令
        public static byte[] CreateCMD(short _Address, int _Line, RelayType _type)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)_type;
            command[3] = 0;
            command[4] = 0;
            command[5] = (byte)(_Line >> 8);
            command[6] = (byte)_Line;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }
        //创建命令
        public static byte[] CreateCMD(short _Address,short OH,short OL, short _type)
        {
            byte[] command = new byte[8];
            command[0] = (byte)ProtocolHead;
            command[1] = (byte)(_Address % 256);
            command[2] = (byte)_type;
            command[3] = 0;
            command[4] = 0;
            command[5] = (byte)OH;
            command[6] = (byte)OL;
            int sum = 0;
            for (int i = 0; i <= 6; i++)
            {
                sum = sum + command[i];
            }
            command[7] = (byte)(sum % 256);
            return command;
        }
        //分析输出状态（继电器）
        public static int GetState(byte[] arrRecMsg,int Line)
        {
            int state = (arrRecMsg[5] << 8) + arrRecMsg[6];
            return (state >> (Line - 1)) & 1;
        }
        //分析输入状态（开关量输入）
        public static int GetInputState(byte[] arrRecMsg, int Line)
        {
            int state = (arrRecMsg[3] << 8) + arrRecMsg[4];
            return (state >> (Line - 1)) & 1;
        }

        //创建命令
        public static byte[] CreateCMDBySet(ushort _Address, byte Option, byte RegH, byte RegL, byte data1, byte data2, byte data3, byte data4, byte data5, byte data6, byte data7, byte data8)
        {
            //Array to receive CRC bytes:
            byte[] command = new byte[15];
            command[0] = (byte)0xAA;
            command[1] = (byte)0x00;
            command[2] = (byte)(_Address % 256);
            command[3] = (byte)Option;
            command[4] = (byte)RegH;
            command[5] = (byte)RegL;
            command[6] = (byte)data1;
            command[7] = (byte)data2;
            command[8] = (byte)data3;
            command[9] = (byte)data4;
            command[10] = (byte)data5;
            command[11] = (byte)data6;
            command[12] = (byte)data7;
            command[13] = (byte)data8;
            int sum = 0;
            for (int i = 0; i <= 13; i++)
            {
                sum = sum + command[i];
            }
            command[14] = (byte)(sum % 256);
            return command;
        }

    }
}
