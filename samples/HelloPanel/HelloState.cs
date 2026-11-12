using System;
using Brutal.GlfwApi;
using BrutalUi.Core;
using BrutalUi.Framework;

namespace HelloPanel;

/// <summary>
/// The state row of the sample: one <see cref="Store{T}"/> that drives a label, three commands on the buttons of the
/// row, and the actions on the keys and the gamepad. The class also builds the focus commands and the action map of
/// the sample.
/// </summary>
/// <remarks>
/// <para>
/// The class is the A7 demo of the state-and-commands epic. The <c>Add</c> button runs the command
/// <c>hello.add</c> through <see cref="CommandBindings.BindCommand(Button, CommandRegistry, ReadOnlySpan{byte})"/>.
/// The command adds 1 to <see cref="Score"/>. The label of the row shows the score through
/// <see cref="LabelBindings.BindText{T, TValue}(Label, Store{T}, Func{T, TValue}, string?, IFormatProvider?)"/>. The
/// action <c>Cancel</c> runs the command <c>hello.reset</c> from the Escape key and from the gamepad button
/// <c>B</c>. The actions <c>NavigateUp</c> and <c>NavigateDown</c> run the focus commands from the Up key and the
/// Down key, and from <c>DpadUp</c> and <c>DpadDown</c>. The action <c>Move</c> is a Vector2 action with no
/// command. The W, A, S, and D keys and the left stick of gamepad slot 0 fill its value, and the frame loop
/// shows <see cref="UiActionMap.ReadVector"/> in the move label.
/// </para>
/// <para>
/// The rebind rule: the <c>Rebind (Up)</c> button runs the command <c>hello.rebind</c>. The handler opens a listen
/// on the action <c>NavigateUp</c> with the KeyboardMouse scheme id. The sample rebinds that key only. The map
/// closes the listen itself, so the frame loop needs no extra call: it reads <see cref="UiActionMap.IsRebinding"/>
/// and the last result, which the result slot stores here in <see cref="RebindStatus"/>,
/// <see cref="RebindCode"/>, and <see cref="RebindDone"/>. The constructor writes the slot one time with a target
/// delegate, so the frame path allocates nothing. The sample must never rebind <c>Cancel</c> to Escape: the listen
/// consumes the cancel key.
/// </para>
/// <para>
/// The restore rule: the <c>Restore</c> button runs the command <c>hello.restore</c>, and the handler calls
/// <see cref="UiActionMap.ResetBindings"/>. It brings the <c>Up</c> key back after a rebind. The map takes the
/// snapshot on its first <see cref="UiActionMap.Activate(UiContext)"/> call. The
/// <see cref="UiManager"/> does that call in the mode diff, so the state panel is mounted before the first rebind.
/// A restore that a command calls takes effect at the next frame. Escape still runs <c>hello.reset</c> in every
/// frame that is not a listen.
/// </para>
/// <para>
/// The build-time rule: the constructor does all the allocation. It creates the store, the registry, the map, the
/// row, and the bindings one time. The handlers are <see langword="static"/> fields and take the state as the
/// target, so a command call allocates nothing. The read slot of the active map reads the keys, the buttons, and
/// the pad of slot 0 in <see cref="UiContext.Update(float)"/>, and the context calls the map for a key that no
/// element took. <see cref="HelloModes"/> later registers
/// <c>hello.mode</c> and <c>hello.back</c> in the registry of <see cref="Map"/>, so one registry serves the map, the
/// buttons, and the panel context.
/// </para>
/// <para>
/// The label rule: the longest score text is <c>2147483647</c>, 10 bytes. The start buffer of a label holds 32 bytes,
/// and the buffer of the number binding starts at 32 bytes too. Thus the label never grows its buffer, and a score
/// change allocates nothing.
/// </para>
/// <para>
/// The group rule: each bind names its scheme. The constructor takes the two ids of <see cref="UiScheme"/> from
/// <see cref="UiActionMap.AddScheme(ReadOnlySpan{byte})"/>, and passes one as the last argument of each bind. The key
/// binds and the W, A, S, and D composite carry the KeyboardMouse id, and the pad button binds and the left stick carry
/// the Gamepad id. The map holds no lock, so it reads each binding whatever its tag, and the behavior of the sample does
/// not change. The <c>F1</c> and <c>F2</c> binds of <see cref="HelloModes"/> stay untagged, so the sample shows the
/// default scheme too: a binding with it belongs to each scheme.
/// </para>
/// </remarks>
internal sealed class HelloState
{
    /// <summary>The UTF-8 name of the Vector2 move action. The frame loop shows its value in the move label.</summary>
    public static ReadOnlySpan<byte> MoveAction => "Move"u8;

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

