﻿using System.Text.Json;
using SlimFaas.Kubernetes;

namespace SlimFaas;

public class ReplicasSynchronizationWorker(IReplicasService replicasService,
        IMasterService masterService,
        IDatabaseService slimDataService,
        ILogger<ReplicasSynchronizationWorker> logger,
        int delay = EnvironmentVariables.ReplicasSynchronizationWorkerDelayMillisecondsDefault)
    : BackgroundService
{
    private readonly int _delay = EnvironmentVariables.ReadInteger(logger,
        EnvironmentVariables.ReplicasSynchronisationWorkerDelayMilliseconds, delay);

    private readonly string _namespace = Environment.GetEnvironmentVariable(EnvironmentVariables.Namespace) ??
                                         EnvironmentVariables.NamespaceDefault;
    public const string kubernetesDeployments = "kubernetes-deployments";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                if(masterService.IsMaster == false)
                {
                    await Task.Delay(_delay/10, stoppingToken);
                    var currentDeploymentsJson = await slimDataService.GetAsync(kubernetesDeployments);
                    Console.WriteLine(currentDeploymentsJson);
                    if (string.IsNullOrEmpty(currentDeploymentsJson))
                    {
                        return;
                    }
                    Console.WriteLine("currentDeploymentsJson");
                    var deployments = JsonSerializer.Deserialize(currentDeploymentsJson, DeploymentsInformationsSerializerContext.Default.DeploymentsInformations);
                    if (deployments == null)
                    {
                        return;
                    }
                    Console.WriteLine("SyncDeploymentsFromSlimData");
                    await replicasService.SyncDeploymentsFromSlimData(deployments);
                }
                else
                {
                    await Task.Delay(_delay, stoppingToken);
                    var deployments = await replicasService.SyncDeploymentsAsync(_namespace);
                    Console.WriteLine("SyncDeploymentsAsync");
                    var currentDeploymentsJson = await slimDataService.GetAsync(kubernetesDeployments);
                    Console.WriteLine(currentDeploymentsJson);
                    var newDeploymentsJson = JsonSerializer.Serialize(deployments, DeploymentsInformationsSerializerContext.Default.DeploymentsInformations);
                    if (currentDeploymentsJson != newDeploymentsJson)
                    {
                        Console.WriteLine("SetAsync");
                        await slimDataService.SetAsync(kubernetesDeployments, newDeploymentsJson);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Global Error in ScaleReplicasWorker");
            }
        }
    }
}
