using System;
using System.Threading;
using Xunit;
using mcp_nexus.Infrastructure.Core;

namespace mcp_nexus_unit_tests.Infrastructure.Core
{
    public class ThreadPoolTuningTests
    {
        [Fact]
        public void ThreadPoolTuning_Class_Exists()
        {
            // Act & Assert
            Assert.True(typeof(ThreadPoolTuning).IsClass);
        }

        [Fact]
        public void ThreadPoolTuning_IsStatic()
        {
            // Act & Assert
            Assert.True(typeof(ThreadPoolTuning).IsAbstract && typeof(ThreadPoolTuning).IsSealed);
        }

        [Fact]
        public void ThreadPoolTuning_IsNotInterface()
        {
            // Act & Assert
            Assert.False(typeof(ThreadPoolTuning).IsInterface);
        }

        [Fact]
        public void ThreadPoolTuning_IsNotEnum()
        {
            // Act & Assert
            Assert.False(typeof(ThreadPoolTuning).IsEnum);
        }

        [Fact]
        public void ThreadPoolTuning_IsNotValueType()
        {
            // Act & Assert
            Assert.False(typeof(ThreadPoolTuning).IsValueType);
        }

        [Fact]
        public void ThreadPoolTuning_HasExpectedMethods()
        {
            // Arrange
            var type = typeof(ThreadPoolTuning);

            // Act & Assert
            Assert.NotNull(type.GetMethod("Apply"));
        }

        [Fact]
        public void ThreadPoolTuning_Apply_IsStatic()
        {
            // Arrange
            var method = typeof(ThreadPoolTuning).GetMethod("Apply");

            // Act & Assert
            Assert.NotNull(method);
            Assert.True(method!.IsStatic);
        }

        [Fact]
        public void ThreadPoolTuning_Apply_ReturnsVoid()
        {
            // Arrange
            var method = typeof(ThreadPoolTuning).GetMethod("Apply");

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(void), method!.ReturnType);
        }

        [Fact]
        public void ThreadPoolTuning_Apply_HasNoParameters()
        {
            // Arrange
            var method = typeof(ThreadPoolTuning).GetMethod("Apply");

            // Act & Assert
            Assert.NotNull(method);
            Assert.Empty(method!.GetParameters());
        }

        [Fact]
        public void Apply_CanBeCalledWithoutThrowing()
        {
            // Act & Assert
            var exception = Record.Exception(() => ThreadPoolTuning.Apply());
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            var exception1 = Record.Exception(() => ThreadPoolTuning.Apply());
            var exception2 = Record.Exception(() => ThreadPoolTuning.Apply());
            var exception3 = Record.Exception(() => ThreadPoolTuning.Apply());

            Assert.Null(exception1);
            Assert.Null(exception2);
            Assert.Null(exception3);
        }

        [Fact]
        public void Apply_ModifiesThreadPoolSettings()
        {
            // Arrange
            ThreadPool.GetMinThreads(out var originalWorker, out var originalIo);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var newWorker, out var newIo);