    /// <summary>
    /// The handler of <c>hello.rebind</c>. The target is the state. The handler opens the listen on the
    /// <c>NavigateUp</c> action in the KeyboardMouse scheme.
    /// </summary>
    private static readonly CommandHandler s_rebind = static target =>
    {
        HelloState state = (HelloState)target!;
        state.Map.BeginRebind(UiActions.NavigateUp, state.KeyboardScheme);
    };

    /// <summary>
    /// The handler of <c>hello.restore</c>. The target is the state. The handler restores the bindings of the build
    /// with <see cref="UiActionMap.ResetBindings"/> and closes an open listen. The sample calls it after the first
    /// activation, so the map holds the snapshot, and the handler reads no return value.
    /// </summary>
    private static readonly CommandHandler s_restore = static target =>
    {
        HelloState state = (HelloState)target!;
        state.Map.ResetBindings();
    };

    /// <summary>The selector of the score label. It returns the score itself.</summary>
    private static readonly Func<int, int> s_scoreText = static s => s;

    /// <summary>The id of the KeyboardMouse scheme. The key binds and the WASD composite carry it.</summary>
    private readonly int _keyboardScheme;

    /// <summary>The id of the Gamepad scheme. The pad button binds and the left stick carry it.</summary>
    private readonly int _gamepadScheme;

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
        commands.Register("hello.rebind"u8, s_rebind, this);
        commands.Register("hello.restore"u8, s_restore, this);
        FocusCommands.Register(commands, context);

        Map = new UiActionMap(new UiActionMap.CreateInfo { Commands = commands });
        _keyboardScheme = Map.AddScheme(UiScheme.KeyboardMouse);
        KeyboardScheme = _keyboardScheme;
        _gamepadScheme = Map.AddScheme(UiScheme.Gamepad);
        Map.AddAction(UiActions.Cancel, "hello.reset"u8);
        Map.AddAction(UiActions.NavigateUp, FocusCommands.Previous);
        Map.AddAction(UiActions.NavigateDown, FocusCommands.Next);
        Map.BindKey(UiActions.Cancel, (int)GlfwKey.Escape, UiModifiers.None, _keyboardScheme);
        Map.BindGamepadButton(UiActions.Cancel, UiGamepadButtons.B, _gamepadScheme);
        Map.BindKey(UiActions.NavigateUp, (int)GlfwKey.Up, UiModifiers.None, _keyboardScheme);
        Map.BindGamepadButton(UiActions.NavigateUp, UiGamepadButtons.DpadUp, _gamepadScheme);
        Map.BindKey(UiActions.NavigateDown, (int)GlfwKey.Down, UiModifiers.None, _keyboardScheme);
        Map.BindGamepadButton(UiActions.NavigateDown, UiGamepadButtons.DpadDown, _gamepadScheme);
        Map.AddAction(MoveAction, UiActionType.Vector2);
        Map.BindKeyVector(MoveAction, (int)GlfwKey.W, (int)GlfwKey.A, (int)GlfwKey.S, (int)GlfwKey.D, _keyboardScheme);
        Map.BindGamepadStick(MoveAction, UiGamepadStick.Left, _gamepadScheme);
        Map.OnRebindResult = OnRebind;

