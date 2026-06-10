namespace AgentGenCli.Cqrs;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult, TQuery>
{
    Task<Option<TResult>> RunQueryAsync(TQuery query, CancellationToken cancellationToken);
}
