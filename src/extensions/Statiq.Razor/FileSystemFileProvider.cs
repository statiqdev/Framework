﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Statiq.Common;
using IFileProvider = Microsoft.Extensions.FileProviders.IFileProvider;

namespace Statiq.Razor
{
    /// <summary>
    /// Looks up files using the Statiq virtual file system.
    /// </summary>
    internal class FileSystemFileProvider : IFileProvider
    {
        private readonly ConcurrentBag<ExecutionChangeToken> _executionChangeTokens =
            new ConcurrentBag<ExecutionChangeToken>();
        private readonly ConcurrentDictionary<string, object> _reportedViewStartOverrides =
            new ConcurrentDictionary<string, object>();

        public IReadOnlyFileSystem StatiqFileSystem { get; }

        public FileSystemFileProvider(IReadOnlyFileSystem fileSystem)
        {
            StatiqFileSystem = fileSystem.ThrowIfNull(nameof(fileSystem));
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
            {
                return new NotFoundFileInfo(subpath);
            }

            string inputPath = subpath.TrimStart('/');
            if (inputPath.Contains(RenderRazor.ViewStartPlaceholder))
            {
                // Display an informational message that an existing view start file is being ignored due to layout override
                int start = inputPath.IndexOf(RenderRazor.ViewStartPlaceholder) + 1;
                int length = inputPath.IndexOf(RenderRazor.ViewStartPlaceholder, start);
                string requestingPath = inputPath[start..length];
                inputPath = inputPath.Remove(start - 1, length + 1).Insert(start - 1, "_ViewStart.cshtml");
                if (StatiqFileSystem.GetInputFile(inputPath).Exists
                    && _reportedViewStartOverrides.TryAdd(requestingPath, null))
                {
                    IExecutionContext.CurrentOrNull?.LogInformation($"Existing {inputPath} file for {requestingPath} being ignored in favor of explicit layout metadata");
                }
                return new NotFoundFileInfo(subpath);
            }
            IFile file = StatiqFileSystem.GetInputFile(inputPath);
            return !file.Exists ? new NotFoundFileInfo(subpath) : (IFileInfo)new StatiqFileInfo(file, inputPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null)
            {
                return new NotFoundDirectoryContents();
            }

            IDirectory directory = StatiqFileSystem.GetInputDirectory(subpath);
            if (!directory.Exists)
            {
                return new NotFoundDirectoryContents();
            }

            List<IFileInfo> fileInfos = new List<IFileInfo>();
            fileInfos.AddRange(directory.GetDirectories().Select(x => new StatiqDirectoryInfo(x, subpath)));
            fileInfos.AddRange(directory.GetFiles().Select(x => new StatiqFileInfo(x, directory.Path.GetRelativePath(x.Path).FullPath)));
            return new EnumerableDirectoryContents(fileInfos);
        }

        IChangeToken IFileProvider.Watch(string filter)
        {
            ExecutionChangeToken token = new ExecutionChangeToken();
            _executionChangeTokens.Add(token);
            return token;
        }

        public void ExpireChangeTokens()
        {
            ExecutionChangeToken token;
            while (_executionChangeTokens.TryTake(out token))
            {
                token.Expire();
            }
        }

        private class EnumerableDirectoryContents : IDirectoryContents
        {
            private readonly IEnumerable<IFileInfo> _entries;

            public bool Exists => true;

            public EnumerableDirectoryContents(IEnumerable<IFileInfo> entries)
            {
                _entries = entries;
            }

            public IEnumerator<IFileInfo> GetEnumerator() => _entries.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();
        }

        private class StatiqFileInfo : IFileInfo
        {
            private readonly IFile _file;

            // Pass the path in separately to return the correct input-relative path
            public StatiqFileInfo(IFile file, string path)
            {
                _file = file;
                PhysicalPath = path;
            }

            public bool Exists => _file.Exists;

            public long Length => _file.Length;

            public string PhysicalPath { get; }

            public string Name => _file.Path.FileName.FullPath;

            public DateTimeOffset LastModified => DateTimeOffset.Now;

            public bool IsDirectory => false;

            public Stream CreateReadStream() => _file.OpenRead();
        }

        private class StatiqDirectoryInfo : IFileInfo
        {
            private readonly IDirectory _directory;

            // Pass the path in separately to return the correct input-relative path
            public StatiqDirectoryInfo(IDirectory directory, string path)
            {
                _directory = directory;
                PhysicalPath = path;
            }

            public bool Exists => _directory.Exists;

            public long Length => -1L;

            public string PhysicalPath { get; }

            public string Name => _directory.Path.Name;

            public DateTimeOffset LastModified => DateTimeOffset.Now;

            public bool IsDirectory => true;

            public Stream CreateReadStream()
            {
                throw new InvalidOperationException("Cannot create a stream for a directory.");
            }
        }
    }
}
