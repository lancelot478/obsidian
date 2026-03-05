using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SAGA.Editor
{
    public static class ExecuteHelper
    {
        public static bool ParallelRun<T>(IList<T> data, Action<T> action)
        {
            if (action == null)
            {
                return false;
            }

            var degree = SystemInfo.processorCount;
            var result = Parallel.ForEach(data, new ParallelOptions {MaxDegreeOfParallelism = degree}, (obj, state) =>
            {
                try
                {
                    action.Invoke(obj);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    state.Break();
                }
            });
            var allComplete = result.IsCompleted;
            return allComplete;
        }
    }
}