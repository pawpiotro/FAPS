using System;
using System.Net;
using System.Net.Sockets;
using FAPS.Commands;

namespace FAPS
{
    class CommandTransceiver
    {
        private Socket socket;
        private bool fromClient;

        public CommandTransceiver(Socket _socket, bool _fromClient)
        {
            socket = _socket;
            fromClient = _fromClient;
        }

        private NetworkFrame receiveNetworkFrame()
        {
            NetworkFrame nf = new NetworkFrame();
            socket.ReceiveTimeout = 10000;
            try
            {
                int rec = socket.Receive(nf.Code);
                if (rec.Equals(0))
                    throw new Exception();
                if (nf.needMoreData())
                {
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(nf.Size, rec, 4 - rec, SocketFlags.None);
                    } while (rec < 4);
                    nf.nSize = IPAddress.NetworkToHostOrder(nf.nSize);
                    if (nf.nSize > 25000)
                    {
                        throw new SocketException();
                    }
                    nf.Data = new byte[nf.nSize];
                    rec = 0;
                    do
                    {
                        rec += socket.Receive(nf.Data, rec, nf.nSize - rec, SocketFlags.None);
                    } while (rec < nf.nSize);
                }
                return nf;
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
        }

        public Command getCmd()
        {
            NetworkFrame nf;
            try
            {
                nf = receiveNetworkFrame();

                switch ((NetworkFrame.CMD)nf.nCode)
                {
                    case NetworkFrame.CMD.ACCEPT:
                        return new CommandAccept(nf);
                    case NetworkFrame.CMD.LOGIN:
                        return new CommandLogin(nf);
                    case NetworkFrame.CMD.LIST:
                        return new CommandList(nf);
                    case NetworkFrame.CMD.DOWNLOAD:
                        return new CommandDownload(nf);
                    case NetworkFrame.CMD.UPLOAD:
                        return new CommandUpload(nf);
                    case NetworkFrame.CMD.CHUNK:
                        return new CommandChunk(nf, fromClient);
                    case NetworkFrame.CMD.DELETE:
                        return new CommandDelete(nf);
                    case NetworkFrame.CMD.RENAME:
                        return new CommandRename(nf);
                    case NetworkFrame.CMD.ERROR:
                        return new CommandError(nf);
                    case NetworkFrame.CMD.EXIT:
                        return new CommandExit(nf);
                    default:
                        return null; // ERROR?
                }
            }
            catch(SocketException se)
            {
               throw new SocketException(se.ErrorCode);
            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        public void sendCmd(Command cmd)
        {
            NetworkFrame nf = cmd.toNetworkFrame();
            try
            {
                int sent = socket.Send(nf.Code);
                if (sent.Equals(0))
                    throw new Exception();
                if (nf.needMoreData())
                {
                    sent = 0;
                    nf.nSize = IPAddress.HostToNetworkOrder(nf.nSize);
                    do
                    {
                        sent += socket.Send(nf.Size, sent, 4 - sent, SocketFlags.None);
                    } while (sent < 4);
                    int hSize = IPAddress.NetworkToHostOrder(nf.nSize);
                    Console.WriteLine(hSize);
                    Console.WriteLine(nf.sData);
                    sent = 0;
                    do
                    {
                        sent += socket.Send(nf.Data, sent, hSize - sent, SocketFlags.None);
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
