using System;
using Brutal.GlfwApi;
using BrutalUi.Core;
using BrutalUi.Framework;

namespace HelloPanel;

/// <summary>
/// The state row of the sample: one <see cref="Store{T}"/> that drives a label, one command on a button, and one
/// action on a key. The class also builds the focus commands and the action map of the sample.
/// </summary>
/// <remarks>
/// <para>
/// The class is the A7 demo of the state-and-commands epic. The <c>Add</c> button runs the command
/// <c>hello.add</c> through <see cref="CommandBindings.BindCommand(Button, CommandRegistry, ReadOnlySpan{byte})"/>.
/// The command adds 1 to <see cref="Score"/>. The label of the row shows the score through
/// <see cref="LabelBindings.BindText{T, TValue}(Label, Store{T}, Func{T, TValue}, string?, IFormatProvider?)"/>. The
/// action <c>Cancel</c> runs the command <c>hello.reset</c> from the Escape key and from the gamepad button
/// <c>B</c>. The actions <c>NavigateUp</c> and <c>NavigateDown</c> run the focus commands from the Up key and the
/// Down key, and from <c>DpadUp</c> and <c>DpadDown</c>.
/// </para>
/// <para>
/// The build-time rule: the constructor does all the allocation. It creates the store, the registry, the map, the
/// row, and the bindings one time. The handlers are <see langword="static"/> fields and take the state as the
/// target, so a command call allocates nothing. The frame loop only calls <see cref="UiActionMap.PushGamepad"/>, and
/// the context calls the map for a key that no element took. <see cref="HelloModes"/> later registers
/// <c>hello.mode</c> and <c>hello.back</c> in the registry of <see cref="Map"/>, so one registry serves the map, the
/// buttons, and the panel context.
/// </para>
/// <para>
/// The label rule: the longest score text is <c>2147483647</c>, 10 bytes. The start buffer of a label holds 32 bytes,
/// and the buffer of the number binding starts at 32 bytes too. Thus the label never grows its buffer, and a score
/// change allocates nothing.
/// </para>
/// </remarks>
internal sealed class HelloState
{
    /// <summary>The class of the row: the class of the <c>.hello-row</c> rules of the sheet.</summary>
    private static readonly ClassId RowClass = ClassName.Intern("hello-row");

    /// <summary>The handler of <c>hello.add</c>. The target is the state.</summary>
    private static readonly CommandHandler s_add = static target =>
    {
        HelloState state = (HelloState)target!;
        state.Score.Set(state.Score.Value + 1);
        state.Adds++;
    };

    /// <summary>The handler of <c>hello.reset</c>. The target is the state.</summary>
    private static readonly CommandHandler s_reset = static target =>
    {
        HelloState state = (HelloState)target!;
        state.Score.Set(0);
        state.Resets++;
    };

    /// <summary>The selector of the score label. It returns the score itself.</summary>
    private static readonly Func<int, int> s_scoreText = static s => s;

    /// <summary>
    /// Creates the state and builds the store, the commands, the focus commands, the map, and the row.
    /// </summary>
    /// <remarks>
    /// The constructor does not activate the map. The first apply of the mode that names the map activates it, so the
    /// caller does not call <see cref="UiActionMap.Activate(UiContext)"/> (see <see cref="HelloModes"/>).
    /// </remarks>
    /// <param name="context">The context that the focus commands move the focus of.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public HelloState(UiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Score = new Store<int>(new Store<int>.CreateInfo { Initial = 0 });

        CommandRegistry commands = new();
        commands.Register("hello.add"u8, s_add, this);
        commands.Register("hello.reset"u8, s_reset, this);
        FocusCommands.Register(commands, context);

        Map = new UiActionMap(new UiActionMap.CreateInfo { Commands = commands });
        Map.AddAction(UiActions.Cancel, "hello.reset"u8);
        Map.AddAction(UiActions.NavigateUp, FocusCommands.Previous);
        Map.AddAction(UiActions.NavigateDown, FocusCommands.Next);
        Map.BindKey(UiActions.Cancel, (int)GlfwKey.Escape);
        Map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B);
        Map.BindKey(UiActions.NavigateUp, (int)GlfwKey.Up);
        Map.BindGamepadButton(UiActions.NavigateUp, UiGamepadButtons.DpadUp);
        Map.BindKey(UiActions.NavigateDown, (int)GlfwKey.Down);
        Map.BindGamepadButton(UiActions.NavigateDown, UiGamepadButtons.DpadDown);

        AddButton = new Button(new Button.CreateInfo { Text = "Add" }).BindCommand(commands, "hello.add"u8);
        Label scoreLabel = new Label(new Label.CreateInfo()) { IsPickable = false }.BindText(Score, s_scoreText);

        Row = new Element();
        Row.Classes.Add(RowClass);
        Row.Children.Add(AddButton);
        Row.Children.Add(scoreLabel);
    }

    /// <summary>Gets the row: the <c>Add</c> button and the score label. <see cref="HelloScreen.BuildStatePanel"/> puts
    /// the row in the state panel.</summary>
    public Element Row { get; }

    /// <summary>Gets the <c>Add</c> button. A click runs <c>hello.add</c>.</summary>
    public Button AddButton { get; }

    /// <summary>Gets the action map. The first apply of the mode that names the map activates it; the caller does not
    /// call <see cref="UiActionMap.Activate(UiContext)"/> (see <see cref="HelloModes"/>).</summary>
    public UiActionMap Map { get; }

    /// <summary>Gets the score store. The label of the row shows it.</summary>
    public Store<int> Score { get; }

    /// <summary>Gets the number of runs of <c>hello.add</c>.</summary>
    public int Adds { get; private set; }

    /// <summary>Gets the number of runs of <c>hello.reset</c>.</summary>
    public int Resets { get; private set; }
}
