namespace BrutalUi.Core;

/// <summary>
/// The two values that tell the game when the UI owns the pointer or the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// Read the value from <see cref="UiContext.RoutingGate"/> after <see cref="UiContext.Update(float)"/> and before the
/// game handles its own input. The value is a copy. A later change of the context does not change it.
/// <see langword="default"/> gives two <see langword="false"/> values.
/// </para>
/// <para>
/// <see cref="PointerOverUi"/> is <see langword="true"/> when the last pointer or scroll entry with no target hit an
/// element. <see cref="KeyboardFocusInUi"/> is <see langword="true"/> when the context has a focus owner
/// (<see cref="UiContext.FocusedElement"/>).
/// </para>
/// <para>
/// The gate knows nothing of ImGui (Decision 5). With an ImGui window next to the UI, combine the values. The game
/// uses <c>gate.PointerOverUi || io.WantCaptureMouse</c> for its own pointer input. Set
/// <c>GlfwUiInput.KeyboardEnabled</c> from <c>io.WantCaptureKeyboard</c>, so that the UI gets no key while ImGui
/// owns the keyboard.
/// </para>
/// <para>
/// The layout-root rule (P6-8 of pointer-routing): a root that covers the window makes <see cref="PointerOverUi"/>
/// <see langword="true"/> over the full window, because each point hits the root. Set
/// <see cref="Element.IsPickable"/> to <see langword="false"/> on such a root. The pointer then passes to the game
/// over the gaps of the root, and the children of the root still get the hit.
/// </para>
/// </remarks>
public readonly struct UiRoutingGate
{
    /// <summary>Creates a gate with two values.</summary>
    /// <param name="pointerOverUi">The value of <see cref="PointerOverUi"/>.</param>
    /// <param name="keyboardFocusInUi">The value of <see cref="KeyboardFocusInUi"/>.</param>
    internal UiRoutingGate(bool pointerOverUi, bool keyboardFocusInUi)
    {
        PointerOverUi = pointerOverUi;
        KeyboardFocusInUi = keyboardFocusInUi;
    }

    /// <summary>
    /// Gets a value that tells if the last pointer or scroll entry with no target hit an element. When the value is
    /// <see langword="true"/>, the game ignores the pointer for its own input.
    /// </summary>
    public bool PointerOverUi { get; }

    /// <summary>
    /// Gets a value that tells if an element holds the keyboard focus of the context. When the value is
    /// <see langword="true"/>, the game ignores its own keys.
    /// </summary>
    public bool KeyboardFocusInUi { get; }
}
