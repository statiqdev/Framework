﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Statiq.Common;

namespace Statiq.Highlight
{
    /// <summary>
    /// Adds code highlighting CSS styles.
    /// </summary>
    /// <remarks>
    /// This module pre-generates highlight.js (https://highlightjs.org) code highlighting styles.
    /// Note that a highlight.js stylesheet must still be referenced for the styles to render in different colors.
    /// </remarks>
    /// <example>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// &lt;?# highlight csharp ?&gt;
    /// public class Foo
    /// {
    ///   int Bar { get; set; }
    /// }
    /// &lt;?#/ highlight ?&gt;
    /// </code>
    /// <para>
    /// Example output:
    /// </para>
    /// <code>
    /// &lt;code class=&quot;language-csharp hljs&quot;&gt;&lt;span class=&quot;hljs-keyword&quot;&gt;public&lt;/span&gt; &lt;span class=&quot;hljs-keyword&quot;&gt;class&lt;/span&gt; &lt;span class=&quot;hljs-title&quot;&gt;Foo&lt;/span&gt;
    /// {
    ///   &lt;span class=&quot;hljs-keyword&quot;&gt;int&lt;/span&gt; Bar { &lt;span class=&quot;hljs-keyword&quot;&gt;get&lt;/span&gt;; &lt;span class=&quot;hljs-keyword&quot;&gt;set&lt;/span&gt;; }
    /// }&lt;/code&gt;
    /// </code>
    /// </example>
    /// <parameter name="Language">The highlight.js language name to highlight as (for example, "csharp").</parameter>
    /// <parameter name="Element">An element to wrap the highlighted content in. If omitted, <c>&lt;code&gt;</c> will be used.</parameter>
    /// <parameter name="HighlightJsFile">Sets the file path to a custom highlight.js file. If not set the embeded version will be used.</parameter>
    public class HighlightShortcode : SyncShortcode
    {
        /// <inheritdoc />
        public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            IMetadataDictionary dictionary = args.ToDictionary(
                "Language",
                "Element",
                "HighlightJsFile");

            HtmlParser parser = new HtmlParser();
            using (IJavaScriptEnginePool enginePool = context.GetJavaScriptEnginePool(x =>
            {
                if (dictionary.ContainsKey("HighlightJsFile"))
                {
                    x.ExecuteFile(dictionary.GetString("HighlightJsFile"));
                }
                else
                {
                    x.ExecuteResource("highlight-all.js", typeof(Statiq.Highlight.HighlightCode));
                }
            }))
            {
                AngleSharp.Dom.IDocument htmlDocument = parser.ParseDocument(string.Empty);
                AngleSharp.Dom.IElement element = htmlDocument.CreateElement(dictionary.GetString("Element", "code"));
                element.InnerHtml = content.Trim();
                if (dictionary.ContainsKey("Language"))
                {
                    element.SetAttribute("class", $"language-{dictionary.GetString("Language")}");
                }
                HighlightCode.HighlightElement(enginePool, element);
                return element.OuterHtml;
            }
        }
    }
}