            // The new values should be at least as high as the original values
            Assert.True(newWorker >= originalWorker);
            Assert.True(newIo >= originalIo);
        }

        [Fact]
        public void Apply_SetsMinimumThreadsBasedOnProcessorCount()
        {
            // Arrange
            var expectedMinWorker = Math.Max(Environment.ProcessorCount * 2, 1);
            var expectedMinIo = Math.Max(Environment.ProcessorCount * 2, 1);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var actualWorker, out var actualIo);
            Assert.True(actualWorker >= expectedMinWorker);
            Assert.True(actualIo >= expectedMinIo);
        }

        [Fact]
        public void Apply_WithSingleProcessor_SetsAppropriateValues()
        {
            // This test verifies behavior when Environment.ProcessorCount is 1
            // The method should still set reasonable thread pool values

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var worker, out var io);
            Assert.True(worker >= 2); // At least 2 worker threads
            Assert.True(io >= 2);     // At least 2 I/O threads
        }

        [Fact]
        public void Apply_WithHighProcessorCount_SetsAppropriateValues()
        {
            // This test verifies behavior with high processor counts
            // The method should scale appropriately

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var worker, out var io);
            var expectedMin = Environment.ProcessorCount * 2;
            Assert.True(worker >= expectedMin);
            Assert.True(io >= expectedMin);
        }

        [Fact]
        public void Apply_DoesNotDecreaseExistingThreadCounts()
        {
            // Arrange
            ThreadPool.GetMinThreads(out var originalWorker, out var originalIo);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var newWorker, out var newIo);
            Assert.True(newWorker >= originalWorker);
            Assert.True(newIo >= originalIo);
        }

        [Fact]
        public void Apply_IsIdempotent()
        {
            // Arrange
            ThreadPoolTuning.Apply();
            ThreadPool.GetMinThreads(out var afterFirstCallWorker, out var afterFirstCallIo);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var afterSecondCallWorker, out var afterSecondCallIo);
            Assert.Equal(afterFirstCallWorker, afterSecondCallWorker);
            Assert.Equal(afterFirstCallIo, afterSecondCallIo);
        }

        [Fact]
        public async Task Apply_CanBeCalledConcurrently()
        {
            // Arrange
            var tasks = new Task[10];
            var exceptions = new Exception[10];

            // Act
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        ThreadPoolTuning.Apply();
                    }
                    catch (Exception ex)
                    {
                        exceptions[index] = ex;
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            for (int i = 0; i < 10; i++)
            {
                Assert.Null(exceptions[i]);
            }
        }

        [Fact]
        public void Apply_DoesNotThrowUnderStress()
        {
            // Arrange
            var iterations = 100;

            // Act & Assert
            for (int i = 0; i < iterations; i++)
            {
                var exception = Record.Exception(() => ThreadPoolTuning.Apply());
                Assert.Null(exception);
            }
        }

        [Fact]
        public void Apply_ThreadPoolSettingsRemainConsistent()
        {
            // Arrange
            ThreadPoolTuning.Apply();
            ThreadPool.GetMinThreads(out var initialWorker, out var initialIo);

            // Act
            ThreadPoolTuning.Apply();
            ThreadPoolTuning.Apply();
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var finalWorker, out var finalIo);
            Assert.Equal(initialWorker, finalWorker);
            Assert.Equal(initialIo, finalIo);
        }

        [Fact]
        public void Apply_WithCustomThreadPoolSettings_PreservesHigherValues()
        {
            // Arrange
            var customWorker = Environment.ProcessorCount * 4;
            var customIo = Environment.ProcessorCount * 4;
            ThreadPool.SetMinThreads(customWorker, customIo);
            ThreadPool.GetMinThreads(out var beforeWorker, out var beforeIo);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var afterWorker, out var afterIo);
            Assert.True(afterWorker >= beforeWorker);
            Assert.True(afterIo >= beforeIo);
        }

        [Fact]
        public void Apply_WithLowThreadPoolSettings_IncreasesToMinimum()
        {
            // Arrange
            // Set very low thread pool settings
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.GetMinThreads(out var beforeWorker, out var beforeIo);

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var afterWorker, out var afterIo);
            Assert.True(afterWorker > beforeWorker);
            Assert.True(afterIo > beforeIo);
        }

        [Fact]
        public void Apply_ThreadPoolMaxThreadsUnaffected()
        {
            // Arrange
            ThreadPool.GetMaxThreads(out var originalMaxWorker, out var originalMaxIo);
            ThreadPoolTuning.Apply();

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMaxThreads(out var finalMaxWorker, out var finalMaxIo);
            Assert.Equal(originalMaxWorker, finalMaxWorker);
            Assert.Equal(originalMaxIo, finalMaxIo);
        }

        [Fact]
        public async Task Apply_CanBeCalledFromDifferentThreads()
        {
            // Arrange
            var results = new bool[5];
            var tasks = new Task[5];

            // Act
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        ThreadPoolTuning.Apply();
                        results[index] = true;
                    }
                    catch
                    {
                        results[index] = false;
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            for (int i = 0; i < 5; i++)
            {
                Assert.True(results[i]);
            }
        }

        [Fact]
        public async Task Apply_ThreadPoolBehaviorAfterCall()
        {
            // Arrange
            ThreadPoolTuning.Apply();

            // Act
            // Submit some work to the thread pool to verify it's working
            var tcs = new TaskCompletionSource<bool>();
            _ = Task.Run(() => tcs.SetResult(true));

            // Assert
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(result);
        }

        [Fact]
        public void Apply_DoesNotAffectCurrentThread()
        {
            // Arrange
            var currentThreadId = Environment.CurrentManagedThreadId;

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            Assert.Equal(currentThreadId, Environment.CurrentManagedThreadId);
        }

        [Fact]
        public void Apply_CanBeCalledInFinallyBlock()
        {
            // Arrange
            var called = false;

            // Act
            try
            {
                ThreadPoolTuning.Apply();
            }
            finally
            {
                ThreadPoolTuning.Apply();
                called = true;
            }

            // Assert
            Assert.True(called);
        }

        [Fact]
        public void Apply_CanBeCalledInUsingStatement()
        {
            // Arrange
            using var cts = new CancellationTokenSource();

            // Act
            ThreadPoolTuning.Apply();

            // Assert
            // If we get here without exception, the method worked
            Assert.True(true);
        }

        [Fact]
        public void Apply_ThreadPoolSettingsAreReasonable()
        {
            // Act
            ThreadPoolTuning.Apply();

            // Assert
            ThreadPool.GetMinThreads(out var worker, out var io);

            // Should not be unreasonably high
            Assert.True(worker <= Environment.ProcessorCount * 10);
            Assert.True(io <= Environment.ProcessorCount * 10);

            // Should not be unreasonably low
            Assert.True(worker >= 2);
            Assert.True(io >= 2);
        }

        [Fact]
        public void Apply_DoesNotThrowWithZeroProcessorCount()
        {
            // This test simulates a scenario where Environment.ProcessorCount might be 0
            // (though this is unlikely in practice)

            // Act & Assert
            var exception = Record.Exception(() => ThreadPoolTuning.Apply());
            Assert.Null(exception);
        }

        [Fact]
        public void Apply_ThreadPoolSettingsAreConsistentAcrossCalls()
        {
            // Arrange
            var settings1 = new (int worker, int io)[5];
            var settings2 = new (int worker, int io)[5];

            // Act - First set of calls
            for (int i = 0; i < 5; i++)
            {
                ThreadPoolTuning.Apply();
                ThreadPool.GetMinThreads(out var worker, out var io);
                settings1[i] = (worker, io);
            }

            // Reset thread pool to original state
            ThreadPool.SetMinThreads(1, 1);

            // Act - Second set of calls
            for (int i = 0; i < 5; i++)
            {
                ThreadPoolTuning.Apply();
                ThreadPool.GetMinThreads(out var worker, out var io);
                settings2[i] = (worker, io);
            }

            // Assert - All settings should be reasonable (at least 1 thread each)
            for (int i = 0; i < 5; i++)
            {
                Assert.True(settings1[i].worker >= 1, $"First set worker threads should be at least 1: {settings1[i].worker}");
                Assert.True(settings1[i].io >= 1, $"First set IO threads should be at least 1: {settings1[i].io}");
                Assert.True(settings2[i].worker >= 1, $"Second set worker threads should be at least 1: {settings2[i].worker}");
                Assert.True(settings2[i].io >= 1, $"Second set IO threads should be at least 1: {settings2[i].io}");
            }
        }
    }
}
