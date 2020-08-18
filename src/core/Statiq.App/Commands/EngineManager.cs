﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Cli;
using Statiq.Common;
using Statiq.Core;

namespace Statiq.App
{
    /// <summary>
    /// This class can be used from commands to wrap engine execution and apply settings, etc.
    /// </summary>
    internal class EngineManager : IEngineManager, IDisposable
    {
        private readonly ILogger _logger;

        public EngineManager(
            CommandContext commandContext,
            EngineCommandSettings commandSettings,
            IConfigurationSettings configurationSettings,
            IServiceCollection serviceCollection,
            Bootstrapper bootstrapper)
        {
            // Get the standard input stream
            string input = null;
            if (commandSettings?.StdIn == true)
            {
                using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                {
                    input = reader.ReadToEnd();
                }
            }

            // Create the application state
            ApplicationState applicationState = new ApplicationState(bootstrapper.Arguments, commandContext.Name, input);

            // Create the engine and get a logger
            // The configuration settings should not be used after this point
            ConfigurationSettings configurationSettingsImpl = configurationSettings as ConfigurationSettings;
            IDictionary<string, object> settings = configurationSettingsImpl?.Settings;
            Engine = new Engine(applicationState, serviceCollection, configurationSettings.Configuration, settings, bootstrapper.ClassCatalog);
            configurationSettingsImpl?.Dispose();

            // Get the logger from the engine and store it for use during execute
            _logger = Engine.Services.GetRequiredService<ILogger<Bootstrapper>>();

            // Apply command settings
            if (commandSettings is object)
            {
                ApplyCommandSettings(Engine, commandSettings);
            }

            // Run engine configurators after command line, settings, etc. have been applied
            bootstrapper.Configurators.Configure<IEngine>(Engine);

            // Log the full environment
            _logger.LogInformation($"Root path:{Environment.NewLine}       {Engine.FileSystem.RootPath}");
            _logger.LogInformation($"Input path(s):{Environment.NewLine}       {string.Join(Environment.NewLine + "       ", Engine.FileSystem.InputPaths)}");
            if (Engine.FileSystem.ExcludedPaths.Count > 0)
            {
                _logger.LogInformation($"Excluded path(s):{Environment.NewLine}       {string.Join(Environment.NewLine + "       ", Engine.FileSystem.ExcludedPaths)}");
            }
            _logger.LogInformation($"Output path:{Environment.NewLine}       {Engine.FileSystem.OutputPath}");
            _logger.LogInformation($"Temp path:{Environment.NewLine}       {Engine.FileSystem.TempPath}");
        }

        public Engine Engine { get; }

        public string[] Pipelines { get; set; }

        public bool NormalPipelines { get; set; } = true;

        public async Task<bool> ExecuteAsync(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                await Engine.ExecuteAsync(Pipelines, NormalPipelines, cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    _logger.LogCritical(ex.Message);
                    _logger.LogError("To get more detailed logging output run with the \"-l Debug\" flag");
                }
                return false;
            }
            return true;
        }

        public void Dispose() => Engine.Dispose();

        private static void ApplyCommandSettings(Engine engine, EngineCommandSettings commandSettings)
        {
            // Set folders
            NormalizedPath currentDirectory = Environment.CurrentDirectory;
            engine.FileSystem.RootPath = string.IsNullOrEmpty(commandSettings.RootPath)
                ? currentDirectory
                : currentDirectory.Combine(commandSettings.RootPath);
            if (commandSettings.InputPaths?.Length > 0)
            {
                // Clear existing default paths if new ones are set
                // and reverse the inputs so the last one is first to match the semantics of multiple occurrence single options
                engine.FileSystem.InputPaths.Clear();
                engine.FileSystem.InputPaths.AddRange(commandSettings.InputPaths.Select(x => new NormalizedPath(x)).Reverse());
            }
            if (!string.IsNullOrEmpty(commandSettings.OutputPath))
            {
                engine.FileSystem.OutputPath = commandSettings.OutputPath;
            }
            if (commandSettings.NoClean)
            {
                engine.Settings[Keys.CleanOutputPath] = false;
            }

            // Set no cache if requested
            if (commandSettings.NoCache)
            {
                engine.Settings[Keys.UseCache] = false;
            }

            // Set serial mode
            if (commandSettings.SerialExecution)
            {
                engine.SerialExecution = true;
            }
        }
    }
}
