using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using WWB;

namespace winwrap_edit_server.WWB
{
    public class WinWrapBasicService
    {
        private SharedWWB sharedWWB = new SharedWWB(true);
        private string log_file;
        static private object lock_ = new object();

        public WinWrapBasicService()
        {
            log_file = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\WinWrapBasicService.txt";
            File.WriteAllText(log_file, "");
        }

        public string Synchronize(string param, int id)
        {
            AppendToLogFile();
            string response = sharedWWB.SendRequestAndGetResponse(param, id);
            AppendToLogFile();
            return response;
        }

        private void AppendToLogFile()
        {
            lock (lock_)
            {
                string log = sharedWWB.PullLog();
                if (log != null)
                    File.AppendAllText(log_file, log + "\r\n\r\n");
            }
        }

        public string PullLog()
        {
            return sharedWWB.PullLog();
        }
    }
}
