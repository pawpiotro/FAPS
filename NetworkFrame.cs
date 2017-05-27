using System;
using System.Linq;
using System.Net;
using System.Text;

namespace FAPS
{
    class NetworkFrame
    {
        private String id;      //whose NetworkFrame is this

        private byte[] code = new byte[1];
        private byte[] size = new byte[4];
        private byte[] data = null;

        // Code property
        public byte[] Code
        {
            get { return code; }
            set { code = value; }
        }

        public CMD eCode
        {
            get { return (CMD)code[0]; }
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

        public string sData
        {
            get { return Encoding.UTF8.GetString(data); }
            set { data = Encoding.UTF8.GetBytes(value); }
        }

        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        // Others
        private byte[] needMoreDataCmds = { 0x01, 0x02, 0x05, 0x06, 0x07, 0x09, 0x0a, 0x0b, 0x33 };

        public enum CMD
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
        public NetworkFrame()
        {
            id = null;
        }

        public NetworkFrame(CMD c)
        {
            eCode = c;
            id = null;
        }

        public NetworkFrame(CMD c, byte[] d)
        {
            eCode = c;
            nSize = d.Length;
            data = d;
        }

        public NetworkFrame(CMD c, String d)
        {
            eCode = c;
            nSize = d.Length;
            data = Encoding.UTF8.GetBytes(d);
            id = null;
        }

        // Other methods

        public bool needMoreData()
        {
            if (needMoreDataCmds.Contains<byte>(code[0]))
                return true;
            else
                return false;
        }
    }
}
