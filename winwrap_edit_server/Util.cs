using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace winwrap_edit_server
{
    public static class Util
    {
        public static string ReadResourceTextFile(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = assembly.GetName().Name.ToString();
            using (Stream stream = assembly.GetManifestResourceStream($"{path}.TextFiles.{name}.txt"))
            using (StreamReader stream_reader = new StreamReader(stream))
                return stream_reader.ReadToEnd();
        }

        public static string ReadResourceTextFile(string name, params object[] values)
        {
            return Replace(ReadResourceTextFile(name), values);
        }

        public static string ReadResourceTextFile(string name, IDictionary<string, object> substitutions)
        {
            return Replace(ReadResourceTextFile(name),substitutions);
        }

        public static string Replace(string text, IDictionary<string, object> substitutions)
        {
            foreach (KeyValuePair<string, object> kvp in substitutions)
                text = text.Replace("{" + kvp.Key + "}", kvp.Value.ToString());

            return text;
        }

        public static string Replace(string text, params object[] values)
        {
            int index = 0;
            foreach (object value in values)
                text = text.Replace("{" + index++ + "}", value.ToString());

            return text;
        }
    }
}
