﻿namespace SlimFaas;

public class HistoryHttpService
{
    private readonly RedisService _redisService;
    //private readonly IDictionary<string, long> _local = new Dictionary<string, long>();

    public HistoryHttpService(RedisService redisService)
    {
        _redisService = redisService;
    }
    
    public long GetTicksLastCall(string functionName)
    {
        
        var result = _redisService.Get(functionName);
        return string.IsNullOrEmpty(result) ? 0 : long.Parse(result);
    }
    
    public void SetTickLastCall(string functionName, long ticks)
    {
       _redisService.Set(functionName, ticks.ToString());
       //_local[functionName] = ticks;
    }
    
}