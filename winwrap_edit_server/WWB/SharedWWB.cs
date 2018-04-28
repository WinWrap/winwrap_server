using System;
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
    public class SharedWWB : SynchronizationContext, IDisposable
    {
        Thread thread_; // thread for shared WinWrap Basic
        BasicNoUIObj basic_; // shared WinWrap Basic
        Queue<WorkItem> callback_queue_ = new Queue<WorkItem>();
        Semaphore queue_semaphore_ = new Semaphore(0, int.MaxValue);
        ManualResetEvent send_wait_handle_ = new ManualResetEvent(false);
        bool kill_; // kill the thread
        object lock_ = new object();
        SynchronizingQueues responses_sqs_ = new SynchronizingQueues();
        SynchronizingQueue log_sq_;

        public SharedWWB(bool logging = false)
        {
            Logging = logging;

            using (EventWaitHandle ready = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                thread_ = new Thread(() =>
                {
                    WinWrap.Basic.Util.IgnoreDialogs = true;
                    using (basic_ = new BasicNoUIObj())
                    {
                        basic_.DoEvents += Basic__DoEvents;
                        basic_.Synchronizing += Basic__Synchronizing;

                        ready.Set();
                        while (!kill_)
                            ProcessRequestQueue();
                    }
                });

                thread_.Start();
                ready.WaitOne();
            }
        }

        public void Dispose()
        {
            Kill();
            queue_semaphore_.Dispose();
            send_wait_handle_.Dispose();
        }

        public bool Kill()
        {
            if (thread_ != null)
            {
                kill_ = true;
                if (!thread_.Join(1000))
                    return false;

                thread_ = null;
            }

            return true;
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
            if (basic_.IsInitialized() && basic_.Synchronized)
                // process everything from the synchronize queue
                basic_.Synchronize(null, 0);

            // do pending windows events
            WinWrap.Basic.Util.DoEvents();

            while (!kill_)
            {
                if (!queue_semaphore_.WaitOne(50))
                    break;

                WorkItem work_item = null;
                lock (callback_queue_)
                    if (callback_queue_.Count > 0)
                        work_item = callback_queue_.Dequeue();

                if (work_item == null)
                    break;

                work_item.Execute();
            }
        }

        // called from main thread
        public void SendRequests(string param)
        {
            if (thread_ != null)
            {
                if (param != null && param != "[]")
                    lock (lock_)
                        log_sq_?.Enqueue(param);

                // send request
                basic_.Synchronize(param, 0);
            }
        }

        // called from main thread
        public string GetResponses(int id)
        {
            // refresh detach timer
            basic_.Synchronize("[]", id);
            lock (lock_)
                return responses_sqs_.Dequeue(id);
        }

        // called from main thread
        public string GetResponses(SortedSet<int> idset)
        {
            SynchronizingQueue sq = new SynchronizingQueue(0);
            foreach (int id in idset)
                sq.Enqueue(GetResponses(id));
                
            return sq.DequeueAll();
        }

        private void Basic__Synchronizing(object sender, SynchronizingEventArgs e)
        {
            // add to the synchronizing queue
            lock (lock_)
            {
                if (e.Param != "[]")
                    log_sq_?.Enqueue(e.Param);

                responses_sqs_.Enqueue(e.Param, e.Id);
            }
        }

        private void Basic__DoEvents(object sender, EventArgs e)
        {
            ProcessRequestQueue();
            if (kill_)
                basic_.Run = false;
        }

        public void PostAction(Action<BasicNoUIObj> action, Action<Exception> completed)
        {
            SynchronizationContext sc = SynchronizationContext.Current;
            Post(state =>
            {
                Exception ex = null;
                try
                {
                    // execute action handler in the SharedWWB synchronization context
                    action(basic_);
                }
                catch (Exception e)
                {
                    ex = e;
                }

                // execute completed handler in caller's synchronization context
                sc.Post(state2 => completed(ex), null);
            }, null);
        }

        public void SendAction(Action<BasicNoUIObj> action)
        {
            Send(state => action(basic_), null);
        }

        #region SynchronizationContext

        public override void Send(SendOrPostCallback d, object state)
        {
            WorkItem work_item = EnqueueWorkItem(d, state);
            // wait for the item execution to end
            // if there was an exception, throw it on the caller thread, not the
            // sta thread.
            work_item.WaitForSendComplete();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            // queue the item and don't wait for its execution. This is risky because
            // an unhandled exception will terminate the thread. Use with caution.
            EnqueueWorkItem(d, state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        #endregion SynchronizationContext

        private WorkItem EnqueueWorkItem(SendOrPostCallback d, object state)
        {
            // create an item for execution
            WorkItem work_item = new WorkItem(d, state, send_wait_handle_);
            // queue the item
            lock (callback_queue_)
                callback_queue_.Enqueue(work_item);

            queue_semaphore_.Release();
            return work_item;
        }
    }

    internal class WorkItem
    {
        object state_;
        SendOrPostCallback callback_;
        ManualResetEvent wait_handle_;
        Exception ex_;

        public WorkItem(SendOrPostCallback callback, object state, ManualResetEvent wait_handle)
        {
            callback_ = callback;
            state_ = state;
            wait_handle_ = wait_handle;
        }

        // this code must run on the SharedWWB thread
        public void Execute()
        {
            if (wait_handle_ == null)
                // Unhandled exceptions will terminate the SharedWWB thread
                callback_(state_);
            else
            {
                // synchronous call
                try
                {
                    // call the thread
                    callback_(state_);
                }
                catch (Exception ex)
                {
                    ex_ = ex;
                }
                finally
                {
                    wait_handle_.Set();
                }
            }
        }

        public void WaitForSendComplete()
        {
            wait_handle_.WaitOne();
            if (ex_ != null)
                throw ex_;
        }
    }
}
