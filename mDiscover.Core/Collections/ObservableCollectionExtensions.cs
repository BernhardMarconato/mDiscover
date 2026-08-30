namespace mDiscover.Core.Collections;

using System.Collections.ObjectModel;

/// <summary>
/// Extension methods for <see cref="ObservableCollection{T}"/> supporting non-destructive delta synchronizations.
/// </summary>
public static class ObservableCollectionExtensions
{
    extension<T>(ObservableCollection<T> current)
    {
        /// <summary>
        /// Synchronizes an <see cref="ObservableCollection{T}"/> with a desired list using minimal fine-grained
        /// mutations (RemoveAt, Insert, Move), preserving unchanged items and enabling smooth UI animations.
        /// </summary>
        /// <param name="desired">The desired ordered sequence of items.</param>
        /// <param name="equalityComparer">Optional equality comparer for items.</param>
        /// <returns><see langword="true"/> if changes were made; otherwise, <see langword="false"/>.</returns>
        public bool SyncTo(IList<T> desired, IEqualityComparer<T>? equalityComparer = null)
        {
            ArgumentNullException.ThrowIfNull(current);
            ArgumentNullException.ThrowIfNull(desired);

            equalityComparer ??= EqualityComparer<T>.Default;
            var modified = false;

            // Quick equality check: if sequences are already identical, do nothing
            if (current.Count == desired.Count)
            {
                var identical = true;
                for (var k = 0; k < current.Count; k++)
                {
                    if (!equalityComparer.Equals(current[k], desired[k]))
                    {
                        identical = false;
                        break;
                    }
                }

                if (identical)
                    return false;
            }

            // Step 1: Remove items that no longer exist in the desired list
            var desiredSet = new HashSet<T>(desired, equalityComparer);
            for (var i = current.Count - 1; i >= 0; i--)
            {
                if (!desiredSet.Contains(current[i]))
                {
                    current.RemoveAt(i);
                    modified = true;
                }
            }

            // Step 2: Insert or reposition items to match desired list
            for (var targetIndex = 0; targetIndex < desired.Count; targetIndex++)
            {
                var item = desired[targetIndex];

                // Find item in current collection at or after targetIndex
                var currentIndex = -1;
                for (var j = 0; j < current.Count; j++)
                {
                    if (equalityComparer.Equals(current[j], item))
                    {
                        currentIndex = j;
                        break;
                    }
                }

                if (currentIndex == -1)
                {
                    // New item: Insert at exact target index (triggers Add event)
                    current.Insert(targetIndex, item);
                    modified = true;
                }
                else if (currentIndex != targetIndex)
                {
                    // Existing item moved: Move to target index (triggers Move event)
                    current.Move(currentIndex, targetIndex);
                    modified = true;
                }
                // else currentIndex == targetIndex: item is already in place, zero mutation
            }

            return modified;
        }
    }
}
