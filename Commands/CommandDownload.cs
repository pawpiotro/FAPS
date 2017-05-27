using System;
using System.Net;
using System.Text;

namespace FAPS.Commands
{
    class CommandDownload:Command
    {
        private int begin;
        private int end;
        private String filename;
        private String user;

        public int Begin{ get { return begin; } set { begin = value; } }
        public int End { get { return end; } set { end = value; } }
        public String User { get { return user; } set { user = value; } }
        public String Filename { get { return filename; } set { filename = value; } }

        public CommandDownload(NetworkFrame nf) : base(nf)
        {
            byte[] data = nf.Data;
            byte[] tmpBegin = new byte[4];
            byte[] tmpEnd = new byte[4];
            byte[] tmpString = new byte[data.Length-8];
            Array.Copy(data, 0, tmpBegin, 0, 4);
            Array.Copy(data, 4, tmpEnd, 0, 4);
            Array.Copy(data, 8, tmpString, 0, data.Length - 8);

            begin = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(tmpBegin, 0));
            end = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(tmpEnd, 0));

            String tmpData = Encoding.UTF8.GetString(tmpString);

            try
            {
                char[] separators = { ':' };
                String[] tmp = tmpData.Split(separators);
                filename = tmp[0];
                user = tmp[1];
            }
            catch (NullReferenceException)
            {
                throw new Exception();
            }
            catch (IndexOutOfRangeException)
            {
                throw new Exception();
            }
        }

        public override NetworkFrame toNetworkFrame()
        {
            byte[] tmpBegin = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(begin));
            byte[] tmpEnd = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(end));
            byte[] tmpString = Encoding.UTF8.GetBytes(filename + ":" + user);
            byte[] data = new byte[tmpBegin.Length + tmpEnd.Length + tmpString.Length];
            Array.Copy(tmpBegin, 0, data, 0, 4);
            Array.Copy(tmpEnd, 0, data, 4, 4);
            Array.Copy(tmpString, 0, data, 8, tmpString.Length);
            
            return new NetworkFrame(NetworkFrame.CMD.DOWNLOAD, data);
        }
    }
}
