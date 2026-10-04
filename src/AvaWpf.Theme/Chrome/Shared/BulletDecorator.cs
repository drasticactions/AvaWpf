using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaWpf.Chrome;

/// <summary>
/// WPF's <c>BulletDecorator</c>: the <see cref="Bullet"/> on the left, centered on the first text line of the
/// <see cref="Decorator.Child"/> to its right.
/// </summary>
public class BulletDecorator : Decorator
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<BulletDecorator>();

    /// <summary>Defines the <see cref="Bullet"/> property.</summary>
    public static readonly StyledProperty<Control?> BulletProperty =
        AvaloniaProperty.Register<BulletDecorator, Control?>(nameof(Bullet));

    static BulletDecorator()
    {
        AffectsRender<BulletDecorator>(BackgroundProperty);
        AffectsMeasure<BulletDecorator>(BulletProperty);
    }

    /// <summary>The brush drawn over the whole render size.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The bullet element.</summary>
    public Control? Bullet
    {
        get => GetValue(BulletProperty);
        set => SetValue(BulletProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BulletProperty)
        {
            if (change.OldValue is Control old)
            {
                LogicalChildren.Remove(old);
                VisualChildren.Remove(old);
            }

            if (change.NewValue is Control bullet)
            {
                LogicalChildren.Add(bullet);
                VisualChildren.Add(bullet);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (Background is { } background)
        {
            context.FillRectangle(background, new Rect(Bounds.Size));
        }
    }

    /// <summary>Measures the bullet, then the child in the remaining width; the height is the taller of the two.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var bulletSize = default(Size);
        if (Bullet is { } bullet)
        {
            bullet.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            bulletSize = bullet.DesiredSize;
        }

        var contentSize = default(Size);
        if (Child is { } child)
        {
            var width = double.IsPositiveInfinity(availableSize.Width) ? availableSize.Width : Math.Max(0, availableSize.Width - bulletSize.Width);
            child.Measure(new Size(width, availableSize.Height));
            contentSize = child.DesiredSize;
        }

        return new Size(bulletSize.Width + contentSize.Width, Math.Max(bulletSize.Height, contentSize.Height));
    }

    /// <summary>Arranges the child right of the bullet, then centers the bullet on the child's first line.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var bullet = Bullet;
        var child = Child;
        var bulletSize = default(Size);
        if (bullet is not null)
        {
            bulletSize = bullet.DesiredSize;
            bullet.Arrange(new Rect(bulletSize));
        }

        var bulletOffsetY = 0.0;
        if (child is not null)
        {
            var width = finalSize.Width;
            var height = finalSize.Height;
            if (bullet is not null)
            {
                width = Math.Max(child.DesiredSize.Width, finalSize.Width - bulletSize.Width);
                height = Math.Max(child.DesiredSize.Height, finalSize.Height);
            }

            child.Arrange(new Rect(bulletSize.Width, 0, width, height));
            var centerY = FirstLineHeight(child) * 0.5;
            bulletOffsetY = Math.Max(0, centerY - (bulletSize.Height * 0.5));
        }

        if (bullet is not null && bulletOffsetY > 0)
        {
            bullet.Arrange(new Rect(0, bulletOffsetY, bulletSize.Width, bulletSize.Height));
        }

        return finalSize;
    }

    /// <summary>The height of the first line of the first TextBlock in the child, else the child's height.</summary>
    private static double FirstLineHeight(Control element)
    {
        var text = element as TextBlock ?? element.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
        if (text is not null && text.IsMeasureValid && text.TextLayout.TextLines is { Count: > 0 } lines)
        {
            // Add twice the TextBlock's offset, so the line's center is measured from the top of the child.
            var offset = text.TranslatePoint(default, element)?.Y ?? 0;
            return lines[0].Height + (offset * 2.0);
        }

        return element.Bounds.Height;
    }
}
