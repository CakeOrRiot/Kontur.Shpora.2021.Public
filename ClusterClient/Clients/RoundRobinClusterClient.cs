using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class RoundRobinClusterClient : ClusterClientBase
    {
        public RoundRobinClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            for (var curReplica = 0; curReplica < ReplicaAddresses.Length; curReplica++)
            {
                var uri = ReplicaAddresses[curReplica];
                var request = CreateRequest(uri + "?query=" + query);
                var responseTask = ProcessRequestAsync(request);
                var timeoutForCurrentTask = timeout / (ReplicaAddresses.Length - curReplica);
                var timeoutTask = Task.Delay(timeoutForCurrentTask);
                var sw = new Stopwatch();
                sw.Start();
                await Task.WhenAny(responseTask, timeoutTask);
                sw.Stop();
                timeout -= sw.Elapsed;
                if (responseTask.IsCompletedSuccessfully)
                    return await responseTask;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(RoundRobinClusterClient));
    }
}