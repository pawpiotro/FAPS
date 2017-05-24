﻿using System;

namespace FAPS
{
    class CommandProcessor
    {
        ClientSession clientSession;
        public CommandProcessor(ClientSession cs)
        {
            clientSession = cs; 
        }

        public void processCommand(Command cmd)
        {
            switch (clientSession.State)
            {
                case ClientSession.STATE.unauthenticated:
                    switch ((Command.CMD)cmd.nCode)
                    {
                        case Command.CMD.LOGIN:
                            LOGIN(cmd);
                            break;
                        case Command.CMD.ERROR:
                            ERROR();
                            break;
                        case Command.CMD.EXIT:
                            EXIT();
                            break;
                        default:
                            ERROR();
                            break;
                    }
                    break;
                case ClientSession.STATE.idle:
                    switch ((Command.CMD)cmd.nCode)
                    {
                        case Command.CMD.LIST:
                            clientSession.Monitor.queueMisc(cmd);
                            break;
                        case Command.CMD.DOWNLOAD:
                            clientSession.Monitor.queueDownload(cmd);
                            break;
                        case Command.CMD.UPLOAD:
                            clientSession.Monitor.queueUpload(cmd);
                            break;
                        case Command.CMD.CHUNK:
                            break;
                        case Command.CMD.DELETE:
                            clientSession.Monitor.queueMisc(cmd);
                            break;
                        case Command.CMD.RENAME:
                            clientSession.Monitor.queueMisc(cmd);
                            break;
                        case Command.CMD.ERROR:
                            ERROR();
                            break;
                        case Command.CMD.EXIT:
                            EXIT();
                            break;
                        default:
                            ERROR();
                            break;
                    }
                    break;

                case ClientSession.STATE.waitForAccept:
                    switch ((Command.CMD)cmd.nCode)
                    {
                        case Command.CMD.ACCEPT:
                            ACCEPT();
                            break;
                        case Command.CMD.ERROR:
                            ERROR();
                            break;
                        case Command.CMD.EXIT:
                            EXIT();
                            break;
                        default:
                            ERROR();
                            break;
                    }
                    break;
            }



            /*
            switch ((Command.CMD)cmd.nCode)
            {
                case Command.CMD.INTRODUCE:
                    break;
                case Command.CMD.LOGIN:
                    if(clientSession.State.Equals(ClientSession.STATE.unauthenticated))
                        LOGIN(cmd);
                    break;
                case Command.CMD.LIST:
                    clientSession.Monitor.queueMisc(cmd);
                    break;
                case Command.CMD.DOWNLOAD:
                    clientSession.Monitor.queueDownload(cmd);
                    break;
                case Command.CMD.UPLOAD:
                    clientSession.Monitor.queueUpload(cmd);
                    break;
                case Command.CMD.ACCEPT:
                    if (clientSession.State.Equals(ClientSession.STATE.waitForAccept))
                        clientSession.State = ClientSession.STATE.accepted;
                    break;
                case Command.CMD.CHUNK:
                    break;
                case Command.CMD.DELETE:
                    clientSession.Monitor.queueMisc(cmd);
                    break;
                case Command.CMD.RENAME:
                    clientSession.Monitor.queueMisc(cmd);
                    break;
                case Command.CMD.COMMIT:
                    break;
                case Command.CMD.ROLLBACK:
                    break;
                case Command.CMD.COMMITRDY:
                    break;
                case Command.CMD.COMMITACK:
                    break;
                case Command.CMD.ERROR:
                    break;
                case Command.CMD.EXIT:
                    break;
                default:
                    break;
            }*/
        }

        private void LOGIN(Command cmd)
        {
            Console.WriteLine("CH: logging in...");
            char[] separators = { ':' };
            String[] tmp = cmd.sData.Split(separators);
            Console.WriteLine("CH: Login: {0} Pass: {1}", tmp[0], tmp[1]);
            clientSession.authenticate(tmp[0], tmp[1]);
            if(clientSession.State.Equals(ClientSession.STATE.idle))
            {
                Console.WriteLine("CH: Login successful");
                clientSession.ID = tmp[0];
                Command ctmp = new Command(Command.CMD.ACCEPT);
                clientSession.ToSend.Add(ctmp);
            }
            else
            {
                Console.WriteLine("CH: Login failed");
            }
        }

        private void EXIT()
        {
            clientSession.State = ClientSession.STATE.stop;
        }

        private void ACCEPT()
        {
            clientSession.State = ClientSession.STATE.idle;
        }

        private void ERROR()
        {
            Console.WriteLine("CH: Connection failed");
            clientSession.State = ClientSession.STATE.stop;
        }

   
    }
}
