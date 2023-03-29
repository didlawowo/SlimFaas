﻿namespace SlimFaas;

public class ReplicasService
{
    private readonly HistoryHttpService _historyHttpService;
    private readonly IKubernetesService _kubernetesService;

    public ReplicasService(IKubernetesService kubernetesService, HistoryHttpService historyHttpService)
    {
        _kubernetesService = kubernetesService;
        _historyHttpService = historyHttpService;
        Functions = new List<DeploymentInformation>();
    }

    public IList<DeploymentInformation> Functions { get; private set; }

    public async Task SyncFunctionsAsync(string kubeNamespace)
    {
        var functions = await _kubernetesService.ListFunctionsAsync(kubeNamespace);
        lock (this)
        {
            Functions = functions;
        }
    }

    public Task CheckScaleAsync(string kubeNamespace)
    {
        var maximumTicks = 0L;
        IDictionary<string, long> ticksLastCall = new Dictionary<string, long>();
        foreach (var deploymentInformation in Functions)
        {
            var tickLastCall = _historyHttpService.GetTicksLastCall(deploymentInformation.Deployment);
            ticksLastCall.Add(deploymentInformation.Deployment, tickLastCall);
            maximumTicks = Math.Max(maximumTicks, tickLastCall);
        }

        var tasks = new List<Task>();
        foreach (var deploymentInformation in Functions)
        {
            var tickLastCall = deploymentInformation.ReplicasStartAsSoonAsOneFunctionRetrieveARequest
                ? maximumTicks
                : ticksLastCall[deploymentInformation.Deployment];

            var timeElapsedWhithoutRequest = TimeSpan.FromTicks(tickLastCall) +
                                             TimeSpan.FromSeconds(deploymentInformation
                                                 .TimeoutSecondBeforeSetReplicasMin) <
                                             TimeSpan.FromTicks(DateTime.Now.Ticks);
            var currentScale = deploymentInformation.Replicas;

            if (timeElapsedWhithoutRequest)
            {
                if (currentScale.HasValue && currentScale > deploymentInformation.ReplicasMin)
                {
                    var task = _kubernetesService.ScaleAsync(new ReplicaRequest
                    {
                        Replicas = deploymentInformation.ReplicasMin,
                        Deployment = deploymentInformation.Deployment,
                        Namespace = kubeNamespace
                    });

                    tasks.Append(task);
                }
            }
            else if (currentScale is 0)
            {
                // Fire and Forget
                var task = _kubernetesService.ScaleAsync(new ReplicaRequest
                {
                    Replicas = deploymentInformation.ReplicasAtStart,
                    Deployment = deploymentInformation.Deployment, Namespace = kubeNamespace
                });

                tasks.Append(task);
            }
        }

        Task.WaitAll(tasks.ToArray());

        return Task.CompletedTask;
    }
}