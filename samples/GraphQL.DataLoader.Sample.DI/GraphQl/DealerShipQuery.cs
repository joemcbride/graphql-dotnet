using GraphQL.Types;

namespace GraphQL.DataLoader.Sample.DI.GraphQl;

public sealed class DealerShipQuery : ObjectGraphType
{
    public DealerShipQuery()
    {
        Field<SalespersonGraphType>("salespeople")
            .Argument<string>("name")
            .Resolve(ctx =>
        {
            var name = ctx.GetArgument<string>("name");
            var loader = ctx.RequestServices!.GetRequiredService<SalespeopleByNameDataLoader>();
            return loader.LoadAsync(name);
        });
    }
}
