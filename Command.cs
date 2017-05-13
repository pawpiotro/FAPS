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
        private byte[] code;

        public byte[] Code
        {
            get { return code; }
            set { code = value; }
        }

        public int nCode
        {
            get { return code[0]; }
            set { code[0] = Convert.ToByte(value); }
        }

        private byte[] size;

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

        private byte[] data;

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
        private byte[] noData = { 8, 12, 13, 14, 15, 255 };
        private byte[] needMoreData = { 1, 2, 5, 6, 7, 9, 10, 11, 51 };


    /*private bool needMore;

    public bool NeedMore
    {
        get { return needMore; }
        set { needMore = value; }
    }*/

    /*enum cmd
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
    }*/

        public Command()
        {
            code = new byte[1];
            size = new byte[4];
            data = null;
            id = null;
        }

        /*public Command()
        {
            code = new byte[1];
            size = new byte[4];
            data = null;
            id = null;
        }

        public Command()
        {
            code = new byte[1];
            size = new byte[4];
            data = null;
            id = null;
        }*/
        public Command getCmd(Socket socket) // return Command?
        {
            socket.ReceiveTimeout = 100;
            try
            {
                socket.Receive(Code);
                if (interpret())
                {
                    socket.Receive(Size);
                    IPAddress.NetworkToHostOrder(nSize);
                    data = new byte[nSize];
                    socket.Receive(Data)
                }
            } catch(SocketException se)
            {
                return null;
            }
            return this;
        }

        public bool sendCmd(Socket socket, byte c)
        {
            nCode = c;
            socket.Send(Code);
            return true;
        }

        public bool sendCmd(Socket socket, byte c, int s, Object d)
        {
            socket.SendTimeout = 100;
            try
            {
                nCode = c;
                socket.Send(Code);
                //nSize = sizeof(d); ?? UNSAFE
                nSize = s;
                socket.Send(Size);
                data = Encoding.ASCII.GetBytes(d.ToString()); // dunno 
                socket.Send(data);
            } catch(SocketException se)
            { 
                return false;
            }

            return true;
        }

        /*
        private void revSize()
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(size);
        }
        

        public void assignCmd(String _id)
        {
            id = _id;
        }

        private bool interpret()
        {
            if (needMoreData.Contains<byte>(code[0]))
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
