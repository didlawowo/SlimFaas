﻿using Microsoft.Extensions.Logging;
using Moq;

namespace SlimFaas.Tests;



public class ReplicasScaleWorkerShould
{
    [Theory]
    [ClassData(typeof(DeploymentsTestData))]
    public async Task ScaleFunctionUpAndDown(DeploymentsInformations deploymentsInformations)
    {
        var logger = new Mock<ILogger<ScaleReplicasWorker>>();
        var kubernetesService = new Mock<IKubernetesService>();
        var masterService = new Mock<IMasterService>();
        var historyHttpService = new HistoryHttpMemoryService();
        historyHttpService.SetTickLastCall("fibonacci2", DateTime.Now.Ticks);
        var replicasService = new ReplicasService(kubernetesService.Object, historyHttpService);
        masterService.Setup(ms => ms.IsMaster).Returns(true);
        kubernetesService.Setup(k => k.ListFunctionsAsync(It.IsAny<string>())).ReturnsAsync(deploymentsInformations);
        
        var scaleRequestFibonacci1 = new ReplicaRequest { Replicas = 0, Deployment = "fibonacci1", Namespace = "default"};
        kubernetesService.Setup(k => k.ScaleAsync(scaleRequestFibonacci1)).ReturnsAsync(scaleRequestFibonacci1);
        var scaleRequestFibonacci2= new ReplicaRequest { Replicas = 1, Deployment = "fibonacci2", Namespace = "default"};
        kubernetesService.Setup(k => k.ScaleAsync(scaleRequestFibonacci2)).ReturnsAsync(scaleRequestFibonacci2);
        await replicasService.SyncDeploymentsAsync("default");
        
        var service = new ScaleReplicasWorker(replicasService, masterService.Object, logger.Object, 100);
        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(300);

        Assert.True(task.IsCompleted);
    }
}