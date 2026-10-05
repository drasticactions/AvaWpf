using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// Releases discarded templates after a family switch re-templates a live control; Avalonia otherwise keeps them (and
/// the old family's ControlThemes) reachable from the control.
/// <list type="bullet">
/// <item>Removes the templated-parent ControlTheme frames of nested template parts, which Avalonia removes only from the
/// template root.</item>
/// <item>Clears the templated parent of closed popup content in the template, which the visual teardown misses. A
/// control that is not measured during the re-attach (on a hidden tab) is re-templated later, so its parts are
/// released when it applies its new template.</item>
/// <item>Empties a discarded <see cref="ContentPresenter"/> as soon as its owner applies the new template, because it
/// keeps its content as its visual child (TabControl's selected content), and removes the data-template child it left
/// in its owner's logical children.</item>
/// <item>Removes the containers of a replaced <see cref="VirtualizingPanel"/> from the <see cref="ItemsControl"/>'s
/// logical children.</item>
/// <item>Disposes the bindings that controls set on template parts in code (NumericUpDown, DatePicker, TimePicker,
/// ScrollBar).</item>
/// </list>
/// A step that needs a non-public Avalonia member is a no-op if that member is missing.
/// </summary>
internal static class TemplateFrameCleanup
{
    private static readonly Action<StyledElement>? s_clearFrames = FindMethod<Action<StyledElement>>(typeof(StyledElement), "OnTemplatedParentControlThemeChanged");
    private static readonly Action<StyledElement, AvaloniaObject?>? s_setTemplatedParent = FindTemplatedParentSetter();
    private static readonly Action<ItemsControl, Control>? s_removeLogicalChild = FindMethod<Action<ItemsControl, Control>>(typeof(ItemsControl), "RemoveLogicalChild", typeof(Control));
    private static readonly Action<ScrollBar>? s_reattachScrollBar = FindMethod<Action<ScrollBar>>(typeof(ScrollBar), "AttachToScrollViewer");
    private static readonly Func<StyledElement, IAvaloniaList<ILogical>>? s_logicalChildren = FindLogicalChildren();
    private static readonly ConditionalWeakTable<TemplatedControl, HashSet<StyledElement>> s_pending = new();
    private static readonly ConditionalWeakTable<TemplatedControl, List<ContentPresenter>> s_filled = new();

    /// <summary>Captures the template parts under <paramref name="root"/> and the containers of its virtualizing panels.</summary>
    public static Snapshot Capture(Visual root)
    {
        var snapshot = new Snapshot();

        // Closed popup content is only in the logical tree and a template root only in the visual tree, so walk both.
        var seen = new HashSet<Visual>(ReferenceEqualityComparer.Instance);
        var stack = new Stack<Visual>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var v = stack.Pop();
            if (v != root)
            {
                Add(v);
            }

            foreach (var child in v.GetVisualChildren())
            {
                if (seen.Add(child))
                {
                    stack.Push(child);
                }
            }

            foreach (var child in ((ILogical)v).LogicalChildren)
            {
                if (child is Visual lv && seen.Add(lv))
                {
                    stack.Push(lv);
                }
            }
        }

        return snapshot;

