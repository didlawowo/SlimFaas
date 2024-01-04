﻿using Microsoft.Extensions.Logging;
using Moq;
using SlimFaas.Kubernetes;

namespace SlimFaas.Tests;

public class ReplicasScaleWorkerShould
{
    [Theory]
    [ClassData(typeof(DeploymentsTestData))]
    public async Task ScaleFunctionUpAndDown(DeploymentsInformations deploymentsInformations, Times scaleUpTimes,
        Times scaleDownTimes)
    {
        Mock<ILogger<ScaleReplicasWorker>> logger = new();
        Mock<IKubernetesService> kubernetesService = new();
        Mock<IMasterService> masterService = new();
        HistoryHttpMemoryService historyHttpService = new();
        historyHttpService.SetTickLastCall("fibonacci2", DateTime.Now.Ticks);
        Mock<ILogger<ReplicasService>> loggerReplicasService = new();
        ReplicasService replicasService =
            new(kubernetesService.Object, historyHttpService, loggerReplicasService.Object);
        masterService.Setup(ms => ms.IsMaster).Returns(true);
        kubernetesService.Setup(k => k.ListFunctionsAsync(It.IsAny<string>())).ReturnsAsync(deploymentsInformations);

        ReplicaRequest scaleRequestFibonacci1 = new("fibonacci1", "default", 0, PodType.Deployment);
        kubernetesService.Setup(k => k.ScaleAsync(scaleRequestFibonacci1)).ReturnsAsync(scaleRequestFibonacci1);
        ReplicaRequest scaleRequestFibonacci2 = new("fibonacci2", "default", 1, PodType.Deployment);
        kubernetesService.Setup(k => k.ScaleAsync(scaleRequestFibonacci2)).ReturnsAsync(scaleRequestFibonacci2);
        await replicasService.SyncDeploymentsAsync("default");

        ScaleReplicasWorker service = new(replicasService, masterService.Object, logger.Object, 100);
        Task task = service.StartAsync(CancellationToken.None);
        await Task.Delay(3000);

        kubernetesService.Verify(v => v.ScaleAsync(scaleRequestFibonacci2), scaleUpTimes);
        kubernetesService.Verify(v => v.ScaleAsync(scaleRequestFibonacci1), scaleDownTimes);

        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task LogErrorWhenExceptionIsThrown()
    {
        Mock<ILogger<ScaleReplicasWorker>> logger = new();
        Mock<IMasterService> masterService = new();
        masterService.Setup(ms => ms.IsMaster).Returns(true);
        Mock<IReplicasService> replicaService = new();
        replicaService.Setup(r => r.CheckScaleAsync(It.IsAny<string>())).Throws(new Exception());

        HistoryHttpMemoryService historyHttpService = new();
        historyHttpService.SetTickLastCall("fibonacci2", DateTime.Now.Ticks);

        ScaleReplicasWorker service = new(replicaService.Object, masterService.Object, logger.Object, 10);
        Task task = service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.AtLeastOnce);

        Assert.True(task.IsCompleted);
    }
}
