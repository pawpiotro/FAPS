using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FAPS
{
    class Command
    {
        private String id;      //whose command is this

        private byte[] code = new byte[1];
        private byte[] size = new byte[4];
        private byte[] data = null;

        // Code property
        public byte[] Code
        {
            get { return code; }
            set { code = value; }
        }

        public cmd eCode
        {
            get { return (cmd)code[0]; }
            set { code[0] = Convert.ToByte((int)value); }
        }

        public int nCode
        {
            get { return code[0]; }
            set { code[0] = Convert.ToByte(value); }
        }

        // Size property
        public byte[] Size
        {
            get { return size; }
            set { size = value; }
        }

        public int nSize
        {
            get { return BitConverter.ToInt32(size, 0); }
            set { size = BitConverter.GetBytes(value); }
        }

        // Data property
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }

        public String sData
        {
            get { return Encoding.ASCII.GetString(data); }
            set { data = Encoding.ASCII.GetBytes(value); }
        }

        // Others
        private byte[] needMoreDataCmds = { 0x01, 0x02, 0x05, 0x06, 0x07, 0x09, 0x0a, 0x0b, 0x33 };

        public enum cmd
        {
            INTRODUCE = 0x01,
            LOGIN = 0x02,
            LIST = 0x05,
            DOWNLOAD = 0x06,
            UPLOAD = 0x07,
            ACCEPT = 0x08,
            CHUNK = 0x09,
            DELETE = 0x0a,
            RENAME = 0x0b,
            COMMIT = 0x0c,
            ROLLBACK = 0x0d,
            COMMITRDY = 0x0e,
            COMMITACK = 0x0f,
            ERROR = 0x33,
            EXIT = 0xff
        }

        // Constructors
        public Command()
        {
            id = null;
        }

        public Command(cmd c)
        {
            eCode = c;
            id = null;
        }

        public Command(cmd c, String d)
        {
            eCode = c;
            nSize = IPAddress.HostToNetworkOrder(d.Length);
            data = Encoding.ASCII.GetBytes(d);
            id = null;
        }

        // Communication methods
        public void getCmd(Socket socket, String _id)
        {
            id = _id;
            socket.ReceiveTimeout = 1000;
            try
            {
                int rec = socket.Receive(Code);
                if (rec.Equals(0))
                    throw new Exception();
                if (needMoreData())
                {
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(Size, rec, 4 - rec, SocketFlags.None);
                    } while (rec < 4);
                    nSize = IPAddress.NetworkToHostOrder(nSize);
                    data = new byte[nSize];
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(Data, rec, nSize - rec, SocketFlags.None);
                    } while (rec < nSize);
                }
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        public void sendCmd(Socket socket)
        {
            socket.SendTimeout = 1000;
            try
            {
                socket.Send(Code);
                if (needMoreData())
                {
                    int sent = 0;
                    do
                    {
                        sent += socket.Send(Size, sent, 4 - sent, SocketFlags.None);
                    } while (sent < 4);
                    int hSize = IPAddress.NetworkToHostOrder(nSize);
                    sent = 0;
                    do
                    {
                        sent += socket.Send(Data, sent, hSize - sent, SocketFlags.None);
                    } while (sent < hSize);
                }
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        // Other methods
        private bool needMoreData()
        {
            if (needMoreDataCmds.Contains<byte>(code[0]))
                return true;
            else
                return false;

            /*cmd cmd = (cmd)code[0];
            switch(cmd)
            {
                case cmd.INTRODUCE:
                    break;
                case cmd.LOGIN:
                    break;
                case cmd.LIST:
                    break;
                case cmd.DOWNLOAD:
                    break;
                case cmd.UPLOAD:
                    break;
                case cmd.ACCEPT:
                    break;
                case cmd.CHUNK:
                    break;
                case cmd.DELETE:
                    break;
                case cmd.RENAME:
                    break;
                case cmd.COMMIT:
                    break;
                case cmd.ROLLBACK:
                    break;
                case cmd.COMMITRDY:
                    break;
                case cmd.COMMITACK:
                    break;
                case cmd.ERROR:
                    break;
                case cmd.EXIT:
                    break;
            }*/
        }
    }
}
