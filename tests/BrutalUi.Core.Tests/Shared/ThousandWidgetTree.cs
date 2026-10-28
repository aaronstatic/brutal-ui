using System;
using Brutal.Numerics;

namespace BrutalUi.Core.Tests;

/// <summary>
/// The 1000-element tree with each widget type: the tree of <see cref="ThousandElementTree"/> with 9 widget elements in
/// place of the last 3 footer slots.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Build"/> calls <see cref="ThousandElementTree.Build(IUiRenderer)"/> with a new
/// <see cref="RecordingRenderer"/>, so the context has a measurer. It removes the last 3 slots of the footer (3 x 3 = 9
/// elements) and adds 9 elements to the footer, in this order: the button <c>Play</c>, the disabled button <c>Off</c>, a
/// <see cref="Core.Toggle"/> of 20 x 20, a <see cref="Core.Slider"/> of 120 x 20 (0 to 100, step 1, value 50), a
/// <see cref="Core.TextField"/> of 120 x 16 (capacity 32, text <c>field</c>), and a <see cref="Core.ScrollView"/> of
/// 80 x 48 with 3 rows of 80 x 40. The rows are not pickable. The content of the view is 120 pixels tall, so the
/// maximum offset is 72 pixels. The tree keeps 1000 elements.
/// </para>
/// <para>
/// The footer has no test handle of <see cref="ThousandElementTree"/>, no rule of its sheet names the footer, and no
/// absolute overlay covers it. Thus the handles of the base tree stay valid, and each widget center hits its widget.
/// </para>
/// <para>
/// The build adds <see cref="WidgetSheet"/> (<see cref="WidgetRuleCount"/> rules) after the sheet of the base tree, runs
/// one <see cref="UiContext.Update(float)"/>, and then reads the viewport center of each widget. The rules set only a
/// fill or a border color, so a state change changes no rect. The type selectors use the real type names. Each element
/// name of the widgets starts with <see cref="NamePrefix"/>, because xUnit runs the test classes in parallel. The
/// build sets no delegate slot. The caller sets them.
/// </para>
/// <para>
/// The benchmark project and the ImGui test project link each file of this folder, so the file uses no test framework
/// type and no internal member of <c>BrutalUi.Core</c>.
/// </para>
/// </remarks>
internal sealed class ThousandWidgetTree
{
    /// <summary>The prefix of each element name that the build adds.</summary>
    public const string NamePrefix = "thousand-widget-";

    /// <summary>The number of footer slots that the build removes. Each slot holds 3 elements.</summary>
    public const int RemovedSlotCount = 3;

    /// <summary>The number of elements that the build adds to the footer.</summary>
    public const int WidgetElementCount = 9;

    /// <summary>The number of rules in <see cref="WidgetSheet"/>.</summary>
    public const int WidgetRuleCount = 7;

    /// <summary>The width and the height of the toggle, in pixels.</summary>
    public const float ToggleSize = 20f;

    /// <summary>The width of the slider, in pixels.</summary>
    public const float SliderWidth = 120f;

    /// <summary>The height of the slider, in pixels.</summary>
    public const float SliderHeight = 20f;

    /// <summary>The minimum of the slider.</summary>
    public const float SliderMinimum = 0f;

    /// <summary>The maximum of the slider.</summary>
    public const float SliderMaximum = 100f;

    /// <summary>The step of the slider.</summary>
    public const float SliderStep = 1f;

    /// <summary>The value of the slider after the build.</summary>
    public const float SliderStartValue = 50f;

    /// <summary>The width of the text field, in pixels.</summary>
    public const float FieldWidth = 120f;

    /// <summary>The height of the text field, in pixels.</summary>
    public const float FieldHeight = 16f;

    /// <summary>The capacity of the text field, in bytes.</summary>
    public const int FieldCapacity = 32;

    /// <summary>The text of the text field after the build.</summary>
    public const string FieldText = "field";

    /// <summary>The width of the scroll view and of each of its rows, in pixels.</summary>
    public const float ViewWidth = 80f;

    /// <summary>The height of the scroll view, in pixels.</summary>
    public const float ViewHeight = 48f;