        AddButton = new Button(new Button.CreateInfo { Text = "Add" }).BindCommand(commands, "hello.add"u8);
        RebindButton = new Button(new Button.CreateInfo { Text = "Rebind (Up)" }).BindCommand(
            commands, "hello.rebind"u8);
        RestoreButton = new Button(new Button.CreateInfo { Text = "Restore" }).BindCommand(
            commands, "hello.restore"u8);
        Label scoreLabel = new Label(new Label.CreateInfo()) { IsPickable = false }.BindText(Score, s_scoreText);

        Row = new Element();
        Row.Classes.Add(RowClass);
        Row.Children.Add(AddButton);
        Row.Children.Add(RebindButton);
        Row.Children.Add(RestoreButton);
        Row.Children.Add(scoreLabel);
    }

    /// <summary>Gets the row: the <c>Add</c> button, the <c>Rebind (Up)</c> button, the <c>Restore</c> button, and the
    /// score label. <see cref="HelloScreen.BuildStatePanel"/> puts the row in the state panel.</summary>
    public Element Row { get; }

    /// <summary>Gets the <c>Add</c> button. A click runs <c>hello.add</c>.</summary>
    public Button AddButton { get; }

    /// <summary>Gets the <c>Rebind (Up)</c> button. A click runs <c>hello.rebind</c> and opens the listen.</summary>
    public Button RebindButton { get; }

    /// <summary>Gets the <c>Restore</c> button. A click runs <c>hello.restore</c> and brings the <c>Up</c> key
    /// back.</summary>
    public Button RestoreButton { get; }

    /// <summary>Gets the action map. The first apply of the mode that names the map activates it; the caller does not
    /// call <see cref="UiActionMap.Activate(UiContext)"/> (see <see cref="HelloModes"/>).</summary>
    public UiActionMap Map { get; }

    /// <summary>Gets the score store. The label of the row shows it.</summary>
    public Store<int> Score { get; }

    /// <summary>Gets the number of runs of <c>hello.add</c>.</summary>
    public int Adds { get; private set; }

    /// <summary>Gets the number of runs of <c>hello.reset</c>.</summary>
    public int Resets { get; private set; }

    /// <summary>Gets the id of the KeyboardMouse scheme. The <c>hello.rebind</c> handler opens the listen with it.</summary>
    public int KeyboardScheme { get; }

    /// <summary>
    /// Gets the status of the last rebind result: <see cref="UiActionMap.RebindBound"/>,
    /// <see cref="UiActionMap.RebindCancelled"/>, or <see cref="UiActionMap.RebindRejected"/>. It is
    /// <see cref="UiActionMap.RebindBound"/> with <see cref="RebindCode"/> 0 before the first result.
    /// </summary>
    public int RebindStatus { get; private set; }

    /// <summary>Gets the code of the last rebind result, or 0 before the first result.</summary>
    public int RebindCode { get; private set; }

    /// <summary>
    /// Gets a value that tells if the result slot has stored one result. It is <see langword="false"/> before the
    /// first result.
    /// </summary>
    /// <remarks>
    /// The rebind label takes its idle text from this flag, not from <see cref="RebindCode"/>. A code alone cannot
    /// tell an idle map from a mouse-button commit, because a mouse-button capture carries code 0 to 7.
    /// </remarks>
    public bool RebindDone { get; private set; }

    /// <summary>
    /// The handler of <see cref="UiActionMap.OnRebindResult"/>. The map calls it in the frame that closes or
    /// rejects the listen. The method stores the status, the code, and the done flag for the rebind label.
    /// </summary>
    /// <param name="map">The map that ran the listen. The method reads nothing from it.</param>
    /// <param name="action">The name bytes of the target action. The method reads nothing from them.</param>
    /// <param name="status">The status of the result.</param>
    /// <param name="code">The code of the result.</param>
    private void OnRebind(UiActionMap map, ReadOnlySpan<byte> action, int status, int code)
    {
        RebindStatus = status;
        RebindCode = code;
        RebindDone = true;
    }
}
