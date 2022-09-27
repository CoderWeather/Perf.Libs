using System.Collections.Concurrent;
using Mediator.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Mediator.Scope;

public static class Extensions {
	public static void AddScopedMediatorCore(this IServiceCollection services) {
		services.TryAddSingleton<IScopeRegistry, ScopeRegistry>();
	}
}

public interface IScopedMediator : ISender, IDisposable, IAsyncDisposable {
	Guid CorrelationId { get; }
	bool RegisterEntry<TEntry>(TEntry entry) where TEntry : notnull;
	void AttachToScope(Guid correlationId);
	void ReleaseScope();
	Guid RecreateScope();
}

public interface IScopeRegistry {
	bool RegisterEntry<TEntry>(Guid correlationId, TEntry entry) where TEntry : notnull;
	bool TryGetEntry<TEntry>(Guid correlationId, out TEntry? entry) where TEntry : notnull;
	bool DropEntry(Guid correlationId);
}

internal sealed record ScopeRegistry(
	ILogger<ScopeRegistry> Logger
) : IScopeRegistry, IDisposable, IAsyncDisposable {
	private readonly ConcurrentDictionary<Guid, object> registry = new(Environment.ProcessorCount, 32);

	public bool RegisterEntry<TEntry>(Guid correlationId, TEntry entry) where TEntry : notnull => registry.TryAdd(correlationId, entry);

	public bool TryGetEntry<TEntry>(Guid correlationId, out TEntry entry) where TEntry : notnull {
		if (registry.TryGetValue(correlationId, out var o)) {
			if (o is TEntry e) {
				entry = e;
				return true;
			}
		}

		entry = default!;
		return false;
	}

	public bool DropEntry(Guid correlationId) => registry.TryRemove(correlationId, out _);

	private bool disposed;

	public void Dispose() {
		if (disposed is false) {
			foreach (var (_, v) in registry) {
				switch (v) {
					case IDisposable d:
						d.Dispose();
						break;
					case IAsyncDisposable ad:
						ad.DisposeAsync().GetAwaiter().GetResult();
						break;
				}
			}

			registry.Clear();
			disposed = true;
		}
	}

	public async ValueTask DisposeAsync() {
		if (disposed is false) {
			foreach (var (_, v) in registry) {
				switch (v) {
					case IAsyncDisposable ad:
						await ad.DisposeAsync();
						break;
					case IDisposable d:
						d.Dispose();
						break;
				}
			}

			registry.Clear();
			disposed = true;
		}
	}
}

#region Messages scoped wrappers

public readonly record struct ScopedRequest<TIn, TOut>(
	TIn Payload,
	Guid CorrelationId,
	IServiceProvider Services
) : IRequest<TOut> where TIn : IRequest<TOut> {
	public static implicit operator TIn(ScopedRequest<TIn, TOut> r) => r.Payload;

	public TEntry GetEntry<TEntry>() where TEntry : notnull =>
		TryGetEntry(out TEntry? e)
			? e!
			: throw new ArgumentException($"No {typeof(TEntry)} entry found for correlation id: '{CorrelationId}'");

	private IScopeRegistry Registry { get; } = Services.GetService<IScopeRegistry>()!;
	private bool TryGetEntry<TEntry>(out TEntry? entry) where TEntry : notnull => Registry.TryGetEntry(CorrelationId, out entry);
}

public readonly record struct ScopedCommand<TIn, TOut>(
	TIn Payload,
	Guid CorrelationId,
	IServiceProvider Services
) : ICommand<TOut> where TIn : ICommand<TOut> {
	public static implicit operator TIn(ScopedCommand<TIn, TOut> c) => c.Payload;

	public TEntry GetEntry<TEntry>() where TEntry : notnull =>
		TryGetEntry(out TEntry? e)
			? e!
			: throw new ArgumentException($"No {typeof(TEntry)} entry found for correlation id: '{CorrelationId}'");

	private IScopeRegistry Registry { get; } = Services.GetService<IScopeRegistry>()!;
	private bool TryGetEntry<TEntry>(out TEntry? entry) where TEntry : notnull => Registry.TryGetEntry(CorrelationId, out entry);
}

public readonly record struct ScopedQuery<TIn, TOut>(
	TIn Payload,
	Guid CorrelationId,
	IServiceProvider Services
) : IQuery<TOut> where TIn : IQuery<TOut> {
	public static implicit operator TIn(ScopedQuery<TIn, TOut> q) => q.Payload;

	public TEntry GetEntry<TEntry>() where TEntry : notnull =>
		TryGetEntry(out TEntry? e)
			? e!
			: throw new ArgumentException($"No {typeof(TEntry)} entry found for correlation id: '{CorrelationId}'");

	private IScopeRegistry Registry { get; } = Services.GetService<IScopeRegistry>()!;
	private bool TryGetEntry<TEntry>(out TEntry? entry) where TEntry : notnull => Registry.TryGetEntry(CorrelationId, out entry);
}

#endregion

#region Source Generation Handler Markers

public interface IScopedRequestHandler<in TRequest> where TRequest : IRequest { }

public interface IScopedCommandHandler<in TCommand> where TCommand : ICommand { }

public interface IScopedQueryHandler<in TQuery> where TQuery : IQuery { }

#endregion

#region Handlers

public interface IScopedRequestHandler<TRequest, TResponse> :
	IRequestHandler<ScopedRequest<TRequest, TResponse>, TResponse>
	where TRequest : IRequest<TResponse> { }

public interface IScopedCommandHandler<TCommand, TResponse> :
	ICommandHandler<ScopedCommand<TCommand, TResponse>, TResponse>
	where TCommand : ICommand<TResponse> { }

public interface IScopedQueryHandler<TQuery, TResponse> :
	IQueryHandler<ScopedQuery<TQuery, TResponse>, TResponse>
	where TQuery : IQuery<TResponse> { }

#endregion
