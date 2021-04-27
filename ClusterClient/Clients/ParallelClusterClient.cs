using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace ClusterClient.Clients
{
    public class ParallelClusterClient : ClusterClientBase
    {
        public ParallelClusterClient(string[] replicaAddresses) : base(replicaAddresses)
        {
        }

        public override async Task<string> ProcessRequestAsync(string query, TimeSpan timeout)
        {
            var tasks = ReplicaAddresses
                .Select(async uri =>
                {
                    var request = CreateRequest(uri + "?query=" + query);
                    var responseTask = ProcessRequestAsync(request);
                    await Task.WhenAny(responseTask, Task.Delay(timeout));
                    if (!responseTask.IsCompletedSuccessfully)
                        throw new TimeoutException();
                    return responseTask.Result;
                }).ToHashSet();
            
            while (tasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(tasks);
                if (!finishedTask.IsCompletedSuccessfully)
                    tasks.Remove(finishedTask);
                else
                    return await finishedTask;
            }

            throw new TimeoutException();
        }

        protected override ILog Log => LogManager.GetLogger(typeof(ParallelClusterClient));
    }
}