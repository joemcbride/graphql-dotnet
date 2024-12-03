using GraphQL;
using GraphQL.Types;

namespace GraphQL.DataLoader.Di.Sample.GraphQl;

public class DealerShipQuery : ObjectGraphType
{
    public DealerShipQuery()
    {
        Field<SalesmanGraphType>("salespeople")
            .Argument<string>("name")
            .Resolve(ctx =>
        {
            var name = ctx.GetArgument<string>("name");
            var loader = ctx.RequestServices!.GetRequiredService<SalespeopleByNameDataLoader>();
            return loader.LoadAsync(name);
        });
    }
}
