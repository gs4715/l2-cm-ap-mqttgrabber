using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using OPC;
using OPCDA;
using OPCDA.NET;

namespace MQTTGrabber
{
    public class OPCObject
    {
        private SyncIOGroup srwGroup;
        public OpcServer Srv;

        public OPCObject(string serverName, string hostName, string user, string password)
        {
            Srv = new OpcServer();
            OPC.Common.Host accessInfo = new OPC.Common.Host();
            accessInfo.HostName = hostName;
            accessInfo.UserName = user;
            accessInfo.Password = password;
            
            Srv.Connect(accessInfo, serverName);

            srwGroup = new SyncIOGroup(Srv);
        }
        public void WriteValue(string key, object value)
        {
            srwGroup.Write(key, value);
        }
    }
}
