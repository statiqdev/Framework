﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Statiq.Common
{
    /// <summary>
    /// Extension methods for collection types.
    /// </summary>
    public static class ICollectionExtensions
    {
        /// <summary>
        /// Adds a range of values to a collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection to add values to.</param>
        /// <param name="items">The items to add.</param>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            collection.ThrowIfNull(nameof(collection));

            if (items != null)
            {
                foreach (T item in items)
                {
                    collection.Add(item);
                }
            }
        }

        /// <summary>
        /// Adds a range of values to a collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection to add values to.</param>
        /// <param name="items">The items to add.</param>
        public static void AddRange<T>(this ICollection<T> collection, params T[] items) =>
            collection.AddRange(items as IEnumerable<T>);

        /// <summary>
        /// Removes all items that match a predicate from a collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection items.</typeparam>
        /// <param name="collection">The collection to remove items from.</param>
        /// <param name="match">The predicate (return <c>true</c> to remove the item).</param>
        /// <returns>The number of items removed.</returns>
        public static int RemoveAll<T>(this ICollection<T> collection, Func<T, bool> match)
        {
            collection.ThrowIfNull(nameof(collection));

            IList<T> toRemove = collection.Where(match).ToList();
            foreach (T item in toRemove)
            {
                collection.Remove(item);
            }
            return toRemove.Count;
        }
    }
}
