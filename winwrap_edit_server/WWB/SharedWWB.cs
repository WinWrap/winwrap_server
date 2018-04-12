﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

using WinWrap.Basic;
using WinWrap.Basic.Classic;

namespace WWB
{
    public class SharedWWB : IDisposable
    {
        Thread thread_; // thread for shared WinWrap Basic
        BasicNoUIObj basic_; // shared WinWrap Basic
        bool kill_; // kill the thread
        bool killed_;
        EventWaitHandle dead_ = new EventWaitHandle(false, EventResetMode.ManualReset);
        EventWaitHandle basic_synchronize_done_ = new EventWaitHandle(false, EventResetMode.ManualReset);
        static object lock_ = new object();
        SynchronizingQueues response_sqs_ = new SynchronizingQueues();
        SynchronizingQueue log_sq_;

        public SharedWWB(Func<BasicNoUIObj, bool> configure, bool logging = false)
        {
            Logging = logging;

            EventWaitHandle ready = new EventWaitHandle(false, EventResetMode.AutoReset);
            thread_ = new Thread(() =>
            {
                WinWrap.Basic.Util.IgnoreDialogs = true;
                using (basic_ = new BasicNoUIObj())
                {
                    basic_.DoEvents += Basic__DoEvents;
                    basic_.Synchronizing += Basic__Synchronizing;
                    // configure basic
                    bool edit = configure?.Invoke(basic_) ?? false;
                    // start synchronizing
                    if (edit)
                        basic_.SynchronizedEdit = true;
                    else
                        basic_.Synchronized = true;

                    ready.Set();
                    while (!kill_)
                    {
                        Thread.Sleep(10);
                        ProcessRequestQueue();
                    }
                }

                killed_ = true;
                dead_.Set();
                basic_synchronize_done_.Dispose();
            });

            thread_.Start();
            ready.WaitOne();
            ready.Close();
        }

        public void Dispose()
        {
            if (dead_ != null && Kill())
            {
                dead_.Dispose();
                dead_ = null;
            }
        }

        public bool Kill()
        {
            kill_ = true;
            if (!killed_)
                dead_.WaitOne(1000);

            return killed_;
        }

        public bool Logging
        {
            get
            {
                return log_sq_ != null;
            }
            set
            {
                lock (lock_)
                {
                    if (value && log_sq_ == null)
                        log_sq_ = new SynchronizingQueue(0);
                    else
                        log_sq_ = null;
                }
            }
        }

        public string PullLog()
        {
            lock (lock_)
                return log_sq_?.DequeueAll();
        }

        private void ProcessRequestQueue()
        {
            // get everything from the synchronize queue
            basic_.Synchronize(null, 0);

            // do pending windows events
            WinWrap.Basic.Util.DoEvents();

            // all synchronize data has been sent
            basic_synchronize_done_.Set();
        }

        // called from main thread
        public void SendRequest(string param, int id)
        {
            if (param == null)
                param = "[]";

            bool attach = id == 0 && param != "[]";
            if (!killed_)
            {
                if (param != "[]")
                    lock (lock_)
                        log_sq_?.Enqueue(param);

                // send request
                basic_.Synchronize(param, id);
            }
        }

        // called from main thread
        public string GetResponse(int id, int maxwait = 0)
        {
            // return responses for the id
            for (int i = 0; i < maxwait/50; ++i)
            {
                // wait for up to 5 seconds
                lock (lock_)
                {
                    if (response_sqs_.Count > 0)
                        break;

                    Thread.Sleep(50);
                }
            }
                
            lock (lock_)
                return response_sqs_.Dequeue(id);
        }

        private void Basic__Synchronizing(object sender, SynchronizingEventArgs e)
        {
            // add to the synchronizing queue
            lock (lock_)
            {
                if (e.Param != "[]")
                    log_sq_?.Enqueue(e.Param);

                response_sqs_.Enqueue(e.Param, e.Id);
            }
        }

        private void Basic__DoEvents(object sender, EventArgs e)
        {
            ProcessRequestQueue();
            if (kill_)
                basic_.Run = false;
        }
    }
}
