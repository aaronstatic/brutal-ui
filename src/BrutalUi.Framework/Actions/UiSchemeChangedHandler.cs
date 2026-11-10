using System;
using BrutalUi.Core;

namespace BrutalUi.Framework;

/// <summary>
/// A handler that the map calls when the active scheme changes. <see cref="UiActionMap"/> calls the slot
/// <see cref="UiActionMap.OnSchemeChanged"/> at the end of the frame read, inside
/// <see cref="UiContext.Update(float)"/>, with the map and the new scheme id.
/// </summary>
/// <remarks>
/// <para>
/// The slot rule: a game writes the slot with <c>=</c> on <see cref="UiActionMap.OnSchemeChanged"/>. The map holds one
/// handler, not a list, so a write with <c>+=</c> is a mistake.
/// </para>
/// <para>
/// The place rule: the map calls the handler in <see cref="UiContext.Update(float)"/>, at the end of the frame read of
/// <see cref="UiActionMap"/>, after the values of each action finish and after the pass that invokes the gamepad
/// commands. The handler therefore sees the finished values of the frame and the new
/// <see cref="UiActionMap.ActiveScheme"/>.
/// </para>
/// <para>
/// The change rule: the map calls the handler only when the scheme that it reports changed. One call comes per
/// <see cref="UiContext.Update(float)"/> at most, so two flips inside one read give one call with the last value.
/// </para>
/// <para>
/// The re-entry rule: the handler is code inside the frame read. The read copies its arrays and its counts to locals
/// first, so a bind or a <c>SetScheme</c> call in the handler takes effect at the next frame. A call of
/// <see cref="UiContext.Update(float)"/> from the handler throws <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The exception rule: an exception of the handler leaves <see cref="UiContext.Update(float)"/> before the binding pass.
/// The map keeps the values of the frame, because the read finished them before the call.
/// </para>
/// <para>
/// The build-time rule: create the delegate one time and set the slot at build time, not in the frame loop. A new
/// delegate in each frame allocates.
/// </para>
/// </remarks>
/// <param name="map">The map that reports the change.</param>
/// <param name="scheme">
/// The id of the new scheme. The map reads the name back with <see cref="UiActionMap.SchemeName(int)"/>.
/// </param>
public delegate void UiSchemeChangedHandler(UiActionMap map, int scheme);