    /// <summary>The number of rows in the scroll view.</summary>
    public const int ViewRowCount = 3;

    /// <summary>The height of each row of the scroll view, in pixels.</summary>
    public const float ViewRowHeight = 40f;

    /// <summary>The maximum vertical offset of the scroll view: 3 rows of 40 pixels in a view of 48 pixels.</summary>
    public const float ViewMaxOffset = (ViewRowCount * ViewRowHeight) - ViewHeight;

    /// <summary>The fill of a hovered button. The rule <c>Button:hover</c> sets it.</summary>
    public static readonly Rgba8 ButtonHoverFill = new(60, 72, 98);

    /// <summary>The fill of a pressed button. The rule <c>Button:active</c> sets it.</summary>
    public static readonly Rgba8 ButtonActiveFill = new(24, 32, 48);

    /// <summary>The fill of a disabled button. The rule <c>Button:disabled</c> sets it.</summary>
    public static readonly Rgba8 ButtonDisabledFill = new(26, 30, 38);

    /// <summary>The fill of a checked toggle. The rule <c>Toggle:checked</c> sets it.</summary>
    public static readonly Rgba8 ToggleCheckedFill = new(60, 140, 90);

    /// <summary>The border color of a focused slider. The rule <c>Slider:focus</c> sets it.</summary>
    public static readonly Rgba8 SliderFocusBorder = new(240, 192, 64);

    /// <summary>The fill of a hovered scroll view. The rule <c>ScrollView:hover</c> sets it.</summary>
    public static readonly Rgba8 ViewHoverFill = new(122, 148, 200);

    /// <summary>The border color of a focused text field. The rule <c>TextField:focus</c> sets it.</summary>
    public static readonly Rgba8 FieldFocusBorder = new(240, 200, 80);

    private static readonly Rgba8 ViewRowFill = new(64, 160, 96);

    /// <summary>Gets the context that holds the root. Its measurer is <see cref="Renderer"/>.</summary>
    public required UiContext Context { get; init; }

    /// <summary>Gets the renderer of the tree. It is the measurer of <see cref="Context"/> and the draw target.</summary>
    public required RecordingRenderer Renderer { get; init; }

    /// <summary>Gets the root of the tree.</summary>
    public required Element Root { get; init; }

    /// <summary>Gets the 1000 live elements of the tree, in tree order. The root is at index 0.</summary>
    public required Element[] All { get; init; }

    /// <summary>Gets the button <c>Play</c>.</summary>
    public required Button Button { get; init; }

    /// <summary>Gets the button <c>Off</c>. It holds <see cref="PseudoStates.Disabled"/>.</summary>
    public required Button DisabledButton { get; init; }

    /// <summary>Gets the toggle. It is not checked after the build.</summary>
    public required Toggle Toggle { get; init; }

    /// <summary>Gets the slider. Its value is <see cref="SliderStartValue"/> after the build.</summary>
    public required Slider Slider { get; init; }

    /// <summary>Gets the text field. Its text is <see cref="FieldText"/> after the build.</summary>
    public required TextField TextField { get; init; }

    /// <summary>Gets the scroll view with <see cref="ViewRowCount"/> rows.</summary>
    public required ScrollView ScrollView { get; init; }

    /// <summary>Gets the sheet of <see cref="WidgetRuleCount"/> widget state rules that the build adds.</summary>
    public required StyleSheet WidgetSheet { get; init; }

    /// <summary>Gets the viewport center of <see cref="Button"/>.</summary>
    public required float2 ButtonCenter { get; init; }

    /// <summary>Gets the viewport center of <see cref="DisabledButton"/>.</summary>
    public required float2 DisabledButtonCenter { get; init; }

    /// <summary>Gets the viewport center of <see cref="Toggle"/>.</summary>
    public required float2 ToggleCenter { get; init; }

    /// <summary>Gets the viewport center of <see cref="Slider"/>.</summary>
    public required float2 SliderCenter { get; init; }

    /// <summary>Gets the viewport center of <see cref="TextField"/>.</summary>
    public required float2 TextFieldCenter { get; init; }

