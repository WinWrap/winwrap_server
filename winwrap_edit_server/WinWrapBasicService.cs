using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using WinWrap.Basic;

namespace winwrap_edit_server
{
    public class WinWrapBasicService
    {
        BasicThread basic_thread_ = new BasicThread();
        WinWrap.Basic.IVirtualFileSystem filesystem_;
        string log_file_;
        static object lock_ = new object();
        static WinWrapBasicService singleton_;
        WWB.SynchronizingQueues responses_sqs_ = new WWB.SynchronizingQueues();

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
                singleton_.basic_thread_.Kill();
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
                foreach (string res_name in WWB.Util.GetResourceFileNames("Samples"))
                {
                    string sample = WWB.Util.ReadResourceTextFile("Samples." + res_name, false);
                    string file_name = root + "\\" + res_name;
                    File.WriteAllText(file_name, sample);
                }
            }

            bool debug = (bool)parameters["debug"];
            bool sandboxed = (bool)parameters["sandboxed"];
            SynchronizationContext sc = new SynchronizationContext();
            basic_thread_.SendAction(basic =>
            {
                // configure basic
                basic.Synchronizing += (sender, e) =>
                {
                    // response/notification from the remote BasicNoUIObj
                    sc.Post(state => {
                        Log(e.Param);
                        lock (lock_)
                            responses_sqs_.Enqueue(e.Param, e.Id);
                    }, null);
                };
                basic.ReceivedAppSyncMessage += (sender, e) =>
                {
                    basic.SendAppSyncMessage(e.Data, -1);
                };
                Util.IgnoreDialogs = true;
                basic.Secret = new Guid(Secret.MySecret);
                basic.Initialize();
                basic.EditOnly = !debug;
                basic.Sandboxed = sandboxed;
                basic.BlockedKeywords = "AboutWinWrapBasic Beep Dialog GetFilePath InputBox MsgBox ShowPopupMenu";
                basic.VirtualFileSystem = filesystem_;
                basic.SynchronizedEdit = true; // synchronized editing
            });
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
                if (log_file_ != null)
                    File.WriteAllText(log_file_, "");
            }
        }

        public string GetResponses(SortedSet<int> idset)
        {
            // keep ids alive
            basic_thread_?.PostAction(basic =>
            {
                foreach (int id in idset)
                    basic.Synchronize("[]", id);
            });

            WWB.SynchronizingQueue sq = new WWB.SynchronizingQueue(0);
            lock (lock_)
                foreach (int id in idset)
                    sq.Enqueue(responses_sqs_.Dequeue(id));

            return sq.DequeueAll();
        }

        public void SendRequests(string requests)
        {
            Log(requests);
            basic_thread_?.PostAction(basic => basic.Synchronize(requests, 0));
        }

        private void Log(string text)
        {
            if (log_file_ != null && text != "[]")
            {
                if (text.StartsWith("[") && text.EndsWith("]"))
                    text = text.Substring(1, text.Length - 2);

                text = text.Replace("\r\n{", "_{");
                text = text.Replace("\r\n", "") + "\r\n";
                text = text.Replace("_{", "\r\n{");
                text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss\r\n") + text;
                lock (lock_)
                    File.AppendAllText(log_file_, text + "\r\n");

                if (text.IndexOf("\"response\":\"!attach\"") >= 0 || text.IndexOf("\"response\":\"!detached\"") >= 0)
                    Console.WriteLine(text);
            }
        }
    }
}
