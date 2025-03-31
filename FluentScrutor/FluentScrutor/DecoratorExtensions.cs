using System.ComponentModel; // For EditorBrowsable
using Microsoft.Extensions.DependencyInjection;

namespace FluentScrutor
{
    /// <summary>
    /// Provides the entry point extension method <see cref="DecorateBase{TService, TImplementation}"/>
    /// for registering services and their decorators using a fluent interface, building upon Scrutor.
    /// </summary>
    /// <remarks>
    /// This library provides a fluent API layer over Scrutor's decoration capabilities
    /// to simplify the registration of decorator chains and configuration of the base service lifetime.
    /// </remarks>
    public static class DecoratorExtensions
    {
        /// <summary>
        /// Begins the fluent configuration for registering a service (<typeparamref name="TService"/>)
        /// with its base implementation (<typeparamref name="TImplementation"/>) and any subsequent decorators.
        /// </summary>
        /// <typeparam name="TService">The service interface type. Must be a reference type.</typeparam>
        /// <typeparam name="TImplementation">The concrete base implementation of the service. Must be a reference type and implement <typeparamref name="TService"/>.</typeparam>
        /// <param name="services">The service collection to add registrations to.</param>
        /// <returns>An <see cref="IDecoratorBuilder{TService, TImplementation}"/> instance to configure decorators and lifetime.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This method initiates a configuration chain. You must call <see cref="IDecoratorBuilder{TService, TImplementation}.Register"/>
        /// at the end of the chain to finalize the registrations in the <see cref="IServiceCollection"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// services.DecorateBase&lt;IMyService, MyServiceImplementation&gt;()
        ///         .WithLifetime(ServiceLifetime.Scoped)
        ///         .DecoratedBy&lt;LoggingDecorator&gt;()
        ///         .DecoratedBy&lt;CachingDecorator&gt;()
        ///         .Register();
        /// </code>
        /// </example>
        public static IDecoratorBuilder<TService, TImplementation> DecorateBase<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            // Null check is handled within the DecoratorBuilder constructor/initializer.
            return new DecoratorBuilder<TService, TImplementation>(services);
        }
    }

    /// <summary>
    /// Defines a builder interface for configuring a service registration with a chain of decorators
    /// and specifying the lifetime of the base service implementation.
    /// </summary>
    /// <typeparam name="TService">The service type being registered and decorated.</typeparam>
    /// <typeparam name="TImplementation">The base implementation type for the service.</typeparam>
    public interface IDecoratorBuilder<TService, TImplementation>
        where TService : class
        where TImplementation : class, TService
    {
        /// <summary>
        /// Specifies the <see cref="ServiceLifetime"/> for the base service registration (<typeparamref name="TImplementation"/>).
        /// If not called, the default lifetime is <see cref="ServiceLifetime.Transient"/>.
        /// </summary>
        /// <param name="lifetime">The desired service lifetime (Singleton, Scoped, or Transient).</param>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Register"/> has already been called.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid <see cref="ServiceLifetime"/> enum value is provided.</exception>
        IDecoratorBuilder<TService, TImplementation> WithLifetime(ServiceLifetime lifetime);

        /// <summary>
        /// Adds a decorator type (<typeparamref name="TDecorator"/>) to the decoration chain for the <typeparamref name="TService"/>.
        /// Decorators are applied in the order they are added; the first decorator added wraps the base implementation,
        /// and subsequent decorators wrap the previously added one. The last decorator added becomes the outermost layer.
        /// </summary>
        /// <typeparam name="TDecorator">The decorator type to add. Must be a reference type and implement <typeparamref name="TService"/>.</typeparam>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Register"/> has already been called.</exception>
        /// <remarks>
        /// The decorator type <typeparamref name="TDecorator"/> must have a constructor that accepts <typeparamref name="TService"/>
        /// as a parameter (along with any other dependencies resolveable from the <see cref="IServiceProvider"/>)
        /// for Scrutor's decoration mechanism to work correctly.
        /// </remarks>
        IDecoratorBuilder<TService, TImplementation> DecoratedBy<TDecorator>() where TDecorator : class, TService;

        /// <summary>
        /// Finalizes the configuration and registers the base service (<typeparamref name="TImplementation"/>)
        /// and all specified decorators with the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <returns>The original <see cref="IServiceCollection"/> instance to allow for further standard registration chaining.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Register"/> has already been called on this builder instance.</exception>
        /// <remarks>
        /// This method performs the actual registration calls using Scrutor's decoration capabilities.
        /// After calling this method, the builder instance should not be used further.
        /// </remarks>
        IServiceCollection Register();

        /// <summary>
        /// Hides object members from IntelliSense to keep the fluent API clean.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object? obj);

        /// <summary>
        /// Hides object members from IntelliSense to keep the fluent API clean.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();

        /// <summary>
        /// Hides object members from IntelliSense to keep the fluent API clean.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();

        /// <summary>
        /// Hides object members from IntelliSense to keep the fluent API clean.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        string? ToString();
    }


    /// <summary>
    /// Internal implementation of the decorator builder.
    /// Uses C# 12 Primary Constructor and collection expression features.
    /// </summary>
    internal sealed class DecoratorBuilder<TService, TImplementation>(IServiceCollection services)
        : IDecoratorBuilder<TService, TImplementation>
        where TService : class
        where TImplementation : class, TService
    {

        private readonly object _lock = new();
        // Use primary constructor parameter directly for field initialization.
        // The null check ensures robustness immediately.
        private readonly IServiceCollection _services = services ?? throw new ArgumentNullException(nameof(services));
        // C# 12 collection expression for concise list initialization.
        private readonly List<Type> _decorators = [];
        private ServiceLifetime _baseLifetime = ServiceLifetime.Transient; // Default lifetime
        private bool _isRegistered = false; // Ensures Register() is called only once

        /// <inheritdoc />
        public IDecoratorBuilder<TService, TImplementation> WithLifetime(ServiceLifetime lifetime)
        {
            // Lock the entire operation on this builder instance
            lock (_lock)
            {
                CheckIfRegistered();

                // Add validation for the enum value - good practice for public-facing libraries
                if (!Enum.IsDefined(typeof(ServiceLifetime), lifetime))
                    // Use nameof() for parameter name consistency
                    throw new ArgumentOutOfRangeException(nameof(lifetime), $"Invalid {nameof(ServiceLifetime)} value: {lifetime}.");

                _baseLifetime = lifetime;
                return this;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Adds a decorator type (<typeparamref name="TDecorator"/>) to the decoration chain for the <typeparamref name="TService"/>.
        /// Decorators are applied in the order they are added; the first decorator added wraps the base implementation,
        /// and subsequent decorators wrap the previously added one. The last decorator added becomes the outermost layer.
        /// Adding the same decorator type multiple times to a single chain is not permitted and will throw an exception.
        /// </summary>
        /// <typeparam name="TDecorator">The decorator type to add. Must be a reference type and implement <typeparamref name="TService"/>.</typeparam>
        /// <returns>The same builder instance for fluent chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="Register"/> has already been called,
        /// or if the specified <typeparamref name="TDecorator"/> type has already been added to this decoration chain.
        /// </exception>
        /// <remarks>
        /// The decorator type <typeparamref name="TDecorator"/> must have a constructor that accepts <typeparamref name="TService"/>
        /// as a parameter (along with any other dependencies resolveable from the <see cref="IServiceProvider"/>)
        /// for Scrutor's decoration mechanism to work correctly.
        /// </remarks>
        public IDecoratorBuilder<TService, TImplementation> DecoratedBy<TDecorator>() where TDecorator : class, TService
        {
            lock (_lock)
            {
                CheckIfRegistered();

                var decoratorType = typeof(TDecorator);

                // Check for duplicate decorator types in this specific chain
                if (_decorators.Contains(decoratorType))
                {
                    throw new InvalidOperationException($"The decorator type '{decoratorType.FullName}' has already been added to the decoration chain for service '{typeof(TService).FullName}'. Adding the same decorator type multiple times in one chain is not permitted by this helper.");
                }

                _decorators.Add(decoratorType);
                return this;
            }
        }

        /// <inheritdoc />
        public IServiceCollection Register()
        {
            // Lock the entire registration process for this builder instance
            lock (_lock)
            {
                CheckIfRegistered(); // Check inside the lock

                // 1. Register the base implementation using standard IServiceCollection.Add
                // Scrutor's Decorate method will find this registration.
                _services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), _baseLifetime));

                // 2. Apply decorators using Scrutor's extension method.
                // This iterates through the collected decorator types and tells Scrutor to wrap
                // the existing TService registration with each decorator type sequentially.
                foreach (var decoratorType in _decorators)
                {
                    // Using the non-generic Decorate(Type, Type) overload is efficient here
                    // as it avoids needing reflection (MakeGenericMethod) inside the loop.
                    _services.Decorate(typeof(TService), decoratorType);
                }

                _isRegistered = true; // Mark builder as used.
                return _services; // Return collection for chaining.
            }
        }

        /// <summary>
        /// Checks if the Register method has already been called and throws if it has.
        /// Prevents misuse of the builder instance after registration is complete.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if Register() was already called.</exception>
        private void CheckIfRegistered()
        {
            // Usenameof(Register) for clarity in the error message.
            if (_isRegistered)
                throw new InvalidOperationException($"Cannot modify decorator configuration after {nameof(Register)}() has been called.");
        }

        #region Hide Object Members
        // Explicitly implement and hide standard object methods from IntelliSense
        // on the interface type to encourage focus on the fluent methods.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString() => base.ToString();
        #endregion
    }
}