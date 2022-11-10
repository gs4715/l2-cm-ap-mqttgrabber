using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace MQTTGrabber
{
    public partial class MQTTGrabberService : ServiceBase
    {
        private Thread MQTTThread;
        private OPCObject OPCServer;
        public Dictionary<string, Dictionary<string, string>> tagInfo = new Dictionary<string, Dictionary<string, string>>();
        public MQTTGrabberService()
        {
            InitializeComponent();
        }

        public class MQTTGrabberConfig
        {
            public string MQTTServer = string.Empty;
            public Dictionary<string, Dictionary<string, string>> tagInfo = new Dictionary<string, Dictionary<string, string>>();
            public List<string> MQTTTags = new List<string>();
            public string opcServerName = string.Empty;
            public string opcHostName = string.Empty;
            public string opcUser = string.Empty;
            public string opcPassword = string.Empty;
            
            public MQTTGrabberConfig(IConfigurationRoot config)
            {
                this.MQTTServer = config.GetSection("MQTTConfig").GetValue<string>("server");
                this.tagInfo = config.GetSection("tags").Get<Dictionary<string, Dictionary<string, string>>>();
                foreach(KeyValuePair<string, Dictionary<string, string>> kvp in tagInfo)
                {
                    this.MQTTTags.Add(kvp.Key);
                }
                this.opcServerName = config.GetSection("OPCConfig").GetValue<string>("serverName");
                this.opcHostName = config.GetSection("OPCConfig").GetValue<string>("hostName");
                this.opcUser = config.GetSection("OPCConfig").GetValue<string>("user");
                this.opcPassword = config.GetSection("OPCConfig").GetValue<string>("password");
            }
        }

        protected override void OnStart(string[] args)
        {
            WriteToFile("Service is started at " + DateTime.Now);

            try
            {
                MQTTGrabberConfig config = new MQTTGrabberConfig(new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build());

                tagInfo = config.tagInfo;

                MQTTObject mqtt = new MQTTObject(config.MQTTServer, config.MQTTTags);
                mqtt.MessageReceived += MQTTMessageReceived;
                MQTTThread = new Thread(new ThreadStart(mqtt.Start));
                MQTTThread.IsBackground = true;
                MQTTThread.Start();

                OPCServer = new OPCObject(config.opcServerName, config.opcHostName, config.opcUser, config.opcPassword);
            }
            catch (Exception e)
            {
                WriteToFile("Exception: " + e);
            }
        }
        protected override void OnStop()
        {
            MQTTThread.Join();
            WriteToFile("Service is stopped at " + DateTime.Now);
        }
        private void MQTTMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string name = tagInfo[e.Topic]["OPCTag"];
            dynamic value = Encoding.UTF8.GetString(e.Payload);
           
            // As of now MQTT is only sending char encoded byte array
            try
            {
                switch (tagInfo[e.Topic]["dataType"].ToString())
                {
                    case "Boolean":
                        value = bool.Parse(value); break;
                    case "Byte":
                        value = byte.Parse(value); break;
                    case "Short":
                        value = short.Parse(value); break;
                    case "Float":
                        value = float.Parse(value); break;
                    case "String":
                        break;
                    case "Raw":
                        value = e.Payload; break;
                }
                //WriteToFile("name: " + name + " value : " + value);
                OPCServer.WriteValue(name, value);
            }
            catch (Exception ex)
            {
                WriteToFile("Exception: " + ex);
            }
        }
        public static void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
