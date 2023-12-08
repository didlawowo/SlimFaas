﻿using DotNext;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Net.Cluster.Consensus.Raft.Http;
using Microsoft.AspNetCore.Connections;
using SlimData;

namespace RaftNode;

public class Startup(IConfiguration configuration)
{
    public static readonly IList<string> ClusterMembers = new List<string>(2);
    

    public void Configure(IApplicationBuilder app)
    {
        const string LeaderResource = "/SlimData/leader";
        const string AddHashSetResource = "/SlimData/AddHashset";
        const string ListRightPopResource = "/SlimData/ListRightPop";
        const string ListLeftPushResource = "/SlimData/ListLeftPush";
        const string AddKeyValueResource = "/SlimData/AddKeyValue";
        const string ListLengthResource = "/SlimData/ListLength";
        const string HealthResource = "/SlimData/ListLength";

        app.UseConsensusProtocolHandler()
            .RedirectToLeader(LeaderResource)
            .RedirectToLeader(ListLengthResource)
            .RedirectToLeader(ListLeftPushResource)
            .RedirectToLeader(ListRightPopResource)
            .RedirectToLeader(AddKeyValueResource)
            .RedirectToLeader(AddHashSetResource)
            .UseRouting()
            .UseEndpoints(static endpoints =>
            {
                endpoints.MapGet(LeaderResource, Endpoints.RedirectToLeaderAsync);
                endpoints.MapGet(HealthResource, async context => { await context.Response.WriteAsync("OK"); });
                endpoints.MapPost(ListLeftPushResource,  Endpoints.ListLeftPush);
                endpoints.MapPost(ListRightPopResource,  Endpoints.ListRigthPop);
                endpoints.MapPost(AddHashSetResource,  Endpoints.AddHashSet);
                endpoints.MapPost(AddKeyValueResource,  Endpoints.AddKeyValue);
            });
    }


    public void ConfigureServices(IServiceCollection services)
    {
        services.UseInMemoryConfigurationStorage(AddClusterMembers)
            .ConfigureCluster<ClusterConfigurator>()
            .AddSingleton<IHttpMessageHandlerFactory, RaftClientHandlerFactory>()
            .AddOptions()
            .AddRouting();
        var path = configuration[SlimPersistentState.LogLocation];
        if (!string.IsNullOrWhiteSpace(path))
            services.UsePersistenceEngine<ISupplier<SupplierPayload>, SlimPersistentState>();
        var endpoint = configuration["publicEndPoint"];
        if (!string.IsNullOrEmpty(endpoint))
        {
            var uri = new Uri(endpoint);
            services.AddSingleton<SlimDataInfo>(sp => new SlimDataInfo(uri.Port));
        }
    }

    private static void AddClusterMembers(ICollection<UriEndPoint> members)
    {
        foreach (var clusterMember in ClusterMembers)
            members.Add(new UriEndPoint(new Uri(clusterMember, UriKind.Absolute)));
    }
}