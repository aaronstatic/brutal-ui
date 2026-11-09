using System;

namespace BrutalUi.Core;

/// <summary>
/// A handler that reads the input state of the frame. <see cref="UiContext.Update(float)"/> calls the slot
/// <see cref="UiContext.OnInputRead"/> after the drain of the input pass, with the context as its one parameter.
/// </summary>
/// <remarks>
/// <para>
/// Q1 of input-actions: the seam of the typed action map. The map lives in a higher layer, and Core names no type
/// of that layer. Thus the seam is one delegate slot on the context: <see cref="UiContext.OnInputRead"/>.
/// </para>
/// <para>
/// The frame rule: the handler reads <see cref="UiContext.GameInput"/> and <see cref="UiContext.Input"/> with the
/// values of this frame, after the gate. The drain has applied each entry of the ring, and the gate has emptied the
/// game view over the UI.
/// </para>
/// <para>
/// The order rule: the call runs after the before-update slot of <see cref="UiContext.OnBeforeUpdate"/> and after
/// the drain, and before the binding pass. A store write in the handler reaches the binding pass of the same call.
/// </para>
/// <para>
/// The re-entry rule: a call of <see cref="UiContext.Update(float)"/> from the handler throws
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The exception rule: an exception of the handler leaves <see cref="UiContext.Update(float)"/> before the binding
/// pass. The state of the frame stays, and the next <see cref="UiContext.Update(float)"/> calls the handler again.
/// </para>
/// <para>
/// The build-time rule: create the delegate one time and set the slot at build time, not in the frame loop. A new
/// delegate in each frame allocates.
/// </para>
/// </remarks>
/// <param name="context">The context that calls the handler.</param>
public delegate void UiInputReadHandler(UiContext context);
