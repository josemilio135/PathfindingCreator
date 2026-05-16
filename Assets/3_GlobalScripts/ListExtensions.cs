using System.Collections.Generic;

public static class ListExtensions
{
    /// <summary>
    /// Checks if the list contains the item.
    /// Optionally removes it if found.
    /// </summary>
    /// <returns>True if the item exists in the list.
    /// </returns>
    public static bool ContainsItem<T>(this IList<T> list, T item, bool remove = false)
    {
        int index = list.IndexOf(item);

        if (index < 0)
            return false;

        if (remove)
            list.RemoveAt(index);

        return true;
    }
}