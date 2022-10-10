using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolsTest
{
    static class CmdHelper
    {
        public static string RunCommand(string workingDirectory, string arguments)
        {
            var output = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    Verb = "runas",
                    FileName = "cmd.exe",
                    Arguments = "/C " + arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    WorkingDirectory = workingDirectory
                };

                var proc = Process.Start(startInfo);

                output = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit(60000);

                return output;
            }
            catch (Exception)
            {
                return output;
            }
        }
    }
}
