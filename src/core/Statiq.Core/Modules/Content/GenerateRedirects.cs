﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Statiq.Common;

namespace Statiq.Core
{
    /// <summary>
    /// Handles redirected content by creating pages with meta refresh tags or other redirect files.
    /// </summary>
    /// <remarks>
    /// <para>When content moves you need some way to redirect from the old location to the new location.
    /// This is especially true when moving content from one publishing system to another that might
    /// have different conventions for things like paths.</para>
    /// <para>This module lets you manage redirected content
    /// by generating special pages that contain a "meta refresh tag". This tag tells client browsers
    /// that the content has moved and to redirect to the new location. Google and other search engines
    /// also understand meta refresh tags and will adjust their search indexes accordingly.</para>
    /// <para>Alternatively (or additionally), you can also create host-specific redirect files to
    /// control redirection on the server.</para>
    /// <para>By default, this module will read the paths that need to be redirected from the
    /// <c>RedirectFrom</c> metadata key. One or more paths can be specified in this metadata and
    /// corresponding redirect files will be created for each.</para>
    /// <para>This module outputs any meta refresh pages as well as any additional redirect files
    /// you specify. It does not output the original input files.</para>
    /// </remarks>
    /// <metadata cref="Keys.RedirectFrom" usage="Input" />
    /// <category>Content</category>
    public class GenerateRedirects : Module
    {
        private readonly Dictionary<NormalizedPath, Func<IDictionary<NormalizedPath, string>, string>> _additionalOutputs =
            new Dictionary<NormalizedPath, Func<IDictionary<NormalizedPath, string>, string>>();

        private Config<IReadOnlyList<NormalizedPath>> _paths = Config.FromDocument<IReadOnlyList<NormalizedPath>>(Keys.RedirectFrom);
        private bool _metaRefreshPages = true;
        private bool _includeHost = false;

        /// <summary>
        /// Controls where the redirected paths come from. By default, values from the metadata
        /// key <c>RedirectFrom</c> are used.
        /// </summary>
        /// <param name="paths">A delegate that should return one or more <see cref="NormalizedPath"/>.</param>
        /// <returns>The current module instance.</returns>
        public GenerateRedirects WithPaths(Config<IReadOnlyList<NormalizedPath>> paths)
        {
            _paths = paths.ThrowIfNull(nameof(paths));
            return this;
        }

        /// <summary>
        /// Controls whether meta refresh pages are output.
        /// </summary>
        /// <param name="metaRefreshPages">If <c>true</c>, meta refresh pages are generated.</param>
        /// <returns>The current module instance.</returns>
        public GenerateRedirects WithMetaRefreshPages(bool metaRefreshPages = true)
        {
            _metaRefreshPages = metaRefreshPages;
            return this;
        }

        /// <summary>
        /// Indicates whether the host should be automatically included in generated redirect links.
        /// </summary>
        /// <param name="includeHost"><c>true</c> to include the host.</param>
        /// <returns>The current module instance.</returns>
        public GenerateRedirects IncludeHost(bool includeHost = true)
        {
            _includeHost = includeHost;
            return this;
        }

        /// <summary>
        /// Adds additional output files that you specify by supplying a delegate that takes a dictionary
        /// of redirected paths to destination URLs.
        /// </summary>
        /// <param name="path">The path of the output file (must be relative).</param>
        /// <param name="content">A delegate that takes a dictionary with keys equal to each redirected file
        /// and values equal to the destination URL. The delegate should return the content of the output file.</param>
        /// <returns>The current module instance.</returns>
        public GenerateRedirects WithAdditionalOutput(in NormalizedPath path, Func<IDictionary<NormalizedPath, string>, string> content)
        {
            path.ThrowIfNull(nameof(path));
            content.ThrowIfNull(nameof(content));
            if (!path.IsRelative)
            {
                throw new ArgumentException("The output path must be relative");
            }
            _additionalOutputs.Add(path, content);
            return this;
        }

        /// <inheritdoc />
        protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
        {
            // Iterate redirects and generate all of the per-redirect documents (I.e., meta refresh pages)
            ConcurrentDictionary<NormalizedPath, string> redirects = new ConcurrentDictionary<NormalizedPath, string>();

            // Need to materialize the parallel operation before creating the additional outputs
            List<IDocument> outputs = new List<IDocument>();
            foreach (IDocument input in context.Inputs)
            {
                await foreach (IDocument output in GetOutputsAsync(input))
                {
                    outputs.Add(output);
                }
            }

            // Generate other output documents if requested
            if (redirects.Count > 0)
            {
                foreach (KeyValuePair<NormalizedPath, Func<IDictionary<NormalizedPath, string>, string>> additionalOutput in _additionalOutputs)
                {
                    string content = additionalOutput.Value(redirects);
                    if (!string.IsNullOrEmpty(content))
                    {
                        outputs.Add(
                            context.CreateDocument(
                                additionalOutput.Key,
                                await context.GetContentProviderAsync(content)));
                    }
                }
            }

            return outputs;

            async IAsyncEnumerable<IDocument> GetOutputsAsync(IDocument input)
            {
                IReadOnlyList<NormalizedPath> paths = await _paths.GetValueAsync(input, context);
                if (paths != null)
                {
                    foreach (NormalizedPath fromPath in paths.Where(x => !x.IsNull))
                    {
                        // Make sure it's a relative path
                        if (!fromPath.IsRelative)
                        {
                            context.LogWarning($"The redirect path must be relative for document {input.Source.ToSafeDisplayString()}: {fromPath}");
                            continue;
                        }

                        // Record the redirect for additional processing
                        string url = context.GetLink(input, _includeHost);
                        redirects.TryAdd(fromPath, url);

                        // Meta refresh documents
                        NormalizedPath outputPath = fromPath;
                        if (!string.Equals(outputPath.Extension, ".html", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(outputPath.Extension, ".htm", StringComparison.OrdinalIgnoreCase))
                        {
                            outputPath = outputPath.AppendExtension(".html");
                        }

                        if (_metaRefreshPages)
                        {
                            yield return context.CreateDocument(
                                outputPath,
                                await context.GetContentProviderAsync(
                                    $@"
<!doctype html>
<html>    
  <head>      
    <title>Redirected</title>      
    <meta http-equiv=""refresh"" content=""0;url='{url}'"" />    
  </head>    
  <body> 
    <p>This page has moved to a <a href=""{url}"">{url}</a></p> 
  </body>  
</html>",
                                    MediaTypes.Html));
                        }
                    }
                }
            }
        }
    }
}
