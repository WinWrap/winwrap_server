﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using WinWrap.Basic;
using WinWrap.Basic.Classic;

namespace WWB
{
    public class MyFileSystem : IVirtualFileSystem
    {
        private string root_;
        private static object lock_ = new object();

        public MyFileSystem(string root)
        {
            if (root.EndsWith("\\"))
                root = root.Substring(0, root.Length - 1);

            root_ = root;
        }

        public string Combine(string baseScriptPath, string name)
        {
            if (string.IsNullOrEmpty(baseScriptPath) && string.IsNullOrEmpty(name))
                return root_;

            if (name.StartsWith("\\"))
            {
                // absolute path, ignore baseScriptPath
                name = root_ + name;
            }
            else if (string.IsNullOrEmpty(baseScriptPath))
            {
                // relative to the root
                name = root_ + "\\" + name;
            }
            else if (baseScriptPath.StartsWith("\\"))
            {
                // relative path, combine with baseScriptPath
                string baseScriptDir = Path.GetDirectoryName(root_ + baseScriptPath);
                name = Path.Combine(baseScriptDir, name);
            }
            else
            {
                // shouldn't happen
                name = root_ + "\\" + name;
            }

            name = Path.GetFullPath(name);
            if (!name.StartsWith(root_ + "\\"))
                throw new Exception($"Invalid file path '{name}'");

            // remove root_
            return name.Substring(root_.Length);
        }

        public void Delete(string scriptPath)
        {
            ValidatePath(scriptPath);
            lock (lock_)
            {
                throw new NotImplementedException();
            }
        }

        public bool Exists(string scriptPath)
        {
            lock (lock_)
            {
                return File.Exists(ValidatePath(scriptPath));
            }
        }

        public string GetCaption(string scriptPath)
        {
            return Path.GetFileName(ValidatePath(scriptPath));
        }

        public DateTime GetTimeStamp(string scriptPath)
        {
            lock (lock_)
            {
                return File.GetCreationTimeUtc(ValidatePath(scriptPath));
            }
        }

        public string Read(string scriptPath)
        {
            lock (lock_)
            {
                string actualPath = ValidatePath(scriptPath);
                if (!actualPath.EndsWith("\\"))
                    return File.ReadAllText(actualPath);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("%DIRECTORY%");
                foreach (string dir in Directory.GetDirectories(actualPath))
                    sb.AppendLine(Path.GetFileName(dir) + "\\");

                foreach (string file in Directory.GetFiles(actualPath))
                    sb.AppendLine(Path.GetFileName(file));

                return sb.ToString();
            }
        }

        public void Write(string scriptPath, string text)
        {
            lock (lock_)
            {
                File.WriteAllText(ValidatePath(scriptPath), text);
            }
        }

        private string ValidatePath(string scriptPath)
        {
            int x = scriptPath.IndexOf('>');
            if (x != -1)
                scriptPath = scriptPath.Substring(x + 1);

            if (!scriptPath.StartsWith("\\") || scriptPath.Contains(".."))
                throw new Exception($"Invalid file path '{scriptPath}'.");

            return root_ + scriptPath;
        }
    }
}