    /// <summary>Gets the viewport center of <see cref="ScrollView"/>.</summary>
    public required float2 ScrollViewCenter { get; init; }

    /// <summary>
    /// Builds the tree, swaps the last 3 footer slots for the 9 widget elements, adds the widget sheet, and runs one
    /// update.
    /// </summary>
    /// <returns>The tree and the handles of the widgets.</returns>
    /// <exception cref="InvalidOperationException">
    /// The base tree has no footer with <see cref="RemovedSlotCount"/> slots or more, or the subtree of the root does not
    /// hold <see cref="ThousandElementTree.ElementCount"/> elements.
    /// </exception>
    public static ThousandWidgetTree Build()
    {
        RecordingRenderer renderer = new();
        ThousandElementTree tree = ThousandElementTree.Build(renderer);
        UiContext context = tree.Context;
        Element root = tree.Root;

        Element footer = FindFooter(root);
        if (footer.Children.Count < RemovedSlotCount)
        {
            throw new InvalidOperationException("The footer of the 1000-element tree must hold 3 slots or more.");
        }

        for (int i = 0; i < RemovedSlotCount; i++)
        {
            footer.Children.RemoveAt(footer.Children.Count - 1);
        }

        Button play = new(new Button.CreateInfo { Text = "Play" }) { Name = NamePrefix + "play" };
        footer.Children.Add(play);

        Button off = new(new Button.CreateInfo { Text = "Off" }) { Name = NamePrefix + "off" };
        off.States |= PseudoStates.Disabled;
        footer.Children.Add(off);

        Toggle toggle = new(default(Toggle.CreateInfo)) { Name = NamePrefix + "toggle" };
        toggle.Inline.Width = ToggleSize;
        toggle.Inline.Height = ToggleSize;
        footer.Children.Add(toggle);

        Slider slider = new(new Slider.CreateInfo
        {
            Minimum = SliderMinimum,
            Maximum = SliderMaximum,
            Step = SliderStep,
            Value = SliderStartValue,
        })
        {
            Name = NamePrefix + "slider",
        };
        slider.Inline.Width = SliderWidth;
        slider.Inline.Height = SliderHeight;
        footer.Children.Add(slider);

        TextField field = new(new TextField.CreateInfo { Capacity = FieldCapacity, Text = FieldText })
        {
            Name = NamePrefix + "field",
        };
        field.Inline.Width = FieldWidth;
        field.Inline.Height = FieldHeight;
        footer.Children.Add(field);

        ScrollView view = new(default(ScrollView.CreateInfo)) { Name = NamePrefix + "view" };
        view.Inline.FlexDirection = FlexDirection.Column;
        view.Inline.Width = ViewWidth;
        view.Inline.Height = ViewHeight;
        footer.Children.Add(view);

        for (int i = 0; i < ViewRowCount; i++)
        {
            Element row = new() { Name = NamePrefix + "row" };
            row.Inline.Width = ViewWidth;
            row.Inline.Height = ViewRowHeight;
            row.Inline.BackgroundColor = ViewRowFill;
            row.IsPickable = false;
            view.Children.Add(row);
        }

        if (CountSubtree(root) != ThousandElementTree.ElementCount)
        {
            throw new InvalidOperationException("The tree with the widgets must hold exactly 1000 elements.");
        }

        StyleSheet sheet = BuildWidgetSheet();
        context.AddSheet(sheet);
        context.Update(0.016f);

        Element[] all = new Element[ThousandElementTree.ElementCount];
        int count = 0;
        Collect(root, all, ref count);

        return new ThousandWidgetTree
        {
            Context = context,
            Renderer = renderer,
            Root = root,
            All = all,
            Button = play,
            DisabledButton = off,
            Toggle = toggle,
            Slider = slider,
            TextField = field,
            ScrollView = view,
            WidgetSheet = sheet,
            ButtonCenter = ViewportCenter(play),
            DisabledButtonCenter = ViewportCenter(off),
            ToggleCenter = ViewportCenter(toggle),
            SliderCenter = ViewportCenter(slider),
            TextFieldCenter = ViewportCenter(field),
            ScrollViewCenter = ViewportCenter(view),
        };
    }

