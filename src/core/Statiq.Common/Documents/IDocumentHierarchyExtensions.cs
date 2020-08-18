﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Statiq.Common;

namespace Statiq.Common
{
    public static class IDocumentHierarchyExtensions
    {
        /// <summary>
        /// Gets the parent document of the given document from the current execution context.
        /// </summary>
        /// <param name="document">The document.</param>
        public static IDocument GetParent(this IDocument document) => document.GetParent(IExecutionContext.Current.Inputs);

        /// <summary>
        /// Gets the first document from a sequence of documents that contains the given document as one of it's children.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="parents">The potential parent documents.</param>
        /// <param name="recursive">If <c>true</c> will recursively descend the candidate parent documents looking for a parent.</param>
        /// <param name="key">The metadata key containing child documents.</param>
        /// <returns>The first document from <paramref name="parents"/> that contains the current document or <c>null</c>.</returns>
        public static IDocument GetParent(this IDocument document, IEnumerable<IDocument> parents, bool recursive = true, string key = Keys.Children)
        {
            parents.ThrowIfNull(nameof(parents));
            key.ThrowIfNull(nameof(key));

            IDocument parent = parents.FirstOrDefault(x => x.GetChildren(key).Contains(document));
            if (parent == null && recursive)
            {
                foreach (IDocument candidate in parents)
                {
                    parent = document.GetParent(candidate.GetChildren(key), true, key);
                    if (parent != null)
                    {
                        break;
                    }
                }
            }
            return parent;
        }

        /// <summary>
        /// Returns if the document has any child documents.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="key">The metadata key containing child documents.</param>
        /// <returns><c>true</c> if the document contains child documents, <c>false</c> otherwise.</returns>
        public static bool HasChildren(this IDocument document, string key = Keys.Children) =>
            document.GetDocumentList(key.ThrowIfNull(nameof(key)))?.Count > 0;

        /// <summary>
        /// Gets the child documents of the current document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="key">The metadata key containing child documents.</param>
        /// <returns>The child documents.</returns>
        public static DocumentList<IDocument> GetChildren(this IDocument document, string key = Keys.Children) =>
            document.GetDocumentList(key.ThrowIfNull(nameof(key))).ToDocumentList();

        /// <summary>
        /// Gets the descendant documents of the current document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="key">The metadata key containing child documents.</param>
        /// <returns>The descendant documents.</returns>
        public static DocumentList<IDocument> GetDescendants(this IDocument document, string key = Keys.Children) => GetDescendants(document, false, key);

        /// <summary>
        /// Gets the descendant documents of the current document and the current document.
        /// </summary>
        /// <remarks>
        /// The current document will be the first one in the result array.
        /// </remarks>
        /// <param name="document">The document.</param>
        /// <param name="key">The metadata key containing child documents.</param>
        /// <returns>The descendant documents.</returns>
        public static DocumentList<IDocument> GetDescendantsAndSelf(this IDocument document, string key = Keys.Children) => GetDescendants(document, true, key);

        private static DocumentList<IDocument> GetDescendants(IDocument document, in bool self, string key = Keys.Children)
        {
            key.ThrowIfNull(nameof(key));

            ImmutableArray<IDocument>.Builder builder = ImmutableArray.CreateBuilder<IDocument>();

            // Use a stack so we don't overflow the call stack with recursive calls for deep trees
            Stack<IDocument> stack = new Stack<IDocument>();
            stack.Push(document);
            if (self)
            {
                builder.Add(document);
            }

            // Depth-first iterate children
            while (stack.Count > 0)
            {
                foreach (IDocument child in stack.Pop().GetChildren(key))
                {
                    stack.Push(child);
                    builder.Add(child);
                }
            }

            return builder.ToImmutable().ToDocumentList();
        }

        /// <summary>
        /// Flattens a tree structure.
        /// </summary>
        /// <remarks>
        /// This extension will either get all descendants of all documents from
        /// a given metadata key (<see cref="Keys.Children"/> by default) or all
        /// descendants from all metadata if a <c>null</c> key is specified. The
        /// result also includes the initial documents in both cases.
        /// </remarks>
        /// <remarks>
        /// The documents will be returned in no particular order and only distinct
        /// documents will be returned (I.e., if a document exists as a
        /// child of more than one parent, it will only appear once in the result set).
        /// </remarks>
        /// <param name="documents">The documents to flatten.</param>
        /// <param name="childrenKey">The metadata key that contains the children or <c>null</c> to flatten documents in all metadata keys.</param>
        /// <returns>The flattened documents.</returns>
        public static DocumentList<IDocument> Flatten(this IEnumerable<IDocument> documents, string childrenKey = Keys.Children) =>
            documents.Flatten(false, childrenKey);

