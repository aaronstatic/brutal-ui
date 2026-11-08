using System;

namespace BrutalUi.Core;

/// <summary>
/// A handler that runs at the start of an update, before the input pass. <see cref="UiContext.Update(float)"/> calls
/// the slot <see cref="UiContext.OnBeforeUpdate"/> first, before the input pass, with the <c>deltaTime</c> of the call.
/// </summary>
/// <remarks>
/// <para>
/// Q1 and A2 of panels-and-modes: the framework layer changes the tree at a mode switch. The manager lives in a
/// higher layer, and Core names no type of that layer. Thus the seam is one delegate slot on the context:
/// <see cref="UiContext.OnBeforeUpdate"/>.
/// </para>
/// <para>
/// The tree-change rule: the handler can add and remove roots and change the tree. The input pass, the binding pass,
/// the style pass, and the layout pass of the same <see cref="UiContext.Update(float)"/> call see the change. The hit
/// test reads the layout of the last update, so a root that the handler adds gets no hit until its first layout.
/// </para>
/// <para>
/// The re-entry rule: a call of <see cref="UiContext.Update(float)"/> from the handler throws
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The exception rule: an exception of the handler leaves <see cref="UiContext.Update(float)"/> before the input
/// pass. The ring keeps its entries, and the next <see cref="UiContext.Update(float)"/> calls the handler again.
/// </para>
/// <para>
/// The build-time rule: create the delegate one time and set the slot at build time, not in the frame loop. A new
/// delegate in each frame allocates.
/// </para>
/// </remarks>
/// <param name="deltaTime">The time since the last update, in seconds. It is the value of the call.</param>
public delegate void UiBeforeUpdateHandler(float deltaTime);
