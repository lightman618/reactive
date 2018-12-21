﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class ToAsyncEnumerable : AsyncEnumerableTests
    {
        [Fact]
        public void ToAsyncEnumerable_Null()
        {
            Assert.Throws<ArgumentNullException>(() => AsyncEnumerable.ToAsyncEnumerable(default(IEnumerable<int>)));
            Assert.Throws<ArgumentNullException>(() => AsyncEnumerable.ToAsyncEnumerable(default(IObservable<int>)));
            Assert.Throws<ArgumentNullException>(() => AsyncEnumerable.ToAsyncEnumerable(default(Task<int>)));
        }

        [Fact]
        public async Task ToAsyncEnumerable1Async()
        {
            var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();
            await HasNextAsync(e, 1);
            await HasNextAsync(e, 2);
            await HasNextAsync(e, 3);
            await HasNextAsync(e, 4);
            await NoNextAsync(e);
        }

        [Fact]
        public async Task ToAsyncEnumerable2Async()
        {
            var ex = new Exception("Bang");
            var xs = ToAsyncEnumerable_Sequence(ex).ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();
            await HasNextAsync(e, 42);
            await AssertThrowsAsync(e.MoveNextAsync(), ex);
        }

        private IEnumerable<int> ToAsyncEnumerable_Sequence(Exception e)
        {
            yield return 42;
            throw e;
        }

        [Fact]
        public async Task ToAsyncEnumerable3Async()
        {
            var subscribed = false;

            var xs = new MyObservable<int>(obs =>
            {
                subscribed = true;

                obs.OnNext(42);
                obs.OnCompleted();

                return new MyDisposable(() => { });
            }).ToAsyncEnumerable();

            Assert.False(subscribed);

            var e = xs.GetAsyncEnumerator();

            Assert.True(subscribed);

            await HasNextAsync(e, 42);
            await NoNextAsync(e);
        }

        [Fact]
        public async Task ToAsyncEnumerable4Async()
        {
            var ex = new Exception("Bang!");
            var subscribed = false;

            var xs = new MyObservable<int>(obs =>
            {
                subscribed = true;

                obs.OnError(ex);

                return new MyDisposable(() => { });
            }).ToAsyncEnumerable();

            Assert.False(subscribed);

            var e = xs.GetAsyncEnumerator();

            Assert.True(subscribed);

            await AssertThrowsAsync(e.MoveNextAsync(), ex);
        }

        [Fact]
        public async Task ToAsyncEnumerable5Async()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4 });

            var xs = set.ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();
            await HasNextAsync(e, 1);
            await HasNextAsync(e, 2);
            await HasNextAsync(e, 3);
            await HasNextAsync(e, 4);
            await NoNextAsync(e);
        }

        [Fact]
        public async Task ToAsyncEnumerable6()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            var xs = set.ToAsyncEnumerable();

            var arr = await xs.ToArrayAsync();

            Assert.True(set.SetEquals(arr));
        }

        [Fact]
        public async Task ToAsyncEnumerable7()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4 });
            var xs = set.ToAsyncEnumerable();

            var arr = await xs.ToListAsync();

            Assert.True(set.SetEquals(arr));
        }

        [Fact]
        public async Task ToAsyncEnumerable8()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4 });
            var xs = set.ToAsyncEnumerable();

            var c = await xs.CountAsync();

            Assert.Equal(set.Count, c);
        }

        [Fact]
        public async Task ToAsyncEnumerable9()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4 });
            var xs = set.ToAsyncEnumerable();

            await SequenceIdentity(xs);
        }

        [Fact]
        public async Task ToAsyncEnumerable10()
        {
            var xs = new[] { 1, 2, 3, 4 }.ToAsyncEnumerable();
            await SequenceIdentity(xs);
        }

        [Fact]
        public void ToAsyncEnumerable11()
        {
            var set = new HashSet<int>(new[] { 1, 2, 3, 4 });
            var xs = set.ToAsyncEnumerable();

            var xc = xs as ICollection<int>;

            Assert.NotNull(xc);

            Assert.False(xc.IsReadOnly);

            xc.Add(5);


            Assert.True(xc.Contains(5));

            Assert.True(xc.Remove(5));

            var arr = new int[4];
            xc.CopyTo(arr, 0);
            Assert.True(arr.SequenceEqual(xc));
            xc.Clear();
            Assert.Equal(0, xc.Count);
        }

        [Fact]
        public void ToAsyncEnumerable12()
        {
            var set = new List<int> { 1, 2, 3, 4 };
            var xs = set.ToAsyncEnumerable();

            var xl = xs as IList<int>;

            Assert.NotNull(xl);

            Assert.False(xl.IsReadOnly);

            xl.Add(5);


            Assert.True(xl.Contains(5));

            Assert.True(xl.Remove(5));

            xl.Insert(2, 10);

            Assert.Equal(2, xl.IndexOf(10));
            xl.RemoveAt(2);

            xl[0] = 7;
            Assert.Equal(7, xl[0]);

            var arr = new int[4];
            xl.CopyTo(arr, 0);
            Assert.True(arr.SequenceEqual(xl));
            xl.Clear();
            Assert.Equal(0, xl.Count);

        }

        [Fact]
        public async Task ToAsyncEnumerable_With_Completed_TaskAsync()
        {
            var task = Task.Factory.StartNew(() => 36);

            var xs = task.ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();

            Assert.True(await e.MoveNextAsync());
            Assert.Equal(36, e.Current);
            Assert.False(await e.MoveNextAsync());
        }

        [Fact]
        public async Task ToAsyncEnumerable_With_Faulted_TaskAsync()
        {
            var ex = new InvalidOperationException();
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(ex);

            var xs = tcs.Task.ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();

            await AssertThrowsAsync(e.MoveNextAsync(), ex);
        }

        [Fact]
        public async Task ToAsyncEnumerable_With_Canceled_TaskAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();

            var xs = tcs.Task.ToAsyncEnumerable();
            var e = xs.GetAsyncEnumerator();

            await AssertThrowsAsync<TaskCanceledException>(e.MoveNextAsync().AsTask());
        }

        private sealed class MyObservable<T> : IObservable<T>
        {
            private readonly Func<IObserver<T>, IDisposable> _subscribe;

            public MyObservable(Func<IObserver<T>, IDisposable> subscribe)
            {
                _subscribe = subscribe;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return _subscribe(observer);
            }
        }

        private sealed class MyDisposable : IDisposable
        {
            private readonly Action _dispose;

            public MyDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }
        }
    }
}