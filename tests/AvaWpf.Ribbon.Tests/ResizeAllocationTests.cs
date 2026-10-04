using System;
using System.Linq;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Allocations while a Ribbon window is resized.</summary>
public class ResizeAllocationTests
{
    /// <summary>
    /// The allocation budget for 20 widths, down through every group size and back up (40 resizes). Measured at about
    /// 1.35 MB, most of it Avalonia's own layout events and text layout.
    /// </summary>
    private const long BudgetBytes = 1_600_000;

    [AvaloniaFact]
    public void Resizing_Through_20_Widths_Stays_Within_The_Allocation_Budget()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.CollapseWidth = 0;
        var window = RibbonSamples.Show(ribbon, 1400);
        var home = (RibbonTab)ribbon.Items[0]!;

        // From 720 px (nearly every group at its largest step) to 150 px (all three collapsed) and back.
        var widths = Enumerable.Range(0, 20).Select(i => 720.0 - (i * 30)).ToArray();
        void Resize()
        {
            foreach (var width in widths.Concat(widths.Reverse()))
            {
                window.Width = width;
                RibbonSamples.Settle(window);
            }
        }

        // The first pass creates what is created once (default size steps, collapsed templates, text caches).
        Resize();
        var state = string.Join(" ", home.Groups.Select(g => g.SizeDefinitionIndex));
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        Resize();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(state, string.Join(" ", home.Groups.Select(g => g.SizeDefinitionIndex)));
        Assert.True(allocated < BudgetBytes, $"resizing allocated {allocated / 1024} KB (budget {BudgetBytes / 1024} KB)");
        window.Close();
    }
}
