﻿using System;
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

        private NetworkFrame receiveNetworkFrame(int timeout = 10000)
        {
            NetworkFrame nf = new NetworkFrame();
            socket.ReceiveTimeout = timeout;
            try
            {
                int rec = socket.Receive(nf.Code);
                if (rec.Equals(0))
                    throw new Exception("Not responding");
                if (nf.needMoreData())
                {
                    rec = 0;
                    while (rec < 4)
                    {
                        rec += socket.Receive(nf.Size, rec, 4 - rec, SocketFlags.None);
                    }
                    nf.nSize = IPAddress.NetworkToHostOrder(nf.nSize);
                    if (nf.nSize > 10000)
                    {
                        throw new Exception("File too big");
                    }
                    nf.Data = new byte[nf.nSize];
                    rec = 0;
                    while (rec < nf.nSize)
                    {
                        rec += socket.Receive(nf.Data, rec, nf.nSize - rec, SocketFlags.None);
                    }
                    Console.WriteLine("FROM " + socket.RemoteEndPoint + ": Code=" + nf.eCode +" Size=" + nf.nSize);
                    if (nf.nSize < 100)
                        Console.Write(" Data=" + nf.sData);
                    Console.Write("\n");
                }
                else
                {
                    Console.WriteLine("FROM " + socket.RemoteEndPoint + ": Code=" + nf.eCode);
                }
                return nf;
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
            catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Command getCmd(int timeout = 10000)
        {
            NetworkFrame nf;
            try
            {
                nf = receiveNetworkFrame(timeout);

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
                    case NetworkFrame.CMD.COMMIT:
                        return new CommandCommit(nf);
                    case NetworkFrame.CMD.COMMITACK:
                        return new CommandCommitAck(nf);
                    case NetworkFrame.CMD.COMMITRDY:
                        return new CommandCommitRdy(nf);
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
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void sendCmd(Command cmd, int timeout = 0)
        {
            NetworkFrame nf = cmd.toNetworkFrame();
            socket.SendTimeout = timeout;
            try
            {
                int sent = socket.Send(nf.Code);
                if (sent.Equals(0))
                    throw new Exception("Not responding");
                if (nf.needMoreData())
                {
                    sent = 0;
                    int hSize = nf.nSize;
                    nf.nSize = IPAddress.HostToNetworkOrder(nf.nSize);
                    do
                    {
                        sent += socket.Send(nf.Size, sent, 4 - sent, SocketFlags.None);
                    } while (sent < 4);
                    //int hSize = IPAddress.NetworkToHostOrder(nf.nSize);
                    sent = 0;
                    do
                    {
                        sent += socket.Send(nf.Data, sent, hSize - sent, SocketFlags.None);
                    } while (sent < hSize);
                    Console.Write("  TO " + socket.RemoteEndPoint + ": Code=" + nf.eCode + " Size=" + hSize);
                    if (hSize < 100)
                        Console.Write(" Data=" + nf.sData);
                    Console.Write("\n");
                }
                else
                {
                    Console.WriteLine("  TO " + socket.RemoteEndPoint + ": Code=" + nf.eCode);
                }
                
            }
            catch (SocketException se)
            {
                throw new SocketException(se.ErrorCode);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

     
    }
}
