// <copyright file="ServerlessMiniAgent.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Datadog.Trace.MiniAgent.Bootstrap;

// based on https://github.com/DataDog/dd-trace-dotnet/blob/4d13fffb6a13cbda7e2f998324c5e9113680fad4/tracer/src/Datadog.Trace/ClrProfiler/ServerlessInstrumentation/ServerlessMiniAgent.cs
public partial class ServerlessMiniAgent
{
    // [GeneratedRegex(@"\[(?<date>.+?) (?<level>.+?) (?<source>.+?)\] (?<log>.+)")]
    // private static partial Regex GetMiniAgentLogRegex();
    //
    // private static readonly Regex MiniAgentLogRegex = GetMiniAgentLogRegex();

    private readonly ILogger _logger;

    public ServerlessMiniAgent(ILogger logger)
    {
        _logger = logger;
    }

    private static string GetMiniAgentPath()
    {
        if (Environment.GetEnvironmentVariable("DD_MINI_AGENT_PATH") is { } envPath)
        {
            if (File.Exists(envPath))
            {
                return envPath;
            }

            throw new FileNotFoundException("The path specified in the DD_MINI_AGENT_PATH environment variable does not exist.", envPath);
        }

        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windowsPath = Path.Combine(basePath, "datadog-serverless-agent-windows-amd64\\datadog-serverless-trace-mini-agent.exe");

            if (File.Exists(windowsPath))
            {
                return windowsPath;
            }

            throw new FileNotFoundException("The Mini Agent executable for Windows was not found.", windowsPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var linuxPath = Path.Combine(basePath, "datadog-serverless-agent-linux-amd64/datadog-serverless-trace-mini-agent");

            if (File.Exists(linuxPath))
            {
                return linuxPath;
            }

            throw new FileNotFoundException("The Mini Agent executable for Linux was not found.", linuxPath);
        }

        throw new PlatformNotSupportedException("Unsupported OS platform.");
    }

    public Process Start()
    {
        var path = GetMiniAgentPath();
        _logger.LogInformation("[Mini-Agent] Starting Mini-Agent at {Path}", path);

        var process = new Process();
        process.StartInfo.FileName = path;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.OutputDataReceived += MiniAgentDataReceivedHandler;

        process.Start();
        process.BeginOutputReadLine();

        return process;
    }

    // Tries to clean Mini Agent logs and log to the correct level, otherwise just logs the data as-is to Info
    // Mini Agent logs will be prefixed with "[Datadog Serverless Mini Agent]"
    private void MiniAgentDataReceivedHandler(object sender, DataReceivedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Data))
        {
           _logger.LogInformation("[Mini-Agent] {Message}", args.Data);
           Console.WriteLine($"[Mini-Agent] {args.Data}");
        }

        // if (args.Data is { } data)
        // {
        //     if (ProcessMiniAgentLog(data, out var level, out var log))
        //     {
        //         switch (level)
        //         {
        //             case "ERROR":
        //                 Log.Error("Datadog Serverless Mini Agent", log, exception: null);
        //                 break;
        //             case "WARN":
        //                 Log.Warning("Datadog Serverless Mini Agent", log);
        //                 break;
        //             case "INFO":
        //                 Log.Information("Datadog Serverless Mini Agent", log);
        //                 break;
        //             case "DEBUG":
        //                 Log.Debug("Datadog Serverless Mini Agent", log);
        //                 break;
        //             default:
        //                 Log.Information("Datadog Serverless Mini Agent", log);
        //                 break;
        //         }
        //     }
        //     else
        //     {
        //         Console.WriteLine(data);
        //     }
        // }
    }

    // Processes a raw log from the mini agent, modifying two "out" parameters level and log.
    // For example, given this raw log:
    // [2023-06-06T01:31:30Z DEBUG datadog_trace_mini_agent::mini_agent] Random log
    // level and log will be the following values:
    // level == "DEBUG", log == "Random log"
    // private static bool ProcessMiniAgentLog(string rawLog, out ReadOnlySpan<char> level, out ReadOnlySpan<char> log)
    // {
    //     var match = MiniAgentLogRegex.Match(rawLog);
    //
    //     if (match.Success)
    //     {
    //         level = match.Groups["level"].ValueSpan;
    //         log = match.Groups["log"].ValueSpan;
    //         return true;
    //     }
    //
    //     level = default;
    //     log = default;
    //     return false;
    // }
}
