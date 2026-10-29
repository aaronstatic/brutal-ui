using System;
using System.Collections.Generic;

namespace BrutalUi.Framework;

/// <summary>
/// The handler of an event on the <see cref="EventBus"/>.
/// </summary>
/// <remarks>
/// Pass a static method in a <see langword="static"/> <see langword="readonly"/> field, so the subscribe allocates no
/// delegate. The event is a read-only reference. It is valid during the call only. Copy the event to keep it.
/// </remarks>
/// <typeparam name="T">The type of the event.</typeparam>
/// <param name="e">The event.</param>
/// <param name="target">The object that the subscriber gave to <see cref="EventBus.Subscribe{T}"/>.</param>
public delegate void EventBusHandler<T>(in T e, object? target);

/// <summary>
/// A typed publish and subscribe bus. A publish calls each subscriber of the type of the event.
/// </summary>
/// <remarks>
/// <para>
/// The bus keeps one subscriber list for each event type. The type must match exactly: a publish of a derived type
/// does not reach a subscriber of the base type. Use a <see langword="readonly"/> <see langword="record"/>
/// <see langword="struct"/> for an event, so the publish copies no object and the caller allocates nothing. A class
/// event allocates at each <see langword="new"/> of the caller.
/// </para>
/// <para>
/// <see cref="Subscribe{T}"/> and <see cref="Unsubscribe{T}"/> write a new array, so call them at build time.
/// <see cref="Publish{T}"/> reads the list that holds at the start of the call. A subscribe during a publish gets the
/// next publish. An unsubscribe during a publish takes effect at the next publish.
/// </para>
/// <para>
/// <see cref="Publish{T}"/> allocates nothing and throws nothing of its own. An exception of a subscriber leaves
/// <see cref="Publish{T}"/>, and the later subscribers get no call for that publish. The same handler and target pair
/// can subscribe twice and then gets two calls. <see cref="Unsubscribe{T}"/> removes the first match.
/// </para>
/// <para>
/// The bus holds each target until the unsubscribe. The bus runs on the UI thread and uses no lock.
/// </para>
/// </remarks>
public sealed class EventBus
{
    /// <summary>The channel of each event type. A channel exists after the first subscribe of its type.</summary>
    private readonly Dictionary<Type, object> _channels = new();

    /// <summary>Creates an empty bus.</summary>
    public EventBus()
    {
    }

    /// <summary>
    /// Adds a subscriber to the event type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// The subscriber goes last in the order of the call. A subscribe during a publish gets the next publish. The
    /// method writes a new array, so call it at build time.
    /// </remarks>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="handler">The handler. Pass a static method in a <see langword="static"/> field.</param>
    /// <param name="target">The object that the bus gives to the handler. It can be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public void Subscribe<T>(EventBusHandler<T> handler, object? target)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Channel<T> channel;
        if (_channels.TryGetValue(typeof(T), out object? c))
        {
            channel = (Channel<T>)c;
        }
        else
        {
            channel = new Channel<T>();
            _channels.Add(typeof(T), channel);
        }

        Subscriber<T>[] old = channel.Items;
        Subscriber<T>[] items = new Subscriber<T>[old.Length + 1];
        Array.Copy(old, items, old.Length);
        items[old.Length] = new Subscriber<T>(handler, target);
        channel.Items = items;
    }

    /// <summary>
    /// Removes the first subscriber that matches a handler and a target.
    /// </summary>
    /// <remarks>
    /// An unsubscribe during a publish takes effect at the next publish. The method writes a new array, so call it at
    /// build time.
    /// </remarks>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="handler">The handler of the subscribe.</param>
    /// <param name="target">The target of the subscribe. The compare is by reference.</param>
    /// <returns><see langword="true"/> when the bus removed a subscriber. <see langword="false"/> when none matched.</returns>
    public bool Unsubscribe<T>(EventBusHandler<T> handler, object? target)
    {
        if (!_channels.TryGetValue(typeof(T), out object? c))
        {
            return false;
        }

        Channel<T> channel = (Channel<T>)c;
        Subscriber<T>[] old = channel.Items;
        for (int i = 0; i < old.Length; i++)
        {
            if (old[i].Handler == handler && old[i].Target == target)
            {
                Subscriber<T>[] items = new Subscriber<T>[old.Length - 1];
                Array.Copy(old, 0, items, 0, i);
                Array.Copy(old, i + 1, items, i, old.Length - i - 1);
                channel.Items = items;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calls each subscriber of the event type <typeparamref name="T"/> with the event, in the order of the subscribe.
    /// </summary>
    /// <remarks>
    /// The method reads the list that holds at the start of the call. A publish of a type with no subscriber does
    /// nothing. The method allocates nothing and throws nothing of its own. An exception of a subscriber leaves the
    /// method, and the later subscribers get no call for that publish.
    /// </remarks>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="e">The event. It is valid during the call only.</param>
    public void Publish<T>(in T e)
    {
        if (!_channels.TryGetValue(typeof(T), out object? c))
        {
            return;
        }

        Subscriber<T>[] items = ((Channel<T>)c).Items;
        for (int i = 0; i < items.Length; i++)
        {
            items[i].Handler(in e, items[i].Target);
        }
    }

    /// <summary>
    /// Gets the count of subscribers of the event type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <returns>The count of subscribers. 0 when the type has no subscriber.</returns>
    public int SubscriberCount<T>()
    {
        if (!_channels.TryGetValue(typeof(T), out object? c))
        {
            return 0;
        }

        return ((Channel<T>)c).Items.Length;
    }

    /// <summary>The subscribers of one event type.</summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    private sealed class Channel<T>
    {
        /// <summary>The subscribers in the order of the subscribe. Each write replaces the array.</summary>
        internal Subscriber<T>[] Items = [];
    }

    /// <summary>One subscriber: a handler and its target.</summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    private readonly struct Subscriber<T>
    {
        /// <summary>The handler.</summary>
        internal readonly EventBusHandler<T> Handler;

        /// <summary>The target that the bus gives to the handler.</summary>
        internal readonly object? Target;

        /// <summary>Creates a subscriber.</summary>
        /// <param name="handler">The handler.</param>
        /// <param name="target">The target.</param>
        internal Subscriber(EventBusHandler<T> handler, object? target)
        {
            Handler = handler;
            Target = target;
        }
    }
}
