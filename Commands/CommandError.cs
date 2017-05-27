using System;
using System.Net;

namespace FAPS.Commands
{
    class CommandError:Command
    {
        private int errorCode;
        public int ErrorCode
        {
            get { return errorCode; }
            set { errorCode = value; }
        }

        public CommandError(NetworkFrame nf) : base(nf)
        {
            byte[] tmp = nf.Data;
            errorCode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(tmp,0));
        }

        public override NetworkFrame toNetworkFrame()
        {
            byte[] tmp = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(errorCode));
            return new NetworkFrame(NetworkFrame.CMD.ERROR, tmp);
        }
    }
}
