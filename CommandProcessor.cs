using System;

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
            switch((Command.CMD)cmd.nCode)
            {
                case Command.CMD.INTRODUCE:
                    break;
                case Command.CMD.LOGIN:
                    LOGIN(cmd);
                    break;
                case Command.CMD.LIST:
                    break;
                case Command.CMD.DOWNLOAD:
                    break;
                case Command.CMD.UPLOAD:
                    break;
                case Command.CMD.ACCEPT:
                    break;
                case Command.CMD.CHUNK:
                    break;
                case Command.CMD.DELETE:
                    break;
                case Command.CMD.RENAME:
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
            }
        }

        private void LOGIN(Command cmd)
        {
            Console.WriteLine("logging in...");
            char[] separators = { ':' };
            String[] tmp = cmd.sData.Split(separators);
            Console.WriteLine("Login: {0} Pass: {1}", tmp[0], tmp[1]);
            clientSession.authenticate(tmp[0], tmp[1]);
            if(clientSession.State.Equals(ClientSession.STATE.logged))
            {
                clientSession.ID = tmp[0];
            }
            else
            {
                Console.WriteLine("Invalid login or password");
            }
        }
    }
}
