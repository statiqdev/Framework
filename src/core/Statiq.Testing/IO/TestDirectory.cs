using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common;

namespace Statiq.Testing
{
    public class TestDirectory : IDirectory
    {
        private readonly IReadOnlyFileSystem _fileSystem;
        private readonly TestFileProvider _fileProvider;

        public TestDirectory(IReadOnlyFileSystem fileSystem, TestFileProvider fileProvider, in NormalizedPath path)
        {
            _fileSystem = fileSystem;
            _fileProvider = fileProvider;
            Path = path;
        }

        public NormalizedPath Path { get; }

        NormalizedPath IFileSystemEntry.Path => Path;

        public bool Exists => _fileProvider.Directories.Contains(Path);

        public DateTime LastWriteTime { get; set; }

        public DateTime CreationTime { get; set; }

        public IDirectory Parent
        {
            get
            {
                NormalizedPath parentPath = Path.Parent;
                return parentPath.IsNull ? null : _fileSystem.GetDirectory(parentPath);
            }
        }

        public void Create() => _fileProvider.Directories.Add(Path);

        public void Delete(bool recursive) => _fileProvider.Directories.Remove(Path);

        public IEnumerable<IDirectory> GetDirectories(SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return _fileProvider.Directories
                    .Where(x => Path.ContainsChild(x))
                    .Select(x => _fileSystem.GetDirectory(x));
            }
            return _fileProvider.Directories
                .Where(x => Path.ContainsDescendant(x))
                .Select(x => _fileSystem.GetDirectory(x));
        }

        public IEnumerable<IFile> GetFiles(SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return _fileProvider.Files.Keys
                    .Where(x => Path.ContainsChild(x))
                    .Select(x => _fileSystem.GetFile(x));
            }
            return _fileProvider.Files.Keys
                .Where(x => Path.ContainsDescendant(x))
                .Select(x => _fileSystem.GetFile(x));
        }

        public IDirectory GetDirectory(NormalizedPath path)
        {
            path.ThrowIfNull(nameof(path));

            if (!path.IsRelative)
            {
                throw new ArgumentException("Path must be relative", nameof(path));
            }

            return _fileSystem.GetDirectory(Path.Combine(path));
        }

        public IFile GetFile(NormalizedPath path)
        {
            path.ThrowIfNull(nameof(path));

            if (!path.IsRelative)
            {
                throw new ArgumentException("Path must be relative", nameof(path));
            }

            return _fileSystem.GetFile(Path.Combine(path));
        }

        public override string ToString() => Path.ToString();

        public string ToDisplayString() => Path.ToSafeDisplayString();
    }
}