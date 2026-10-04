using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaWpf;

/// <summary>
/// Starts a window resize drag from a control inside the window, such as a resize grip.
/// </summary>
/// <remarks>
/// Avalonia.Native has no resize drag for an undecorated window, so on macOS the drag follows the mouse itself. It reads
/// the global mouse location, which does not move with the window, so a resize that moves the window does not feed back.
/// </remarks>
internal static class WindowResizeDrag
{
    /// <summary>Resizes <paramref name="window"/> from <paramref name="edge"/> while the pressed pointer is down.</summary>
    public static void Begin(Window window, WindowEdge edge, PointerPressedEventArgs e)
    {
        if (!OperatingSystem.IsMacOS() || window.TryGetPlatformHandle() is not { HandleDescriptor: "NSWindow" })
        {
            window.BeginResizeDrag(edge, e);
            return;
        }

        if (e.Source is not InputElement source)
        {
            return;
        }

        var start = NativeMacOS.MouseLocation();
        var startPosition = window.Position;
        var startSize = window.ClientSize;
        var scale = window.RenderScaling;
        e.Pointer.Capture(source);

        void OnMoved(object? sender, PointerEventArgs args)
        {
            var now = NativeMacOS.MouseLocation();

            // Cocoa screen coordinates grow upward.
            var dx = now.X - start.X;
            var dy = start.Y - now.Y;
            var minWidth = Math.Max(window.MinWidth, 100);
            var minHeight = Math.Max(window.MinHeight, 40);
            var width = startSize.Width;
            var height = startSize.Height;
            var position = startPosition;
            if (edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast)
            {
                width = Math.Max(minWidth, startSize.Width + dx);
            }
            else if (edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest)
            {
                width = Math.Max(minWidth, startSize.Width - dx);
                position = position.WithX(startPosition.X + (int)Math.Round((startSize.Width - width) * scale));
            }

            if (edge is WindowEdge.South or WindowEdge.SouthEast or WindowEdge.SouthWest)
            {
                height = Math.Max(minHeight, startSize.Height + dy);
            }
            else if (edge is WindowEdge.North or WindowEdge.NorthEast or WindowEdge.NorthWest)
            {
                height = Math.Max(minHeight, startSize.Height - dy);
                position = position.WithY(startPosition.Y + (int)Math.Round((startSize.Height - height) * scale));
            }

            window.Width = width;
            window.Height = height;
            if (position != startPosition)
            {
                window.Position = position;
            }

            args.Handled = true;
        }

        void OnReleased(object? sender, PointerReleasedEventArgs args)
        {
            args.Pointer.Capture(null);
            args.Handled = true;
        }

        void OnCaptureLost(object? sender, PointerCaptureLostEventArgs args)
        {
            source.PointerMoved -= OnMoved;
            source.PointerReleased -= OnReleased;
            source.PointerCaptureLost -= OnCaptureLost;
        }

        source.PointerMoved += OnMoved;
        source.PointerReleased += OnReleased;
        source.PointerCaptureLost += OnCaptureLost;
    }

    private static class NativeMacOS
    {
        private const string ObjCLibrary = "/usr/lib/libobjc.dylib";

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct CGPoint
        {
            public readonly double X;
            public readonly double Y;
        }

        [DllImport(ObjCLibrary)]
        private static extern IntPtr objc_getClass(string name);

        [DllImport(ObjCLibrary)]
        private static extern IntPtr sel_registerName(string selector);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        private static extern CGPoint SendCGPoint(IntPtr receiver, IntPtr selector);

        /// <summary>[NSEvent mouseLocation]: the mouse in screen points, origin at the bottom left.</summary>
        public static CGPoint MouseLocation() => SendCGPoint(objc_getClass("NSEvent"), sel_registerName("mouseLocation"));
    }
}
