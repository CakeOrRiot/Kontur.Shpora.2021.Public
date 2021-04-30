using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class SmartClusterClient : ClusterClientBase
    {
        public SmartClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }


        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var pendingTasks = new HashSet<Task<string>>();
            for (var curReplica = 0; curReplica < ReplicaAddresses.Length; curReplica++)
            {
                var uri = ReplicaAddresses[curReplica];
                var request = CreateRequest(uri + "?query=" + query);
                var currentTask = ProcessRequestAsync(request);
                var timeoutForCurrentTask = timeout / (ReplicaAddresses.Length - curReplica);
                var timeoutTask = Task.Delay(timeoutForCurrentTask);
                var sw = Stopwatch.StartNew();
                var completedTask = await Task.WhenAny(AnySucsessfulFromPool(pendingTasks), timeoutTask, currentTask);
                timeout -= sw.Elapsed;
                pendingTasks.Add(currentTask);
                if (completedTask == timeoutTask)
                    continue;
                if (completedTask.IsCompletedSuccessfully)
                    return await (Task<string>) completedTask;
            }

            throw new TimeoutException();
        }

        private async Task<string> AnySucsessfulFromPool(HashSet<Task<string>> taskPool)
        {
            taskPool = taskPool.ToHashSet();
            while (taskPool.Count > 0)
            {
                var completed = await Task.WhenAny(taskPool);
                if (completed.IsCompletedSuccessfully)
                    return completed.Result;
                taskPool.Remove(completed);
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}