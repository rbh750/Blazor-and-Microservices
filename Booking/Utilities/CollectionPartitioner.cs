namespace Booking.Utilities;

public static class CollectionPartitioner
{
    /// <summary>
    /// Evenly distribute a collection into a specified number of groups. 
    /// Useful in scenarios where data needs to be divided into chunks for parallel processing.
    /// Since the resulting groups cannot exceed the number of items specified in the maxNumberOfGroups parameter, the last item might be added to a new group.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    /// <param name="maxNumberOfGroups">The maximum number of groups into which the items in the collection will be distributed.</param>
    /// <param name="collection">A List of objects.</param>
    /// <returns>An indexed collection of collections.</returns>
    public static Dictionary<int, List<T>> PartitionIntoGroups<T>(int maxNumberOfGroups, List<T> collection)
    {
        int collectionCount = collection.Count;
        int numberOfItemsPerGroup;
        int additionalItemsForLastGroup;

        if (maxNumberOfGroups == 1)
        {
            return PartitionCollection(1, collectionCount, 0, collection);
        }
        else if (collectionCount > maxNumberOfGroups)
        {
            additionalItemsForLastGroup = collectionCount % maxNumberOfGroups;
            numberOfItemsPerGroup = collectionCount / maxNumberOfGroups;

            return PartitionCollection(maxNumberOfGroups, numberOfItemsPerGroup, additionalItemsForLastGroup, collection);
        }
        else
        {
            return PartitionCollection(collectionCount, 1, 0, collection);
        }
    }

    // Split a collection of objects into groups of equal size, except for the last group, which can contain 0 or more items.
    private static Dictionary<int, List<T>> PartitionCollection<T>(
        int numberOfGroups,
        int numberOfItemsPerGroup,
        int numberOfItemsInLastlGroup,
        List<T> collection)
    {
        Dictionary<int, List<T>> result = [];
        List<T> group;

        var additionalGroupCont = 0;

        for (int i = 0; i < numberOfGroups; i++)
        {
            if (additionalGroupCont <= numberOfItemsPerGroup)
            {
                group = [.. collection.Skip(numberOfItemsPerGroup * i).Take(numberOfItemsPerGroup)];
            }
            else
            {
                group = [.. collection.Skip(i).Take(1)];
            }
            result.Add(i, group);
            additionalGroupCont++;
        }

        if (numberOfItemsInLastlGroup > 0)
        {
            var lastProcessedIndex = numberOfGroups * numberOfItemsPerGroup;
            List<T> remainingItems = [];

            for (int i = 0; i < numberOfItemsInLastlGroup; i++)
            {
                remainingItems.Add(collection[lastProcessedIndex]);
                lastProcessedIndex++;
            }

            result.Add(additionalGroupCont, remainingItems);
        }

        return result;
    }
}
