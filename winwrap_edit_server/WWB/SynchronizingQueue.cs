using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace WWB
{
    public class SynchronizingQueue
    {
        public int Id { get; private set; }

        private DateTime touched_;
        private StringBuilder sb_ = new StringBuilder();

        public SynchronizingQueue(int id)
        {
            Id = id;
            Touch();
        }

        public bool Alive
        {
            get
            {
                // true if less than one minute old
                return DateTime.UtcNow - touched_ < new TimeSpan(0, 1, 0);
            }
        }

        public string DequeueAll()
        {
            Touch();
            string json = null;
            if (sb_.Length > 0)
            {
                json = sb_.ToString();
                sb_.Clear();
            }

            return json;
        }

        public void Enqueue(string json)
        {
            Touch();
            if (!string.IsNullOrEmpty(json))
            {
                if (sb_.Length == 0)
                    sb_.Append('[');
                else
                {
                    --sb_.Length; // remove trailing ]
                    sb_.Append(",\r\n");
                }

                if (json.StartsWith("["))
                    sb_.Append(json.Substring(1));
                else
                {
                    sb_.Append(json);
                    sb_.Append(']');
                }
            }
        }

        private void Touch()
        {
            touched_ = DateTime.UtcNow;
        }
    }

    class SynchronizingQueues : Dictionary<int, SynchronizingQueue>
    {
        public string Dequeue(int id)
        {
            string response = null;
            if (ContainsKey(id))
                response = this[id].DequeueAll();

            return response ?? "[]";
        }

        public void Enqueue(string json, int id)
        {
            if (id == -1) // notification
            {
                // get rid of queues that are no longer needed
                PurgeDeadQueues();

                // add to all the queues
                foreach (SynchronizingQueue sq in Values)
                    if (sq.Id > 0)
                        sq.Enqueue(json);
            }
            else // response
            {
                // get the appropriate queue
                SynchronizingQueue sq = null;
                if (!TryGetValue(id, out sq))
                {
                    sq = new SynchronizingQueue(id);
                    Add(id, sq);
                }

                // add to the queue
                sq.Enqueue(json);

                // get rid of queues that are no longer needed
                PurgeDeadQueues();
            }
        }

        private void PurgeDeadQueues()
        {
            // kill any synchronizing queues that haven't been used in over one minute
            List<int> kill_ids = new List<int>();
            foreach (SynchronizingQueue sq2 in Values)
                if (!sq2.Alive && sq2.Id != 0)
                    kill_ids.Add(sq2.Id);

            foreach (int kill_id in kill_ids)
                Remove(kill_id);
        }
    }
}
