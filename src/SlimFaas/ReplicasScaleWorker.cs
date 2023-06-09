﻿namespace SlimFaas;

public class ScaleReplicasWorker: BackgroundService
{
    private readonly IReplicasService _replicasService;
    private readonly IMasterService _masterService;
    private readonly ILogger<ScaleReplicasWorker> _logger;
    private readonly string _namespace;

    public ScaleReplicasWorker(IReplicasService replicasService, IMasterService masterService , ILogger<ScaleReplicasWorker> logger)
    {
        _replicasService = replicasService;
        _masterService = masterService;
        _logger = logger;
        _namespace =
            Environment.GetEnvironmentVariable("NAMESPACE") ?? "default";
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
         while (stoppingToken.IsCancellationRequested == false)
         {
             try
             {
                 await Task.Delay(1000, stoppingToken);
                 if(_masterService.IsMaster == false) continue;
                 await _replicasService.CheckScaleAsync(_namespace);
             }
             catch (Exception e)
             {
                _logger.LogError(e, "Global Error in ScaleReplicasWorker");
             }
         }
    }
}