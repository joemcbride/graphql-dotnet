using GraphQL.DataLoader;
using GraphQL.DataLoader.Di.Sample.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public class SalespeopleByNameDataLoader(DealershipDbContext db) : DataLoaderBase<string, Salesperson>
{
    protected override async Task FetchAsync(IEnumerable<DataLoaderPair<string, Salesperson>> list, CancellationToken cancellationToken)
    {
        IQueryable<Salesperson> salesmen = db.Salespeople;
        var names = list.Select(pair => pair.Key);

        var lookup = await salesmen
            .Where(sm => names.Contains(sm.Name))
            .GroupBy(sm => sm.Name)
            .ToDictionaryAsync(group => group.Key, cancellationToken).ConfigureAwait(false);

        foreach (var pair in list)
        {
            pair.SetResult(lookup[pair.Key].Single());
        }
    }
}
