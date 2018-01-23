using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WWB
{
    public static class Util
    {
#if RESOURCE
        public static byte[] GzipDecode(byte[] bytes, int index, int count)
        {
            using (Stream stream = new MemoryStream(bytes, index, count))
            {
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
                {
                    const int size = 0x10000;
                    byte[] buffer = new byte[size];
                    using (MemoryStream memory = new MemoryStream())
                    {
                        while (true)
                        {
                            int read = gzip.Read(buffer, 0, size);
                            if (read == 0)
                                break;

                            memory.Write(buffer, 0, read);
                        }

                        return memory.ToArray();
                    }
                }
            }
        }
#endif
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

        public static byte[] ReadResourceBinaryFile(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string path = assembly.GetName().Name.ToString();
            using (Stream stream = assembly.GetManifestResourceStream($"{path}.ResFiles.{name}"))
            using (BinaryReader stream_reader = new BinaryReader(stream))
                return stream_reader.ReadBytes((int)stream.Length);
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

        static public Dictionary<string, object> GetParameters(string[] args, IDictionary<string, object> defaults)
        {
            // get the options from the command line
            Dictionary<string, object> parameters = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
            parameters[".appname"] = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

            foreach (string arg in args)
            {
                string[] parts = arg.Split(new char[] { '=' }, 2);
                string key = parts[0];
                if (!defaults.ContainsKey(key))
                {
                    Console.Write(Util.ReadResourceTextFile("Messages.BadOption", key));
                    parameters["help"] = true;
                    continue;
                }

                object value = defaults[key];
                Type value_type = value.GetType();
                if (parts.Length == 1)
                {
                    if (value_type != typeof(Boolean))
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionNoValue", key));
                        parameters["help"] = true;
                        continue;
                    }

                    value = true;
                }
                else
                {
                    if (value_type == typeof(Boolean))
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionValue", key));
                        parameters["help"] = true;
                        continue;
                    }

                    value = parts[1];
                    try
                    {
                        value = Convert.ChangeType(parts[1], value_type);
                    }
                    catch (Exception ex)
                    {
                        Console.Write(Util.ReadResourceTextFile("Messages.BadOptionValue2", key, ex.Message));
                        parameters["help"] = true;
                        continue;
                    }
                }

                parameters[key] = value;
            }

            // establish values from defaults for missing parameters
            foreach (string key in defaults.Keys)
                if (!parameters.ContainsKey(key))
                {
                    parameters[key] = defaults[key];
                    if (parameters[key].GetType() == typeof(String))
                        parameters[key] = Replace((string)parameters[key], parameters);
                }

            string flags = "";
            foreach (KeyValuePair<string, object> kvp in parameters)
                if (kvp.Key[0] != '.' && kvp.Value.GetType() == typeof(bool) && (bool)kvp.Value)
                    flags += "\r\n" + kvp.Key;

            parameters[".flags"] = flags;
            return parameters;
        }
    }
}
