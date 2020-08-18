﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using Statiq.Common;
using Statiq.Testing;

namespace Statiq.Core.Tests.Execution
{
    [TestFixture]
    public class EngineFixture : BaseFixture
    {
        public class GetExecutingPipelines : EngineFixture
        {
            [Test]
            public void NullPipelinesAndNoNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(null, false);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "F" }, true);
            }

            [Test]
            public void NullPipelinesNormalAndNotDeployment()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(null, true);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "D", "E", "F", "H" }, true);
            }

            [Test]
            public void ZeroLengthAndNoNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(Array.Empty<string>(), false);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "F" }, true);
            }

            [Test]
            public void ZeroLengthAndNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(Array.Empty<string>(), true);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "D", "E", "F", "H" }, true);
            }

            [Test]
            public void SpecifiedAndNoNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(new[] { "A", "B" }, false);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "B", "F" }, true);
            }

            [Test]
            public void SpecifiedAndNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(new[] { "A", "B" }, true);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "B", "E", "D", "F", "H" }, true);
            }

            [Test]
            public void SpecifiedAndTransitiveAndNoNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(new[] { "E" }, false);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "D", "E", "F" }, true);
            }

            [Test]
            public void SpecifiedAndTransitiveAndNormal()
            {
                // Given
                Engine engine = GetEngine();

                // When
                IReadOnlyPipelineCollection executingPipelines = engine.GetExecutingPipelines(new[] { "E" }, true);

                // Then
                executingPipelines.Keys.ShouldBe(new[] { "A", "D", "E", "F", "H" }, true);
            }

            [Test]
            public void ThrowsForUndefinedPipeline()
            {
                // Given
                Engine engine = GetEngine();

                // When, Then
                Should.Throw<PipelineException>(() => engine.GetExecutingPipelines(new[] { "Z" }, false));
            }

            private Engine GetEngine()
            {
                Engine engine = new Engine();
                engine.Pipelines.Add("A", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Normal
                });
                engine.Pipelines.Add("B", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Manual
                });
                engine.Pipelines.Add("C", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Manual
                });
                engine.Pipelines.Add("D", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Manual,
                    Dependencies = new HashSet<string>(new[] { "A" }),
                    DependencyOf = new HashSet<string>(new[] { "E" })
                });
                engine.Pipelines.Add("E", new TestPipeline());
                engine.Pipelines.Add("F", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Always
                });
                engine.Pipelines.Add("G", new TestPipeline
                {
                    Deployment = true
                });
                engine.Pipelines.Add("H", new TestPipeline
                {
                    Deployment = true,
                    ExecutionPolicy = ExecutionPolicy.Normal
                });
                return engine;
            }
        }

        public class GetPipelinePhasesTests : EngineFixture
        {
            [Test]
            public void ThrowsForIsolatedPipelineWithDependencies()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Bar");
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Bar" }),
                    Isolated = true
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When, Then
                Should.Throw<PipelineException>(() => Engine.GetPipelinePhases(pipelines, logger));
            }

            [Test]
            public void ThrowsForMissingDependency()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Bar" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When, Then
                Should.Throw<PipelineException>(() => Engine.GetPipelinePhases(pipelines, logger));
            }

            [Test]
            public void ThrowsForSelfDependency()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Foo" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When, Then
                Should.Throw<PipelineException>(() => Engine.GetPipelinePhases(pipelines, logger));
            }

            [Test]
            public void ThrowsForCyclicDependency()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Baz", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Foo" })
                });
                pipelines.Add("Bar", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Baz" })
                });
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Bar" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When, Then
                Should.Throw<PipelineException>(() => Engine.GetPipelinePhases(pipelines, logger));
            }

            [Test]
            public void DoesNotThrowForManualDependency()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Bar", new TestPipeline
                {
                    ExecutionPolicy = ExecutionPolicy.Manual
                });
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Bar" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When
                PipelinePhase[] phases = Engine.GetPipelinePhases(pipelines, logger);

                // Then
                phases.Select(x => (x.PipelineName, x.Phase)).ShouldBe(new (string, Phase)[]
                {
                    ("Bar", Phase.Input),
                    ("Bar", Phase.Process),
                    ("Foo", Phase.Input),
                    ("Foo", Phase.Process),
                    ("Bar", Phase.PostProcess),
                    ("Bar", Phase.Output),
                    ("Foo", Phase.PostProcess),
                    ("Foo", Phase.Output)
                });
            }

            [Test]
            public void DeploymentOutputPhaseComesAfterOthers()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Bar", new TestPipeline
                {
                    Deployment = true,
                    ExecutionPolicy = ExecutionPolicy.Manual
                });
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "Bar" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When
                PipelinePhase[] phases = Engine.GetPipelinePhases(pipelines, logger);

                // Then
                phases.Select(x => (x.PipelineName, x.Phase)).ShouldBe(new (string, Phase)[]
                {
                    ("Bar", Phase.Input),
                    ("Bar", Phase.Process),
                    ("Foo", Phase.Input),
                    ("Foo", Phase.Process),
                    ("Bar", Phase.PostProcess),
                    ("Foo", Phase.PostProcess),
                    ("Foo", Phase.Output),
                    ("Bar", Phase.Output),
                });
            }

            [Test]
            public void DependenciesAreCaseInsensitive()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Bar");
                pipelines.Add("Foo", new TestPipeline
                {
                    Dependencies = new HashSet<string>(new[] { "bar" })
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When
                PipelinePhase[] phases = Engine.GetPipelinePhases(pipelines, logger);

                // Then
                phases.Select(x => (x.PipelineName, x.Phase)).ShouldBe(new (string, Phase)[]
                {
                    ("Bar", Phase.Input),
                    ("Bar", Phase.Process),
                    ("Foo", Phase.Input),
                    ("Foo", Phase.Process),
                    ("Bar", Phase.PostProcess),
                    ("Bar", Phase.Output),
                    ("Foo", Phase.PostProcess),
                    ("Foo", Phase.Output)
                });
            }

            [Test]
            public void DeploymentPipelinesDependOnOutputPhases()
            {
                // Given
                IPipelineCollection pipelines = new TestPipelineCollection();
                pipelines.Add("Bar", new TestPipeline
                {
                    Deployment = true
                });
                pipelines.Add("Foo", new TestPipeline
                {
                });
                ILogger logger = new TestLoggerProvider().CreateLogger(null);

                // When
                PipelinePhase[] phases = Engine.GetPipelinePhases(pipelines, logger);

                // Then
                phases
                    .Single(x => x.Pipeline.Deployment && x.Phase == Phase.Output)
                    .Dependencies
                    .Select(x => (x.PipelineName, x.Phase))
                    .ShouldBe(new (string, Phase)[]
                    {
                        ("Bar", Phase.PostProcess),
                        ("Foo", Phase.Output)
                    });
                phases
                    .Single(x => !x.Pipeline.Deployment && x.Phase == Phase.Output)
                    .Dependencies
                    .Select(x => (x.PipelineName, x.Phase))
                    .ShouldBe(new (string, Phase)[]
                    {
                        ("Foo", Phase.PostProcess)
                    });
            }
        }

        public class GetServiceTests : EngineFixture
        {
            [Test]
            public void GetsEngineService()
            {
                // Given
                Engine engine = new Engine();

                // When
                IReadOnlyFileSystem fileSystem = engine.Services.GetRequiredService<IReadOnlyFileSystem>();

                // Then
                fileSystem.ShouldBe(engine.FileSystem);
            }

            [Test]
            public void GetsExternalService()
            {
                // Given
                TestFileProvider testFileProvider = new TestFileProvider();
                ServiceCollection serviceCollection = new ServiceCollection();
                serviceCollection.AddSingleton<IFileProvider>(testFileProvider);
                Engine engine = new Engine(serviceCollection);

                // When
                IFileProvider fileProvider = engine.Services.GetRequiredService<IFileProvider>();

                // Then
                fileProvider.ShouldBe(testFileProvider);
            }

            [Test]
            public void GetsEngineServiceInNestedScope()
            {
                // Given
                Engine engine = new Engine();
                IServiceScopeFactory serviceScopeFactory = engine.Services.GetRequiredService<IServiceScopeFactory>();
                IServiceScope serviceScope = serviceScopeFactory.CreateScope();

                // When
                IReadOnlyFileSystem fileSystem = serviceScope.ServiceProvider.GetRequiredService<IReadOnlyFileSystem>();

                // Then
                fileSystem.ShouldBe(engine.FileSystem);
            }
        }

        public class ExecuteTests : EngineFixture
        {
            [Test]
            public async Task ExecutesModule()
            {
                // Given
                Engine engine = new Engine();
                IPipeline pipeline = engine.Pipelines.Add("TestPipeline");
                CountModule module = new CountModule("Foo")
                {
                    EnsureInputDocument = true
                };
                pipeline.ProcessModules.Add(module);
                CancellationTokenSource cts = new CancellationTokenSource();

                // When
                IPipelineOutputs outputs = await engine.ExecuteAsync(cts.Token);

                // Then
                module.ExecuteCount.ShouldBe(1);
                outputs.FromPipeline("TestPipeline").Select(x => x.GetInt("Foo")).ShouldBe(new int[] { 1 });
            }

            [Test]
            public async Task BeforeModuleEventOverriddesOutputs()
            {
                // Given
                Engine engine = new Engine();
                IPipeline pipeline = engine.Pipelines.Add("TestPipeline");
                CountModule module = new CountModule("Foo")
                {
                    EnsureInputDocument = true
                };
                pipeline.ProcessModules.Add(module);
                CancellationTokenSource cts = new CancellationTokenSource();
                engine.Events.Subscribe<BeforeModuleExecution>(x => x.OverrideOutputs(new TestDocument()
                {
                    { "Foo", 123 }
                }.Yield()));

                // When
                IPipelineOutputs outputs = await engine.ExecuteAsync(cts.Token);

                // Then
                module.ExecuteCount.ShouldBe(0);
                outputs.FromPipeline("TestPipeline").Select(x => x.GetInt("Foo")).ShouldBe(new int[] { 123 });
            }

            [Test]
            public async Task AfterModuleEventOverriddesOutputs()
            {
                // Given
                Engine engine = new Engine();
                IPipeline pipeline = engine.Pipelines.Add("TestPipeline");
                CountModule module = new CountModule("Foo")
                {
                    EnsureInputDocument = true
                };
                pipeline.ProcessModules.Add(module);
                CancellationTokenSource cts = new CancellationTokenSource();
                engine.Events.Subscribe<AfterModuleExecution>(x => x.OverrideOutputs(new TestDocument()
                {
                    { "Foo", x.Outputs[0].GetInt("Foo") + 123 }
                }.Yield()));

                // When
                IPipelineOutputs outputs = await engine.ExecuteAsync(cts.Token);

                // Then
                module.ExecuteCount.ShouldBe(1);
                outputs.FromPipeline("TestPipeline").Select(x => x.GetInt("Foo")).ShouldBe(new int[] { 124 });
            }
        }
    }
}
