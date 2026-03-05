using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunCommand : Editor {
    
    public static void Run(string execName, string command) {
        Process process = new Process();
        process.StartInfo.FileName = execName;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.OutputDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data) && !string.IsNullOrWhiteSpace(args.Data)) {
                Debug.Log(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) => {
            if (!string.IsNullOrEmpty(args.Data) && !string.IsNullOrWhiteSpace(args.Data)) {
                Debug.LogWarning(args.Data);
            }
        };
        process.Start();
        process.StandardInput.WriteLine(command);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
    
    public static Process StartProcess(
        string command,
        string param,
        string workDir,
        DataReceivedEventHandler dataReceived,
        DataReceivedEventHandler errorReceived
    )
    {
        Process ps = new Process
        {
            StartInfo =
            {
                FileName = command,
                Arguments = param,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workDir,

            }
        };
        ps.OutputDataReceived += dataReceived;
        ps.ErrorDataReceived += errorReceived;
        ps.Start();
        ps.BeginOutputReadLine();
        ps.BeginErrorReadLine();

        return ps;
    }
}


