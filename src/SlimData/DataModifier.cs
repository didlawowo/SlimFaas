﻿using System.Text;
using DotNext;
using DotNext.Net.Cluster.Consensus.Raft;

namespace RaftNode;

internal sealed class DataModifier : BackgroundService
{
    private readonly SimplePersistentState entry;
    private readonly IRaftCluster cluster;

    public DataModifier(IRaftCluster cluster, SimplePersistentState entry)
    {
        this.entry = entry;
        this.cluster = cluster;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            //Console.WriteLine("here");
            var leadershipToken = cluster.LeadershipToken;
            if (!leadershipToken.IsCancellationRequested)
            {
                var newValue = ((ISupplier<List<JsonPayload>>)entry).Invoke().Count;
                Console.WriteLine("Saving value {0} generated by the leader node", newValue);
                var source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, leadershipToken);
                try
                {
                    // StringLogEntry entry = new(GenerateStreamFromString(newValue), false);
                   // MyInterpreter interpreter = new MyInterpreter();
                    
                    var logEntry = entry.interpreter.CreateLogEntry(new AddKeyValueCommand() { Key = DateTime.Now.Ticks.ToString(), Value = "20" }, cluster.Term); 
                    // this.entry.CreateJsonLogEntry(new JsonPayload { Key = newValue, Value = 2, Message = "Hello" });
                    // var entry = state.CreateJsonLogEntry(new SubtractCommand { Key = 10, Value = 20 });
                    await entry.AppendAsync(logEntry, source.Token);

                    await entry.CommitAsync(source.Token);

                    //var isSuccess = await cluster.ReplicateAsync(entry, source.Token);
                    //Console.WriteLine($"Replication success : {isSuccess}");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected error {0}", e);
                }
                finally
                {
                    source?.Dispose();
                }
            }
        }
    }
}