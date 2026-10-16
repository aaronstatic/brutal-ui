using System;

namespace BrutalUi.Core;

/// <summary>
/// The pseudo-states of an element. A style rule can match a state.
/// </summary>
/// <remarks>
/// The input layer sets the states. A change of the states marks the element for
/// <see cref="DirtyFlags.Style"/>, because the cascade can give a new result.
/// </remarks>
[Flags]
public enum PseudoStates
{
    /// <summary>No state is set.</summary>
    None = 0,

    /// <summary>The pointer is over the element.</summary>
    Hover = 1,

    /// <summary>The user presses the element.</summary>
    Active = 2,

    /// <summary>The element has the keyboard focus.</summary>
    Focus = 4,

    /// <summary>The element does not accept input.</summary>
    Disabled = 8,

    /// <summary>The element is in the checked state.</summary>
    Checked = 16,
}
