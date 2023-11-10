﻿using Newtonsoft.Json;

namespace SlimFaas;

public interface IMasterService
{
    bool IsMaster { get; }
    Task CheckAsync();
}

public class MasterService : IMasterService
{
    private readonly IRedisService _redisService;
    private readonly string _id = Guid.NewGuid().ToString();
    public bool IsMaster { get; private set; }
    private const string SlimFaasMaster = "slimfaas_master";
    private const string MasterId = "master_id";
    private const string LastTicks = "last_ticks";
    private readonly object Lock = new();

    public MasterService(IRedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task CheckAsync()
    {
        var dictionary= await _redisService.HashGetAllAsync(SlimFaasMaster);
        Console.WriteLine(JsonConvert.SerializeObject(dictionary));
        if (dictionary.Count == 0)
        {
            await _redisService.HashSetAsync(SlimFaasMaster, new Dictionary<string, string>
            {
                { MasterId, _id },
                { LastTicks, DateTime.Now.Ticks.ToString() },
            });
            return;
        }

        var currentMasterId = dictionary[MasterId];
        var currentTicks = long.Parse(dictionary[LastTicks]);
        var isMaster = currentMasterId == _id;
        if (isMaster != IsMaster)
        {
            lock (Lock)
            {
                IsMaster = isMaster;
            }
        }

        var isMasterTimeElapsed = TimeSpan.FromTicks(currentTicks) + TimeSpan.FromSeconds(5) < TimeSpan.FromTicks(DateTime.Now.Ticks);
        switch (isMaster)
        {
            case false when isMasterTimeElapsed:
            case true when !isMasterTimeElapsed:
                await _redisService.HashSetAsync(SlimFaasMaster, new Dictionary<string, string>
                {
                    { MasterId, _id },
                    { LastTicks, DateTime.Now.Ticks.ToString() },
                });
                break;
        }
    }

}