        /// <summary>
        /// Flattens a tree structure.
        /// </summary>
        /// <remarks>
        /// This extension will either get all descendants of all documents from
        /// a given metadata key (<see cref="Keys.Children"/> by default) or all
        /// descendants from all metadata if a <c>null</c> key is specified. The
        /// result also includes the initial documents in both cases.
        /// </remarks>
        /// <remarks>
        /// The documents will be returned in no particular order and only distinct
        /// documents will be returned (I.e., if a document exists as a
        /// child of more than one parent, it will only appear once in the result set).
        /// </remarks>
        /// <param name="documents">The documents to flatten.</param>
        /// <param name="removeTreePlaceholders"><c>true</c> to filter out documents with the <see cref="Keys.TreePlaceholder"/> metadata.</param>
        /// <param name="childrenKey">The metadata key that contains the children or <c>null</c> to flatten documents in all metadata keys.</param>
        /// <returns>The flattened documents.</returns>
        public static DocumentList<IDocument> Flatten(this IEnumerable<IDocument> documents, bool removeTreePlaceholders, string childrenKey = Keys.Children) =>
            documents.Flatten(removeTreePlaceholders ? Keys.TreePlaceholder : null, childrenKey);

        /// <summary>
        /// Flattens a tree structure.
        /// </summary>
        /// <remarks>
        /// This extension will either get all descendants of all documents from
        /// a given metadata key (<see cref="Keys.Children"/> by default) or all
        /// descendants from all metadata if a <c>null</c> key is specified. The
        /// result also includes the initial documents in both cases.
        /// </remarks>
        /// <remarks>
        /// The documents will be returned in no particular order and only distinct
        /// documents will be returned (I.e., if a document exists as a
        /// child of more than one parent, it will only appear once in the result set).
        /// </remarks>
        /// <param name="documents">The documents to flatten.</param>
        /// <param name="treePlaceholderKey">
        /// The metadata key that identifies placeholder documents (<see cref="Keys.TreePlaceholder"/> by default).
        /// If <c>null</c>, tree placeholders will not be removed.
        /// </param>
        /// <param name="childrenKey">The metadata key that contains the children or <c>null</c> to flatten documents in all metadata keys.</param>
        /// <returns>The flattened documents.</returns>
        public static DocumentList<IDocument> Flatten(this IEnumerable<IDocument> documents, string treePlaceholderKey, string childrenKey = Keys.Children)
        {
            documents.ThrowIfNull(nameof(documents));

            // Use a stack so we don't overflow the call stack with recursive calls for deep trees
            Stack<IDocument> stack = new Stack<IDocument>(documents);
            HashSet<IDocument> results = new HashSet<IDocument>();
            while (stack.Count > 0)
            {
                IDocument current = stack.Pop();

                // Only process if we haven't already processed this document
                if (results.Add(current))
                {
                    IEnumerable<IDocument> children = childrenKey == null
                        ? current.SelectMany(x => current.GetDocumentList(x.Key))
                        : current.GetDocumentList(childrenKey);
                    if (children != null)
                    {
                        foreach (IDocument child in children.Where(x => x != null))
                        {
                            stack.Push(child);
                        }
                    }
                }
            }
            return treePlaceholderKey == null
                ? results.ToDocumentList()
                : results.RemoveTreePlaceholders(treePlaceholderKey).ToDocumentList();
        }

        /// <summary>
        /// Removes tree placeholder documents (this method will not flatten a tree).
        /// </summary>
        /// <param name="documents">
        /// The documents from which to remove the placeholder documents.
        /// </param>
        /// <param name="treePlaceholderKey">
        /// The metadata key that identifies placeholder documents (<see cref="Keys.TreePlaceholder"/> by default).
        /// </param>
        /// <returns>The documents without placeholder documents.</returns>
        public static IEnumerable<IDocument> RemoveTreePlaceholders(this IEnumerable<IDocument> documents, string treePlaceholderKey = Keys.TreePlaceholder) =>
            documents.Where(x => !x.GetBool(treePlaceholderKey));
    }
}
