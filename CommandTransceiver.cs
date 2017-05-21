using System;
using System.Net;
using System.Net.Sockets;

namespace FAPS
{
    class CommandTransceiver
    {

        public Command getCmd(Socket socket, String _id)
        {
            Command cmd = new Command();
            cmd.ID = _id;
            socket.ReceiveTimeout = 1000;
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

        public void sendCmd(Socket socket, Command cmd)
        {
            socket.SendTimeout = 1000;
            try
            {
                socket.Send(cmd.Code);
                if (cmd.needMoreData())
                {
                    int sent = 0;
                    do
                    {
                        sent += socket.Send(cmd.Size, sent, 4 - sent, SocketFlags.None);
                    } while (sent < 4);
                    int hSize = IPAddress.NetworkToHostOrder(cmd.nSize);
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
