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
            var pendingTasks = new HashSet<Task>();
            for (var curReplica = 0; curReplica < ReplicaAddresses.Length; curReplica++)
            {
                var uri = ReplicaAddresses[curReplica];
                var request = CreateRequest(uri + "?query=" + query);
                var currentTask = ProcessRequestAsync(request);
                var timeoutForCurrentTask = timeout / (ReplicaAddresses.Length - curReplica);
                var timeoutTask = Task.Delay(timeoutForCurrentTask);
                pendingTasks.Add(currentTask);
                Task completedTask;
                do
                {
                    pendingTasks.Add(timeoutTask);
                    var sw = new Stopwatch();
                    sw.Start();
                    completedTask = await Task.WhenAny(pendingTasks);
                    sw.Stop();
                    pendingTasks.Remove(timeoutTask);
                    timeout -= sw.Elapsed;
                    pendingTasks.Remove(completedTask);

                    if (completedTask == timeoutTask)
                        break;

                    if (completedTask.IsCompletedSuccessfully)
                        return await (Task<string>) completedTask;
                } while (completedTask != currentTask);
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(SmartClusterClient));
    }
}