    /// <summary>
    /// Gets the viewport rect of an element: the sum of the layout positions of the element and its ancestors, and the
    /// size of the element. The sum subtracts no scroll offset, so it is correct for an element outside each scroll view.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The rect as (x, y, width, height).</returns>
    public static float4 ViewportRect(Element element)
    {
        float4 own = element.Layout.Rect;
        float x = 0f;
        float y = 0f;
        for (Element? current = element; current is not null; current = current.Parent)
        {
            float4 r = current.Layout.Rect;
            x += r.X;
            y += r.Y;
        }

        return new float4(x, y, own.Z, own.W);
    }

    /// <summary>Gets the viewport center of an element outside each scroll view.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The center of <see cref="ViewportRect(Element)"/>.</returns>
    public static float2 ViewportCenter(Element element)
    {
        float4 r = ViewportRect(element);
        return new float2(r.X + (r.Z * 0.5f), r.Y + (r.W * 0.5f));
    }

    /// <summary>Counts the element and each element of its subtree.</summary>
    /// <param name="element">The top element.</param>
    /// <returns>The number of elements.</returns>
    public static int CountSubtree(Element element)
    {
        int count = 1;
        for (int i = 0; i < element.Children.Count; i++)
        {
            count += CountSubtree(element.Children[i]);
        }

        return count;
    }

    /// <summary>Builds the sheet of the 7 widget state rules. Each rule sets a fill or a border color only.</summary>
    /// <returns>The new sheet.</returns>
    private static StyleSheet BuildWidgetSheet()
    {
        ElementTypeId buttonType = ElementType.Intern("Button");
        ElementTypeId toggleType = ElementType.Intern("Toggle");
        ElementTypeId sliderType = ElementType.Intern("Slider");
        ElementTypeId viewType = ElementType.Intern("ScrollView");
        ElementTypeId fieldType = ElementType.Intern("TextField");

        return new StyleSheet.Builder()
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = ButtonHoverFill })
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType, States = PseudoStates.Active }),
                new StyleProperties { BackgroundColor = ButtonActiveFill })
            .Add(
                Selector.For(new CompoundSelector { Type = buttonType, States = PseudoStates.Disabled }),
                new StyleProperties { BackgroundColor = ButtonDisabledFill })
            .Add(
                Selector.For(new CompoundSelector { Type = toggleType, States = PseudoStates.Checked }),
                new StyleProperties { BackgroundColor = ToggleCheckedFill })
            .Add(
                Selector.For(new CompoundSelector { Type = sliderType, States = PseudoStates.Focus }),
                new StyleProperties { BorderColor = SliderFocusBorder })
            .Add(
                Selector.For(new CompoundSelector { Type = viewType, States = PseudoStates.Hover }),
                new StyleProperties { BackgroundColor = ViewHoverFill })
            .Add(
                Selector.For(new CompoundSelector { Type = fieldType, States = PseudoStates.Focus }),
                new StyleProperties { BorderColor = FieldFocusBorder })
            .Build();
    }

    /// <summary>Finds the root child with the name <c>footer</c>.</summary>
    /// <param name="root">The root of the base tree.</param>
    /// <returns>The footer.</returns>
    /// <exception cref="InvalidOperationException">The root has no child with the name <c>footer</c>.</exception>
    private static Element FindFooter(Element root)
    {
        for (int i = 0; i < root.Children.Count; i++)
        {
            Element child = root.Children[i];
            if (string.Equals(child.Name, "footer", StringComparison.Ordinal))
            {
                return child;
            }
        }

        throw new InvalidOperationException("The 1000-element tree must have a root child with the name footer.");
    }

    /// <summary>Writes the element and its subtree into the array, in tree order: a parent before its children.</summary>
    /// <param name="element">The top element.</param>
    /// <param name="all">The array of the elements.</param>
    /// <param name="count">The number of elements in the array. The call adds the elements that it writes.</param>
    private static void Collect(Element element, Element[] all, ref int count)
    {
        all[count] = element;
        count++;
        for (int i = 0; i < element.Children.Count; i++)
        {
            Collect(element.Children[i], all, ref count);
        }
    }
}
