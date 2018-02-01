using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        public static void Shutdown()
        {
            if (singleton_ != null)
            {
                singleton_.sharedWWB_.Kill();
                singleton_ = null;
            }
        }

        public void Initialize(IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            log_file_ = (string)parameters[".log_file"];
            if (log_file_ != null)
                File.WriteAllText(log_file_, "");

            string scriptroot = (string)parameters["scriptroot"];
            filesystem_ = new WWB.MyFileSystem(scriptroot);
            string root = filesystem_.Combine(null, null);

            bool reset = (bool)parameters["reset"];
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

            bool debug = (bool)parameters["debug"];
            bool sandboxed = (bool)parameters["sandboxed"];
            string secret = (string)parameters[".secret"];
            sharedWWB_ = new WWB.SharedWWB(basic =>
            {
                // configure basic
                basic.Secret = new Guid(secret);
                basic.Initialize();
                basic.EditOnly = !debug;
                basic.Sandboxed = sandboxed;
                basic.BlockedKeywords = "AboutWinWrapBasic Dialog GetFilePath InputBox MsgBox ShowPopupMenu";
                basic.VirtualFileSystem = filesystem_;
                return true; // synchronized editing
            }, log_file_ != null);
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
