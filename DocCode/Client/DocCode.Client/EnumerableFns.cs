using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Breeze.Sharp.Core
{
    public static class EnumerableFns
    {
        public static async Task InParallel<T>(this IEnumerable<T> items, Func<T, Task> f)
        {
            var tasks = items.Select(item => f(item));
            await Task.WhenAll(tasks);
        }

        public static async Task<IEnumerable<TResult>> InParallel<T, TResult>(this IEnumerable<T> items, Func<T, Task<TResult>> f)
        {
            Exception exception = null;
            var tasks = items.Select(async item =>
            {
                try
                {
                    return await f(item);
                }
                catch (Exception e)
                {
                    exception = e;
                    return default(TResult);
                }
                finally
                {
                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }).ToArray();
            var results = await Task.WhenAll(tasks);
            return results;
        }

        public static async Task<IEnumerable<TResult>> InParallel<T, TResult>(this IEnumerable<T> items, Func<T, Task<TResult>> f, int maxParallelism)
        {
            var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);
            Exception exception = null;
            var tasks = items.Select(async item =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await f(item);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        return default(TResult);
                    }
                    finally
                    {
                        semaphore.Release();
                        if (exception != null)
                        {
                            throw exception;
                        }
                    }
                }).ToArray();
            var results = await Task.WhenAll(tasks);
            return results;
        }

        public static Task<IEnumerable<TResult>> OneAtATime<T, TResult>(this IEnumerable<T> items, Func<T, Task<TResult>> f)
        {
            return items.InParallel<T, TResult>(f, 1);
        }

    }
}
