using System;
using System.Net;
using System.Text;

namespace FAPS.Commands
{
    class CommandUpload:Command
    {
        private long size;
        private String filename;
        private String user;

        public long Size { get { return size; } set { size = value; } }
        public String User { get { return user; } set { user = value; } }
        public String Filename { get { return filename; } set { filename = value; } }

        public CommandUpload(NetworkFrame nf) : base(nf)
        {
            byte[] data = nf.Data;
            byte[] tmpSize = new byte[8];
            byte[] tmpString = new byte[data.Length - 8];
            Array.Copy(data, 0, tmpSize, 0, 8);
            Array.Copy(data, 8, tmpString, 0, data.Length - 8);

            size = IPAddress.NetworkToHostOrder(BitConverter.ToInt64(tmpSize, 0));

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
            byte[] tmpSize = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(size));
            byte[] tmpString = Encoding.UTF8.GetBytes(filename + ":" + user);
            byte[] data = new byte[tmpSize.Length + tmpString.Length];
            Array.Copy(tmpSize, 0, data, 0, 8);
            Array.Copy(tmpString, 0, data, 8, tmpString.Length);

            return new NetworkFrame(NetworkFrame.CMD.UPLOAD, data);
        }
    }
}
