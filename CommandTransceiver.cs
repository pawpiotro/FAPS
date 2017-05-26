using System;
using System.Net;
using System.Net.Sockets;

namespace FAPS
{
    class CommandTransceiver
    {
        private Socket socket;


        public CommandTransceiver(Socket _socket)
        {
            socket = _socket;
        }

        public Command getCmd()
        {
            Command cmd = new Command();
            socket.ReceiveTimeout = 10000;
            try
            {
                int rec = socket.Receive(cmd.Code);
                if (rec.Equals(0))
                    throw new Exception();
                if (cmd.needMoreData())
                {
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(cmd.Size, rec, 4 - rec, SocketFlags.None);
                    } while (rec < 4);
                    cmd.nSize = IPAddress.NetworkToHostOrder(cmd.nSize);
                    if(cmd.nSize > 25000)
                    {
                        throw new SocketException();
                    }
                    cmd.Data = new byte[cmd.nSize];
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(cmd.Data, rec, cmd.nSize - rec, SocketFlags.None);
                    } while (rec < cmd.nSize);
                }
                return cmd;
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        public void sendCmd(Command cmd)
        {
            try
            {
                int sent = socket.Send(cmd.Code);
                if (sent.Equals(0))
                    throw new Exception();
                if (cmd.needMoreData())
                {
                    sent = 0;
                    cmd.nSize = IPAddress.HostToNetworkOrder(cmd.nSize);
                    do
                    {
                        sent += socket.Send(cmd.Size, sent, 4 - sent, SocketFlags.None);
                    } while (sent < 4);
                    int hSize = IPAddress.NetworkToHostOrder(cmd.nSize);
                    Console.WriteLine(hSize);
                    Console.WriteLine(cmd.sData);
                    sent = 0;
                    do
                    {
                        sent += socket.Send(cmd.Data, sent, hSize - sent, SocketFlags.None);
                    } while (sent < hSize);
                }
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }
    }
}
