using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WWB
{
    public class WinWrapBasicService
    {
        private SharedWWB sharedWWB_ = new SharedWWB();
        private string log_file_;
        static private object lock_ = new object();
        static private WinWrapBasicService singleton_;

        public static WinWrapBasicService Singleton
        {
            get
            {
                lock (lock_)
                {
                    if (singleton_ == null)
                        singleton_ = new WinWrapBasicService();

                    return singleton_;
                }
            }
        }
        public WinWrapBasicService()
        {
        }

        public string LogFile
        {
            get
            {
                return log_file_;
            }
            set
            {
                log_file_ = value;
                sharedWWB_.Logging = log_file_ != null;
                if (log_file_ != null)
                    File.WriteAllText(log_file_, "");
            }
        }

        public string Synchronize(string param, int id)
        {
            AppendToLogFile();
            string response = sharedWWB_.SendRequestAndGetResponse(param, id);
            AppendToLogFile();
            return response;
        }

        private void AppendToLogFile()
        {
            lock (lock_)
            {
                string log = sharedWWB_.PullLog();
                if (log != null && log_file_ != null)
                    File.AppendAllText(log_file_, log + "\r\n\r\n");
            }
        }

        public string PullLog()
        {
            return sharedWWB_.PullLog();
        }
    }
}
