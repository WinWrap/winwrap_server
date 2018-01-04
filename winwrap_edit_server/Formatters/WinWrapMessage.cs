using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace winwrap_edit_server.Formatters
{
    public class WinWrapMessage
    {
        private string jsontext_;

        public WinWrapMessage(string jsontext)
        {
            jsontext_ = jsontext ?? "[]";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() == typeof(WinWrapMessage) || obj.GetType() == typeof(WinWrapMessage))
                return obj.ToString() == jsontext_;

            return false;
        }

        public override int GetHashCode()
        {
            return jsontext_.GetHashCode();
        }

        public override string ToString()
        {
            return jsontext_;
        }
    }
}
