using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WpfShooter;

/// <summary>Command-line options. run.py passes all of them; see tools/wpf-reference/README.md.</summary>
public sealed class Options
{
    /// <summary>AvaWpf family name, used in the shot file names (<c>Aero2</c>).</summary>
    public string Family { get; set; } = "Aero2";

    /// <summary>Scheme id, used in the shot file names (<c>Default</c>, <c>Metallic</c>, <c>Brick</c>, <c>Dark</c>).</summary>
    public string Scheme { get; set; } = "Default";

    /// <summary>The WPF theme dictionary merged into the application resources.</summary>
    public string Dictionary { get; set; } = "/PresentationFramework.Aero2;component/themes/aero2.normalcolor.xaml";

    /// <summary>What <c>"background": "auto"</c> means: a SystemColors role, <c>resource:Key</c> or <c>#RRGGBB</c>.</summary>
    public string Surface { get; set; } = "Control";

    /// <summary>The accent color set explicitly (WinRT UISettings is not available under Wine).</summary>
    public Color Accent { get; set; } = Color.FromRgb(0x00, 0x78, 0xD7);

    public string ScenesDir { get; set; } = "scenes";

    public List<string> Scenes { get; } = [];

    public string OutDir { get; set; } = "out";

    /// <summary>The handshake directory shared with run.py, for hover and pressed states. Null: skip those states.</summary>
    public string? IpcDir { get; set; }

    /// <summary>Forces <c>SystemParameters.HighContrast</c> on (Wine does not report the registry flag).</summary>
    public bool HighContrast { get; set; }

    public int WindowX { get; set; } = 32;

    public int WindowY { get; set; } = 32;

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--family": o.Family = Next(); break;
                case "--scheme": o.Scheme = Next(); break;
                case "--dictionary": o.Dictionary = Next(); break;
                case "--surface": o.Surface = Next(); break;
                case "--accent": o.Accent = (Color)ColorConverter.ConvertFromString(Next()); break;
                case "--scenes": o.ScenesDir = Next(); break;
                case "--scene": o.Scenes.AddRange(Next().Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                case "--out": o.OutDir = Next(); break;
                case "--ipc": o.IpcDir = Next(); break;
                case "--high-contrast": o.HighContrast = true; break;
                case "--window-x": o.WindowX = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--window-y": o.WindowY = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                default: throw new ArgumentException($"unknown argument {args[i]}");
            }
        }

        return o;
    }
}

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        Options opts;
        try
        {
            opts = Options.Parse(args);
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            return 2;
        }

        Log.Open(Path.Combine(opts.OutDir, "_logs", $"{opts.Family}.{opts.Scheme}.log"));
        Log.Info($"family={opts.Family} scheme={opts.Scheme} dictionary={opts.Dictionary}");

        // The capture itself is RenderTargetBitmap (software). This keeps the on-screen window off Wine's D3D9 path too.
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        if (opts.HighContrast)
        {
            ForceHighContrast();
        }

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.DispatcherUnhandledException += (_, e) =>
        {
            Log.Error("unhandled: " + e.Exception);
            e.Handled = true;
            app.Shutdown(3);
        };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(opts.Dictionary, UriKind.Relative) });
        Accent.Apply(app.Resources, opts.Accent);
        app.Startup += async (_, _) =>
        {
            var rc = 0;
            try
            {
                rc = await new Shooter(opts).RunAsync();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                rc = 1;
            }

            app.Shutdown(rc);
        };
        var exit = app.Run();
        Log.Info($"exit {exit}");
        Log.Close();
        return exit;
    }

    /// <summary>
    /// Wine ignores the high contrast flag (SPI_GETHIGHCONTRAST always reports off), so prime WPF's cached
    /// <c>SystemParameters.HighContrast</c> before any theme reads it.
    /// </summary>
    private static void ForceHighContrast()
    {
        const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        var sp = typeof(SystemParameters);
        var slotType = sp.GetNestedType("CacheSlot", BindingFlags.NonPublic);
        var cache = sp.GetField("_cacheValid", Flags)?.GetValue(null) as BitArray;
        var value = sp.GetField("_highContrast", Flags);
        if (slotType == null || cache == null || value == null)
        {
            Log.Warn("cannot force SystemParameters.HighContrast (WPF internals changed)");
            return;
        }

        value.SetValue(null, true);
        cache[(int)Enum.Parse(slotType, "HighContrast")] = true;
        Log.Info("SystemParameters.HighContrast forced on");
    }
}

/// <summary>A tiny log: stderr plus <c>out/shooter.log</c> (Wine does not always forward stderr of a GUI exe).</summary>
public static class Log
{
    private static StreamWriter? _file;

    public static void Open(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _file = new StreamWriter(path, append: true) { AutoFlush = true };
    }

    public static void Close() => _file?.Dispose();

    public static void Info(string m) => Write("info", m);

    public static void Warn(string m) => Write("warn", m);

    public static void Error(string m) => Write("error", m);

    private static void Write(string level, string m)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {m}";
        Console.Error.WriteLine(line);
        _file?.WriteLine(line);
    }
}
