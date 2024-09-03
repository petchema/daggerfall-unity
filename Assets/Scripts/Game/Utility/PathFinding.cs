using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public static class PathFinding
    {
        protected readonly struct ChainedPath : IComparable<ChainedPath> 
        {
            public readonly Vector3Int position;
            public readonly float cost;
            public readonly float estimatedTotalCost; // cost + heuristic cost of remaining path
            public readonly int movementIndex;
            public readonly int sourceChainedPathIndex;

            public ChainedPath(Vector3Int position, float cost, float estimatedTotalCost, int movementIndex, int sourceChainedPathIndex)
            {
                this.position = position;
                this.cost = cost;
                this.estimatedTotalCost = estimatedTotalCost;
                this.movementIndex = movementIndex;
                this.sourceChainedPathIndex = sourceChainedPathIndex;
            }

            public int CompareTo(ChainedPath otherPath)
            {
                return estimatedTotalCost.CompareTo(otherPath.estimatedTotalCost);
            }
        }

        class ChainedPathStore
        {
            List<ChainedPath> paths = new List<ChainedPath>();
            public int Add(ChainedPath path)
            {
                int index = paths.Count;
                paths.Add(path);
                return index;
            }
            public ChainedPath Get(int index)
            {
                // Does not check if that's a released element
                return paths[index];
            }
        }

        static List<Vector3> RebuildPath(DiscretizedSpace space, ChainedPathStore allocator, ChainedPath path)
        {
            List<Vector3> result = new List<Vector3>();
            while (true)
            {
                result.Add(space.Reify(path.position));
                if (path.sourceChainedPathIndex < 0)
                    break;
                path = allocator.Get(path.sourceChainedPathIndex);
            }
            // We could synthetically add the source at the beginning of the result path, but who cares?
            result.Reverse();
            return result;
        }


        public static bool FindShortestPath(DiscretizedSpace space, PathFindingContext pathFindingContext, Vector3 start, Vector3 destination, float maxLength, out List<Vector3> path, float weight = 1f)
        {

            ChainedPathStore store = new ChainedPathStore();
            PriorityQueue<ChainedPath> openList = new PriorityQueue<ChainedPath>();
            ISet<Vector3> closedList = new HashSet<Vector3>();
            ISet<Vector3> destinationList = new HashSet<Vector3>();
            Vector3Int discretizedStart = space.Discretize(start);
            Vector3Int discretizedDestination = space.Discretize(destination);
            PathFindingContext.CacheKey cacheKey = new PathFindingContext.CacheKey(discretizedStart, discretizedDestination, maxLength, weight);
            if (pathFindingContext.HasCachedShortestPath(cacheKey, out PathFindingContext.CacheValue cachedShortestPath))
            {
                path = cachedShortestPath.path;
                return cachedShortestPath.pathFound;
            }
            for (int x = 0; x <= 1; x++) 
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        // Populate openList
                        Vector3Int startPosition = new Vector3Int(discretizedStart.x + x, discretizedStart.y + y, discretizedStart.z + z);
                        ChainedPath newPath = new ChainedPath(startPosition, 0f, space.MeasuredCost(start, space.Reify(startPosition)) * weight, -1, -1);
                        openList.Enqueue(newPath);

                        // Also populate destinationList
                        Vector3Int destinationPosition = new Vector3Int(discretizedDestination.x + x, discretizedDestination.y + y, discretizedDestination.z + z);
                        destinationList.Add(destinationPosition);
                    }
                }
            }
            try
            {
                while (openList.Count() > 0)
                {
                    ChainedPath Path = openList.Dequeue();
                    int pathIndex = store.Add(Path);
                    if (!closedList.Contains(Path.position))
                    {
                        bool isNavigable = Path.sourceChainedPathIndex >= 0 
                            ? space.IsNavigableInt(store.Get(Path.sourceChainedPathIndex).position, Path.position, Path.movementIndex) 
                            // Start does not have discretized position, so it's special cased as "source index = -1"
                            : space.IsNavigable(start, space.Reify(Path.position));
                        if (isNavigable)
                        {
                            // Are we arrived yet?
                            if (destinationList.Contains(Path.position))
                            {
                                if (space.IsNavigable(space.Reify(Path.position), destination))
                                {
                                    path = RebuildPath(space, store, Path);
                                    // Destination does not have discretized position, so it's synthetically added to the result
                                    path.Add(destination);
                                    cachedShortestPath = new PathFindingContext.CacheValue(true, path);
                                    pathFindingContext.CacheShortestPath(cacheKey, cachedShortestPath);
                                    return true;
                                }
                            }

                            List<DiscretizedSpace.Movement> movements = space.GetMovements();
                            for (int i = 0; i < movements.Count - 1; i++)
                            {
                                DiscretizedSpace.Movement movement = movements[i];
                                Vector3Int newPosition = Path.position + movement.delta;
                                float newCost = Path.cost + movement.cost;
                                if (newCost <= maxLength)
                                {
                                    ChainedPath newPath = new ChainedPath(newPosition, newCost, newCost + space.HeuristicCost(space.Reify(newPosition), destination) * weight, i, pathIndex);
                                    openList.Enqueue(newPath);
                                }
                            }
                        }
                        closedList.Add(Path.position);
                    }
                }
                // Only cache result if not interrupted by raycast budget
                cachedShortestPath = new PathFindingContext.CacheValue(false, null);
                pathFindingContext.CacheShortestPath(cacheKey, cachedShortestPath);
            } 
            catch(OverRaycastBudgetException)
            {
                // do nothing, just return path couldn't be found
            }
            path = null;
            return false;
        }
        
    }
}