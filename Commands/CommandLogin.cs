using System;

namespace FAPS.Commands
{
    class CommandLogin:Command
    {
        private String user;
        private String pass;

        public String User{ get { return user; } set { user = value; }}
        public String Pass { get { return pass; } set { pass = value; } }


        public CommandLogin(String _user, String _pass)
        {
            User = _user;
            Pass = _pass;
        }

        public CommandLogin(NetworkFrame nf):base(nf)
        {
            try
            {
                char[] separators = { ':' };
                String[] tmp = nf.sData.Split(separators);
                user = tmp[0];
                pass = tmp[1];
            } catch(NullReferenceException)
            {
                throw new Exception();
            } catch(IndexOutOfRangeException)
            {
                throw new Exception();
            }
        }
        public override NetworkFrame toNetworkFrame()
        {
            String data = user + ":" + pass;
            return new NetworkFrame(NetworkFrame.CMD.LOGIN, data);
        }
    }
}
