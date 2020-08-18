﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Extensions;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.Extensions.Logging;
using Statiq.Common;

namespace Statiq.Highlight
{
    /// <summary>
    /// Applies syntax highlighting to code blocks
    /// </summary>
    /// <remarks>
    /// <para>
    /// This module finds all &lt;pre&gt; &lt;code&gt; blocks and applies HighlightJs's syntax highlighting.
    /// </para>
    /// <para>
    /// Note that because this module parses the document
    /// content as standards-compliant HTML and outputs the formatted post-parsed DOM, you should
    /// only place this module after all other template processing has been performed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Pipelines.Add("Highlight",
    ///     ReadFiles("*.html"),
    ///     Highlight(),
    ///     WriteFiles(".html")
    /// );
    /// </code>
    /// </example>
    /// <category>Content</category>
    public class HighlightCode : Module
    {
        private string _codeQuerySelector = "pre code";
        private string _highlightJsFile;
        private bool _warnOnMissingLanguage = true;

        /// <summary>
        /// Sets the query selector to use to find code blocks.
        /// </summary>
        /// <param name="querySelector">The query selector to use to select code blocks. The default value is pre code</param>
        /// <returns>The current instance.</returns>
        public HighlightCode WithCodeQuerySelector(string querySelector)
        {
            _codeQuerySelector = querySelector;
            return this;
        }

        /// <summary>
        /// Sets whether a warning should be raised if a missing language is detected in a code block.
        /// </summary>
        /// <param name="warnOnMissingLanguage">if set to <c>true</c> [warn on missing].</param>
        /// <returns>The current instance.</returns>
        public HighlightCode WithMissingLanguageWarning(bool warnOnMissingLanguage = true)
        {
            _warnOnMissingLanguage = warnOnMissingLanguage;
            return this;
        }

        /// <summary>
        /// Sets the file path to a custom highlight.js file. If not set the embeded version will be used.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The current instance.</returns>
        public HighlightCode WithCustomHighlightJs(string filePath)
        {
            _highlightJsFile = filePath;
            return this;
        }

        /// <inheritdoc />
        protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
        {
            HtmlParser parser = new HtmlParser();
            IJavaScriptEnginePool enginePool = context.GetJavaScriptEnginePool(x =>
            {
                if (string.IsNullOrWhiteSpace(_highlightJsFile))
                {
                    x.ExecuteResource("highlight-all.js", typeof(HighlightCode));
                }
                else
                {
                    x.ExecuteFile(_highlightJsFile);
                }
            });
            using (enginePool)
            {
                IEnumerable<IDocument> results = await context.Inputs.ParallelSelectAsync(async input =>
                {
                    try
                    {
                        using (Stream stream = input.GetContentStream())
                        {
                            using (IHtmlDocument htmlDocument = await parser.ParseAsync(stream))
                            {
                                foreach (AngleSharp.Dom.IElement element in htmlDocument.QuerySelectorAll(_codeQuerySelector))
                                {
                                    // Don't highlight anything that potentially is already highlighted
                                    if (element.ClassList.Contains("hljs"))
                                    {
                                        continue;
                                    }

                                    try
                                    {
                                        HighlightElement(enginePool, element);
                                    }
                                    catch (Exception innerEx)
                                    {
                                        if (innerEx.Message.Contains("Unknown language: ") && _warnOnMissingLanguage)
                                        {
                                            context.LogWarning($"Exception while highlighting source code: {innerEx.Message}");
                                        }
                                        else
                                        {
                                            context.LogInformation($"Exception while highlighting source code: {innerEx.Message}");
                                        }
                                    }
                                }

                                using (Stream contentStream = await context.GetContentStreamAsync())
                                {
                                    using (StreamWriter writer = contentStream.GetWriter())
                                    {
                                        htmlDocument.ToHtml(writer, HtmlMarkupFormatter.Instance);
                                        writer.Flush();
                                        return input.Clone(context.GetContentProvider(contentStream, MediaTypes.Html));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        context.LogWarning("Exception while highlighting source code for {0}: {1}", input.ToSafeDisplayString(), ex.Message);
                        return input;
                    }
                });

                // Materialize the results before disposing the JS engine
                return results.ToList();
            }
        }

        internal static void HighlightElement(IJavaScriptEnginePool enginePool, AngleSharp.Dom.IElement element)
        {
            using (IJavaScriptEngine engine = enginePool.GetEngine())
            {
                // Make sure to use TextContent, otherwise you'll get escaped html which highlight.js won't parse
                engine.SetVariableValue("input", element.TextContent);

                // Check if they specified a language in their code block
                string language = element.ClassList.FirstOrDefault(i => i.StartsWith("language"));
                if (language is object)
                {
                    engine.SetVariableValue("language", language.Replace("language-", string.Empty));
                    engine.Execute("result = hljs.highlight(language, input)");
                }
                else
                {
                    language = "(auto)"; // set this to auto in case there is an exception below
                    engine.Execute("result = hljs.highlightAuto(input)");
                    string detectedLanguage = engine.Evaluate<string>("result.language");
                    if (!string.IsNullOrWhiteSpace(detectedLanguage))
                    {
                        element.ClassList.Add("language-" + detectedLanguage);
                    }
                }

                element.ClassList.Add("hljs");
                element.InnerHtml = engine.Evaluate<string>("result.value");
            }
        }
    }
}