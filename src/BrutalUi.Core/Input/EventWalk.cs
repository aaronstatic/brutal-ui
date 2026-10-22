using System;

namespace BrutalUi.Core;

/// <summary>
/// The event walk. It sends one event along the path from the top of the tree to the target, in the capture phase,
/// the target phase, and the bubble phase.
/// </summary>
/// <remarks>
/// <para>
/// The walk is generic over the event struct. The JIT compiles one copy for each struct, so the calls through the
/// constraint (<c>T.HeaderOf</c> and <c>T.GetHandler</c>) box nothing.
/// </para>
/// <para>
/// The walk copies the parent chain of the target into a path buffer at the start (A3). A handler that changes the
/// tree or <see cref="UiEventHeader.Target"/> during the walk does not change the path.
/// </para>
/// <para>
/// The walk uses a loop over the path buffer and no recursion. No method of this class calls itself. The caller owns
/// the buffer. The walk grows it by doubling and never shrinks it (A7), so the walk allocates only during warm-up.
/// </para>
/// </remarks>
internal static class EventWalk
{
    /// <summary>The start length of the path buffer. A target deeper than this value grows the buffer one time.</summary>
    internal const int InitialPathSize = 32;

    /// <summary>
    /// Sends an event along the path of its target: the capture slots of the ancestors from the top down, the capture
    /// slot and then the bubble slot of the target, and the bubble slots of the ancestors from the parent up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before each handler call, the walk writes <see cref="UiEventHeader.Phase"/> and
    /// <see cref="UiEventHeader.CurrentTarget"/>. The walk reads <see cref="Element.Handlers"/> one time for each
    /// element of each phase, and skips an element with no side object. The walk reads a slot just before it calls
    /// the slot.
    /// </para>
    /// <para>
    /// The walk reads the event type one time at the start. A handler that changes <see cref="UiEventHeader.Type"/>
    /// does not change the slots of the walk.
    /// </para>
    /// <para>
    /// A handler can call <see cref="UiEventHeader.StopPropagation"/>. The walk then calls no later handler, also no
    /// later delegate of the same multicast slot.
    /// </para>
    /// <para>
    /// An event with no target does nothing. The drain skips such an event before the walk (A4).
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="e">The event. Each handler gets it by reference.</param>
    /// <param name="path">The path buffer. The walk replaces it with a larger array when it is too short.</param>
    internal static void Dispatch<T>(ref T e, ref Element[] path)
        where T : struct, IUiEvent<T>
    {
        ref UiEventHeader header = ref T.HeaderOf(ref e);
        Element? target = header.Target;
        if (target is null)
        {
            return;
        }

        UiEventType type = header.Type;

        // Step 1: count the parent chain of the target. Grow the path when it is too short (A7).
        int count = 0;
        for (Element? element = target; element is not null; element = element.Parent)
        {
            count++;
        }

        if (count > path.Length)
        {
            // The old buffer holds no reference that the walk needs, so the walk does not copy it.
            path = new Element[Math.Max(count, path.Length * 2)];
        }

        // Step 2: fill the path. path[0] is the top of the chain and path[count - 1] is the target (A3).
        {
            int index = count - 1;
            for (Element? element = target; element is not null; element = element.Parent)
            {
                path[index] = element;
                index--;
            }
        }

        // Steps 3 to 5 run in a separate method. The finally block clears the path after the walk and after an
        // exception, so the buffer holds no element reference (A3).
        try
        {
            Walk(ref e, ref header, path, count, type);
        }
        finally
        {
            Array.Clear(path, 0, count);
        }
    }

    /// <summary>Runs the capture phase, the target phase, and the bubble phase over a filled path.</summary>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="e">The event.</param>
    /// <param name="header">The header of <paramref name="e"/>.</param>
    /// <param name="path">The path buffer. <c>path[0]</c> is the top and <c>path[count - 1]</c> is the target.</param>
    /// <param name="count">The number of elements in the path.</param>
    /// <param name="type">The event type that the walk read at the start.</param>
    private static void Walk<T>(ref T e, ref UiEventHeader header, Element[] path, int count, UiEventType type)
        where T : struct, IUiEvent<T>
    {
        int last = count - 1;

        // Step 3: the capture phase. The ancestors from the top down, the capture slot of each.
        for (int i = 0; i < last; i++)
        {
            Element element = path[i];
            ElementHandlers? handlers = element.Handlers;
            if (handlers is null)
            {
                continue;
            }

            UiEventHandler<T>? handler = T.GetHandler(handlers, type, capture: true);
            if (handler is null)
            {
                continue;
            }

            header.Phase = UiEventPhase.Capture;
            header.CurrentTarget = element;
            if (InvokeSlot(handler, ref e, ref header))
            {
                return;
            }
        }

        // Step 4: the target phase. The capture slot of the target, then the bubble slot of the target.
        {
            Element element = path[last];
            ElementHandlers? handlers = element.Handlers;
            if (handlers is not null)
            {
                UiEventHandler<T>? handler = T.GetHandler(handlers, type, capture: true);
                if (handler is not null)
                {
                    header.Phase = UiEventPhase.Target;
                    header.CurrentTarget = element;
                    if (InvokeSlot(handler, ref e, ref header))
                    {
                        return;
                    }
                }

                // Read the bubble slot after the capture call, so a change of the slot in the capture handler counts.
                handler = T.GetHandler(handlers, type, capture: false);
                if (handler is not null)
                {
                    header.Phase = UiEventPhase.Target;
                    header.CurrentTarget = element;
                    if (InvokeSlot(handler, ref e, ref header))
                    {
                        return;
                    }
                }
            }
        }

        // Step 5: the bubble phase. The ancestors from the parent of the target up, the bubble slot of each.
        for (int i = last - 1; i >= 0; i--)
        {
            Element element = path[i];
            ElementHandlers? handlers = element.Handlers;
            if (handlers is null)
            {
                continue;
            }

            UiEventHandler<T>? handler = T.GetHandler(handlers, type, capture: false);
            if (handler is null)
            {
                continue;
            }

            header.Phase = UiEventPhase.Bubble;
            header.CurrentTarget = element;
            if (InvokeSlot(handler, ref e, ref header))
            {
                return;
            }
        }
    }

    /// <summary>
    /// Calls each delegate of a slot in order. Stops after the delegate that stops the event.
    /// </summary>
    /// <remarks>
    /// The method enumerates the slot with <see cref="Delegate.EnumerateInvocationList{TDelegate}"/>. The enumerator is
    /// a struct, so the method allocates nothing. A plain call of a multicast slot would run each delegate, also after
    /// a stop.
    /// </remarks>
    /// <typeparam name="T">The event struct.</typeparam>
    /// <param name="handler">The slot.</param>
    /// <param name="e">The event.</param>
    /// <param name="header">The header of <paramref name="e"/>.</param>
    /// <returns><see langword="true"/> when a delegate stopped the event.</returns>
    private static bool InvokeSlot<T>(UiEventHandler<T> handler, ref T e, ref UiEventHeader header)
        where T : struct, IUiEvent<T>
    {
        foreach (UiEventHandler<T> one in Delegate.EnumerateInvocationList(handler))
        {
            one(ref e);
            if (header.IsPropagationStopped)
            {
                return true;
            }
        }

        return false;
    }
}
