﻿using Microsoft.Extensions.Logging;
using Moq;

namespace SlimFaas.Tests;

public class ReplicasScaleWorkerTests
{
     [Fact]
    public async Task WorkerShouldCallOneFunctionAsync()
    {
        var logger = new Mock<ILogger<ScaleReplicasWorker>>();
        var kubernetesService = new Mock<IKubernetesService>();
        var masterService = new Mock<IMasterService>();
        var historyHttpService = new HistoryHttpMemoryService();
        var replicasService = new ReplicasService(kubernetesService.Object, historyHttpService);
        
        masterService.Setup(ms => ms.IsMaster).Returns(true);
        
        var service = new ScaleReplicasWorker(replicasService, masterService.Object, logger.Object);

        var task = service.StartAsync(CancellationToken.None);

        await Task.Delay(3000);

        Assert.True(task.IsCompleted);
    }
}