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
        public static ICollection<string> GetResourceFileNames(string prefix)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = assembly.GetName().Name.ToString();
            prefix = path + ".ResFiles." + prefix;
            if (!prefix.EndsWith("."))
                prefix += ".";

            List<string> names = new List<string>();
            foreach (string name in assembly.GetManifestResourceNames())
                if (name.StartsWith(prefix))
                    names.Add(name.Substring(prefix.Length));

            return names;
        }

        public static string ReadResourceTextFile(string name, bool textext = true)
        {
            if (textext)
                name += ".txt";

            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = assembly.GetName().Name.ToString();
            using (Stream stream = assembly.GetManifestResourceStream($"{path}.ResFiles.{name}"))
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
