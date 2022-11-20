\mainpage
[TOC]

# Overview

ClusteringTest is the top-level class responsible for running the benchmarks.

ClusteringTestGui provides a GUI for selecting and running benchmarks.

[ABenchmarkGenerator](#BenchmarkGeneration.ABenchmarkGenerator) is the abstract class for benchmark generators. Each benchmark contains a list of measurements (dispatches) to perform.

[BenchmarkDescription](#BenchmarkGeneration.BenchmarkDescription) represents a single benchmark.

[IDispatcher](#ClusteringAlgorithms.IDispatcher) is the interface for dispatchers. Each dispatcher implements a particular clustering algorithm (e.g. [DispatcherKM](#ClusteringAlgorithms.DispatcherKM)). The dispatcher receives a set of parameters upon creation. When clustering with different parameters is required, a new dispatcher is created.

[StopCondition](#ClusteringAlgorithms.StopCondition) is a static class which defines constant for the stoppind condition. Whenever a [dispatcher](#dispatchers) is running with the stopping condition enabled, it references [StopCondition](#ClusteringAlgorithms.StopCondition) to ensure all benchmarks use the same values.

[ClusteringRTsAndBuffers](#ClusteringAlgorithms.ClusteringRTsAndBuffers) stores all the textures and buffers needed for clustering.

## Helper Classes

ObjectPoolMaxAssert is a wrapper over UnityEngine.Pool.ObjectPool, which throws an exception if the number of objects in the pool exceeds a predetermined threshold. This helps to prevent memory leaks.

Diagnostics is a static class that provides custom diagnostics tools ([Assert](#Diagnostics.Assert), [Throw](#Diagnostics.Throw)). If the aplication is running in the editor, it throws exceptions as usual and stops the editor. If the application is running from a compiled executable, it writes the error into the file `Error.txt` and closes the application.

BenchmarkHelper is a static class, which provides [MeasureTime](#BenchmarkHelper.MeasureTime) method for speed benchmarks. [MeasureTime](#BenchmarkHelper.MeasureTime) ensures that GC is disabled while running the measured function.