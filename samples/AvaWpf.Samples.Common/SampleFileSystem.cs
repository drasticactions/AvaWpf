using System;

namespace AvaWpf.Samples;

/// <summary>A fake file system in the shape of a Windows XP machine, for the Explorer demo.</summary>
public static class SampleFileSystem
{
    /// <summary>Builds a fresh tree rooted at "Desktop".</summary>
    public static FileNode Create()
    {
        var desktop = new FileNode("Desktop", FileKind.Folder);
        var docs = desktop.Add(new FileNode("My Documents", FileKind.Folder));
        var letters = docs.Add(new FileNode("Letters", FileKind.Folder));
        letters.Add(new FileNode("To Grandma.txt", FileKind.Text, 2_311, new DateTime(2005, 12, 20, 18, 4, 0)));
        letters.Add(new FileNode("Thank you.txt", FileKind.Text, 1_022, new DateTime(2006, 1, 3, 9, 12, 0)));
        var reports = docs.Add(new FileNode("Reports", FileKind.Folder));
        for (var q = 1; q <= 4; q++)
        {
            reports.Add(new FileNode($"Quarter {q} report.txt", FileKind.Text, 18_400 + (q * 1_337), new DateTime(2006, q * 3, 28, 16, 0, 0)));
        }

        var pictures = docs.Add(new FileNode("My Pictures", FileKind.Folder));
        foreach (var name in new[] { "Beach.bmp", "Mountains.bmp", "Garden.bmp", "Sunset.bmp", "Winter.bmp" })
        {
            pictures.Add(new FileNode(name, FileKind.Image, 786_486, new DateTime(2004, 8, 4, 12, 0, 0)));
        }

        docs.Add(new FileNode("Budget.xls", FileKind.Other, 27_136, new DateTime(2006, 5, 2, 11, 45, 0)));
        docs.Add(new FileNode("Notes.txt", FileKind.Text, 640, new DateTime(2006, 8, 1, 8, 15, 0)));

        var computer = desktop.Add(new FileNode("My Computer", FileKind.Computer));
        var c = computer.Add(new FileNode("Local Disk (C:)", FileKind.Drive));
        var windows = c.Add(new FileNode("WINDOWS", FileKind.Folder));
        windows.Add(new FileNode("system32", FileKind.Folder));
        windows.Add(new FileNode("Fonts", FileKind.Folder));
        windows.Add(new FileNode("win.ini", FileKind.Other, 1_024));
        var programs = c.Add(new FileNode("Program Files", FileKind.Folder));
        programs.Add(new FileNode("Internet Explorer", FileKind.Folder));
        programs.Add(new FileNode("Windows Media Player", FileKind.Folder));
        c.Add(new FileNode("Documents and Settings", FileKind.Folder));
        c.Add(new FileNode("boot.ini", FileKind.Other, 211));
        computer.Add(new FileNode("CD Drive (D:)", FileKind.Drive));

        desktop.Add(new FileNode("My Network Places", FileKind.Network));
        desktop.Add(new FileNode("Recycle Bin", FileKind.RecycleBin));
        return desktop;
    }
}
