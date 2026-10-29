using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BrutalUi.Framework;

/// <summary>
/// A registry that maps an exact type to one service.
/// </summary>
/// <remarks>
/// <para>
/// The registry maps an exact type to one service. <c>Register&lt;IAudio&gt;(audio)</c> resolves through
/// <c>IAudio</c> only, not through the class of the service.
/// </para>
/// <para>
/// Register at build time. <see cref="Resolve{T}"/> and <see cref="TryResolve{T}"/> allocate nothing and throw
/// nothing. A missing service gives <see langword="null"/> or <see langword="false"/>.
/// </para>
/// <para>
/// The registry holds each service until the game drops the registry. The game can register the
/// <see cref="CommandRegistry"/> and the <see cref="EventBus"/> as services.
/// </para>
/// <para>The registry runs on the UI thread and uses no lock.</para>
/// </remarks>
public sealed class ServiceRegistry
{
    /// <summary>The services, keyed by the exact type of the register.</summary>
    private readonly Dictionary<Type, object> _services = new();

    /// <summary>Creates an empty registry.</summary>
    public ServiceRegistry()
    {
    }

    /// <summary>Gets the count of the registered services.</summary>
    public int Count => _services.Count;

    /// <summary>Registers a service under the type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type to resolve the service through. A resolve must name this exact type.</typeparam>
    /// <param name="service">The service. It must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The registry already holds a service under <typeparamref name="T"/>.</exception>
    public void Register<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        if (_services.ContainsKey(typeof(T)))
        {
            throw new ArgumentException("The registry already holds a service of this type.", nameof(service));
        }

        _services.Add(typeof(T), service);
    }

    /// <summary>Gets the service that was registered under the type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The exact type of the register.</typeparam>
    /// <returns>The service, or <see langword="null"/> when no service holds this type.</returns>
    public T? Resolve<T>() where T : class
    {
        return TryResolve(out T? service) ? service : null;
    }

    /// <summary>Tries to get the service that was registered under the type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The exact type of the register.</typeparam>
    /// <param name="service">The service, or <see langword="null"/> when no service holds this type.</param>
    /// <returns><see langword="true"/> when the registry holds the service.</returns>
    public bool TryResolve<T>([NotNullWhen(true)] out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out object? found))
        {
            service = (T)found;
            return true;
        }

        service = null;
        return false;
    }
}
