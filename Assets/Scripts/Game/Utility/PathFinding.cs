using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class PathFinding
    {
        private readonly DiscretizedSpace space;
        private readonly float TTL = 15; // seconds

        public enum PathFindingResult
        {
            Success,
            Failure,
            NotCompleted
        }

        // Current query
        private float cacheTTL = 0f;
        private Vector3 start;
        private Vector3 destination; 
        private float maxLength;
        private float weight;

        // Working state
        private ChainedPathStore store;
        private PriorityQueue<ChainedPath> openList;
        private ISet<Vector3> closedList;
        private ISet<Vector3> destinationList;
        private PathFindingResult status;
        private List<Vector3> foundPath;

        readonly struct ChainedPath : IComparable<ChainedPath> 
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
            public void Clear()
            {
                paths.Clear();
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

        public PathFinding(DiscretizedSpace space)
        {
            this.space = space;
            store = new ChainedPathStore();
            openList = new PriorityQueue<ChainedPath>();
            closedList = new HashSet<Vector3>();
            destinationList = new HashSet<Vector3>();
        }

        public PathFindingResult RetryableFindShortestPath(Vector3 start, Vector3 destination, float maxLength, out List<Vector3> path, float weight = 1f)
        {
            bool isResumable = false;
            // Information too old, parameters changed or target has moved: we don't have a cached answer and can't resume computation
            if (Time.time >= cacheTTL
                || this.maxLength != maxLength
                || this.weight != weight
                || (destination - this.destination).sqrMagnitude > 4f)
            {
                // Indeed we must start over
            }
            else if (status == PathFindingResult.Success)
            {
                // Assuming we're following the path, can we return a suffix of foundPath?
                int closest = 0;
                float minSqrDist = (start - foundPath[closest]).sqrMagnitude;
                while (closest < foundPath.Count - 1)
                {
                    int nextClosest = closest + 1;
                    float nextMinSqrDist = (start - foundPath[nextClosest]).sqrMagnitude;
                    if (nextMinSqrDist >= minSqrDist)
                        break;
        
                    closest = nextClosest;
                    minSqrDist = nextMinSqrDist;
                }
                if (minSqrDist < 1f)
                {
                    path = foundPath.GetRange(closest, foundPath.Count - closest);
                    return PathFindingResult.Success;
                }
            }
            else if (status == PathFindingResult.Failure)
            {
                // Cached answer was that target was unreachable and nobody has moved much (destination checked above): probably still true
                if ((start - this.start).sqrMagnitude < 4f)
                {
                    path = null;
                    return PathFindingResult.Failure;
                }
            }
            else
            {
                // Query hasn't changed but we aren't done yet: let's continue
                isResumable = true;
            }

            if (!isResumable)
            {
                Initialization(start, destination, maxLength, weight);
            }
            FindShortestPath();
            path = foundPath;
            return status;
        }

        private void Initialization(Vector3 start, Vector3 destination, float maxLength, float weight)
        {
            this.start = start;
            this.destination = destination;
            this.maxLength = maxLength;
            this.weight = weight;
            cacheTTL = Time.time + TTL;

            store.Clear();
            openList.Clear();
            closedList.Clear();
            destinationList.Clear();
            status = PathFindingResult.NotCompleted;
            
            Vector3Int discretizedStart = space.Discretize(start);
            Vector3Int discretizedDestination = space.Discretize(destination);
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
        }

        public void FindShortestPath()
        {
            List<ChainedPath> newPathsBuffer = new List<ChainedPath>(26);
            try
            {
                while (openList.Count() > 0)
                {
                    // Don't remove from openList just yet, in case we're interrupted by OverRaycastBudgetException
                    ChainedPath Path = openList.Peek();
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
                                    foundPath = RebuildPath(space, store, Path);
                                    // Destination does not have discretized position, so it's synthetically added to the result
                                    foundPath.Add(destination);
                                    status = PathFindingResult.Success;
                                    return;
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
                                    newPathsBuffer.Add(newPath);
                                }
                            }
                        }
                        openList.Dequeue();
                        foreach (ChainedPath newPath in newPathsBuffer)
                            openList.Enqueue(newPath);
                        newPathsBuffer.Clear();
                        closedList.Add(Path.position);
                    }
                    else
                        openList.Dequeue();
                }
                foundPath = null;
                status = PathFindingResult.Failure;
            } 
            catch(OverRaycastBudgetException)
            {
                foundPath = null;
                status = PathFindingResult.NotCompleted;
            }
            return;
        }
        
    }
}