using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace winwrap_edit_server
{
    public class WinWrapBasicService
    {
        private WWB.SharedWWB sharedWWB_;
        private WinWrap.Basic.IVirtualFileSystem filesystem_;
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
        private WinWrapBasicService()
        {
        }

        public void Initialize(bool reset, string log_file)
        {
            log_file_ = log_file;
            if (log_file_ != null)
                File.WriteAllText(log_file_, "");

            filesystem_ = new WWB.MyFileSystem("WebEditServer");
            string root = filesystem_.Combine(null, null);
            if (!Directory.Exists(root))
            {
                reset = true;
                Directory.CreateDirectory(root);
            }

            if (reset)
            {
                // copy samples to the virtual file system
                foreach (string res_name in Util.GetResourceFileNames("Samples"))
                {
                    string sample = Util.ReadResourceTextFile("Samples." + res_name, false);
                    string file_name = root + "\\" + res_name;
                    File.WriteAllText(file_name, sample);
                }
            }

            sharedWWB_ = new WWB.SharedWWB(filesystem_, log_file_ != null);
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