        void Add(Visual node)
        {
            if (node is SelectingItemsControl selector)
            {
                snapshot.Selections.Add((selector, selector.SelectedIndex));
            }

            if (node is StyledElement { TemplatedParent: { } owner } e)
            {
                snapshot.Parts.Add((e, owner));
                if (e is ContentPresenter { Child: { } presented } presenter)
                {
                    if (owner is TemplatedControl control)
                    {
                        snapshot.Filled.Add((presenter, control));
                    }

                    if (presented.Parent == owner)
                    {
                        snapshot.Presented.Add((e, owner, presented));
                    }
                }

                if (e is ItemsPresenter { Panel: VirtualizingPanel panel } && owner is ItemsControl items)
                {
                    foreach (var container in panel.Children)
                    {
                        snapshot.Containers.Add((items, container));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Empties each captured content presenter when its owner applies its next template. A presenter keeps its content
    /// as its visual child, and the new template's presenter adds the same content during the first layout pass, so it
    /// must let go before that pass.
    /// </summary>
    public static void EmptyOnNextTemplate(Snapshot snapshot)
    {
        foreach (var (presenter, owner) in snapshot.Filled)
        {
            if (!s_filled.TryGetValue(owner, out var presenters))
            {
                presenters = new List<ContentPresenter>();
                s_filled.Add(owner, presenters);
                owner.TemplateApplied += OnFilledTemplateApplied;
            }

            presenters.Add(presenter);
        }
    }

    private static void OnFilledTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is not TemplatedControl control || !s_filled.TryGetValue(control, out var presenters))
        {
            return;
        }

        control.TemplateApplied -= OnFilledTemplateApplied;
        s_filled.Remove(control);
        foreach (var presenter in presenters)
        {
            if (presenter.Child is not null && !IsInTemplate(presenter, control))
            {
                presenter.Content = null;
            }
        }
    }

    /// <summary>
    /// Selects again what each captured selector had selected: an item that is its own container (a TabItem) loses its
    /// selection when the old panel lets it go, and the new panel then selects the first item.
    /// </summary>
    public static void RestoreSelection(Snapshot snapshot)
    {
        foreach (var (selector, index) in snapshot.Selections)
        {
            if (selector.SelectedIndex != index && index < selector.ItemCount)
            {
                selector.SelectedIndex = index;
            }
        }
    }

    /// <summary>Releases every captured part and container that is no longer in use.</summary>
    public static void ClearDiscarded(Snapshot snapshot)
    {
        foreach (var (part, owner) in snapshot.Parts)
        {
            if (!IsInTemplate(part, owner))
            {
                Release(part);
            }
            else if (owner is TemplatedControl control)
            {
                ReleaseOnNextTemplate(control, part);
            }
        }

        if (s_logicalChildren is { } logicalChildren)
        {
            foreach (var (presenter, owner, presented) in snapshot.Presented)
            {
                // Content that is a control of the owner itself is presented again by the new presenter: keep it.
                if (owner is StyledElement host && presented.Parent == host && !presented.IsAttachedToVisualTree() &&
                    presented.GetVisualParent() is null or ContentPresenter { TemplatedParent: null } && !IsInTemplate(presenter, owner))
                {
                    logicalChildren(host).Remove(presented);
                }
            }
        }

        if (s_removeLogicalChild is { } remove)
        {
            foreach (var (owner, container) in snapshot.Containers)
            {
                // A container that is an item itself (ItemIsOwnContainer) is realized again by the new panel: keep it.
                if (container.GetVisualParent() is null && container.Parent == owner && !container.IsAttachedToVisualTree())
                {
                    remove(owner, container);
                }
            }
        }
    }

    private static void Release(StyledElement part)
    {
        try
        {
            if (part.TemplatedParent is not null)
            {
                // An ItemsPresenter also lets go of its panel's containers here, so an item that is its own
                // container (a gallery in a menu button) can be realized by the new template.
                s_setTemplatedParent?.Invoke(part, null);
            }

            s_clearFrames?.Invoke(part);
            ClearCodeBindings(part);
        }
        catch (Exception)
        {
            // A part that cannot be released is only kept alive; it never affects the live tree.
        }
    }

    private static void ReleaseOnNextTemplate(TemplatedControl control, StyledElement part)
    {
        if (!s_pending.TryGetValue(control, out var parts))
        {
            parts = new HashSet<StyledElement>(ReferenceEqualityComparer.Instance);
            s_pending.Add(control, parts);
            control.TemplateApplied += OnPendingTemplateApplied;
        }

        parts.Add(part);
    }

    private static void OnPendingTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        if (sender is not TemplatedControl control || !s_pending.TryGetValue(control, out var parts))
        {
            return;
        }

        control.TemplateApplied -= OnPendingTemplateApplied;
        s_pending.Remove(control);
        foreach (var part in parts)
        {
            if (!IsInTemplate(part, control))
            {
                Release(part);
            }
        }
    }

    /// <summary>Whether a part still has <paramref name="owner"/> as templated parent and as a logical or visual ancestor.</summary>
    private static bool IsInTemplate(StyledElement part, AvaloniaObject owner)
    {
        if (part.TemplatedParent != owner)
        {
            return false;
        }

        for (var e = part.Parent; e is not null; e = e.Parent)
        {
            if (e == owner)
            {
                return true;
            }
        }

        for (var v = (part as Visual)?.GetVisualParent(); v is not null; v = v.GetVisualParent())
        {
            if (v == owner)
            {
                return true;
            }
        }

        return false;
    }

    private static void ClearCodeBindings(StyledElement part)
    {
        switch (part)
        {
            case TextBox textBox:
                Unbind(textBox, InputElement.TabIndexProperty);
                break;
            case ScrollBar scrollBar:
                s_reattachScrollBar?.Invoke(scrollBar);
                break;
            case DatePickerPresenter date:
                Unbind(date, DatePickerPresenter.MaxYearProperty);
                Unbind(date, DatePickerPresenter.MinYearProperty);
                Unbind(date, DatePickerPresenter.MonthVisibleProperty);
                Unbind(date, DatePickerPresenter.MonthFormatProperty);
                Unbind(date, DatePickerPresenter.DayVisibleProperty);
                Unbind(date, DatePickerPresenter.DayFormatProperty);
                Unbind(date, DatePickerPresenter.YearVisibleProperty);
                Unbind(date, DatePickerPresenter.YearFormatProperty);
                break;
            case TimePickerPresenter time:
                Unbind(time, TimePickerPresenter.MinuteIncrementProperty);
                Unbind(time, TimePickerPresenter.SecondIncrementProperty);
                Unbind(time, TimePickerPresenter.ClockIdentifierProperty);
                Unbind(time, TimePickerPresenter.UseSecondsProperty);
                break;
        }
    }

    private static void Unbind(AvaloniaObject target, AvaloniaProperty property) =>
        BindingOperations.GetBindingExpressionBase(target, property)?.Dispose();

    [DynamicDependency("OnTemplatedParentControlThemeChanged", typeof(StyledElement))]
    [DynamicDependency("RemoveLogicalChild", typeof(ItemsControl))]
    [DynamicDependency("AttachToScrollViewer", typeof(ScrollBar))]
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The methods are kept by the DynamicDependency attributes above.")]
    private static T? FindMethod<T>(Type type, string name, params Type[] parameters)
        where T : Delegate
    {
        try
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, parameters, null)?.CreateDelegate<T>();
        }
        catch (Exception e) when (e is AmbiguousMatchException or ArgumentException)
        {
            return null;
        }
    }

    [DynamicDependency("LogicalChildren", typeof(StyledElement))]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The property is kept by the DynamicDependency above.")]
    private static Func<StyledElement, IAvaloniaList<ILogical>>? FindLogicalChildren()
    {
        try
        {
            return typeof(StyledElement).GetProperty("LogicalChildren", BindingFlags.Instance | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true)?
                .CreateDelegate<Func<StyledElement, IAvaloniaList<ILogical>>>();
        }
        catch (Exception e) when (e is AmbiguousMatchException or ArgumentException)
        {
            return null;
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(StyledElement))]
    private static Action<StyledElement, AvaloniaObject?>? FindTemplatedParentSetter()
    {
        try
        {
            return typeof(StyledElement).GetProperty(nameof(StyledElement.TemplatedParent))?.GetSetMethod(nonPublic: true)?
                .CreateDelegate<Action<StyledElement, AvaloniaObject?>>();
        }
        catch (Exception e) when (e is AmbiguousMatchException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>What <see cref="Capture"/> found before a re-attach.</summary>
    internal sealed class Snapshot
    {
        /// <summary>The template parts, with their templated parent at capture time.</summary>
        public List<(StyledElement Part, AvaloniaObject Owner)> Parts { get; } = new();

        /// <summary>The children content presenters added to the logical children of their templated parent.</summary>
        public List<(StyledElement Presenter, AvaloniaObject Owner, Control Child)> Presented { get; } = new();

        /// <summary>The containers of the virtualizing items panels, with their items control.</summary>
        public List<(ItemsControl Owner, Control Container)> Containers { get; } = new();

        /// <summary>The content presenters that show content, with their templated parent.</summary>
        public List<(ContentPresenter Presenter, TemplatedControl Owner)> Filled { get; } = new();

        /// <summary>The selectors with their selected index at capture time.</summary>
        public List<(SelectingItemsControl Selector, int Index)> Selections { get; } = new();
    }
}
