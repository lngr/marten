using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Baseline.Dates;
using Marten.AsyncDaemon.Testing.TestingSupport;
using Marten.Events.Daemon;
using Marten.Events.Projections;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Marten.AsyncDaemon.Testing
{
    public class build_aggregate_projection: DaemonContext
    {
        public build_aggregate_projection(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task end_to_end_with_events_already_published()
        {
            NumberOfStreams = 10;

            Logger.LogDebug($"The expected number of events is " + NumberOfEvents);

            StoreOptions(x => x.Events.Projections.Add(new TripAggregation(), ProjectionLifecycle.Async), true);

            var agent = await StartNodeAgent();

            await PublishSingleThreaded();


            var shard = theStore.Events.Projections.AllShards().Single();
            var waiter = agent.Tracker.WaitForShardState(new ShardState(shard, NumberOfEvents), 15.Seconds());

            await agent.StartShard(shard.Name.Identity, CancellationToken.None);

            await waiter;

            await CheckAllExpectedAggregatesAgainstActuals();
        }

        [Fact]
        public async Task rebuild_the_projection()
        {
            NumberOfStreams = 10;

            Logger.LogDebug($"The expected number of events is " + NumberOfEvents);

            StoreOptions(x => x.Events.Projections.Add(new TripAggregation(), ProjectionLifecycle.Async), true);

            var agent = await StartNodeAgent();

            await PublishSingleThreaded();


            var waiter = agent.Tracker.WaitForShardState(new ShardState("Trip:All", NumberOfEvents), 30.Seconds());

            await waiter;
            Logger.LogDebug("About to rebuild Trip:All");
            await agent.RebuildProjection("Trip", CancellationToken.None);
            Logger.LogDebug("Done rebuilding Trip:All");
            await CheckAllExpectedAggregatesAgainstActuals();
        }


    }
}
