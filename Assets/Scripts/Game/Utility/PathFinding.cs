using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class PathFinding
    {
        private readonly DiscretizedNavigableSpace space;
        private readonly float TTL = 15f; // seconds

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
        private bool inProgress = false;
        private ChainedPathStore store;
        private PriorityQueue<ChainedPath> openList;
        private DiscretizedSearchSpace spaceState;
        private PathFindingResult status;
        private List<ResultChainedPath> foundPath;

        // Used by A*
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

        // Used for API result
        public readonly struct ResultChainedPath
        {
            public readonly Vector3 position;
            public readonly float remainingDistanceToDestination;

            public ResultChainedPath(Vector3 position, float remainingDistanceToDestination) : this()
            {
                this.position = position;
                this.remainingDistanceToDestination = remainingDistanceToDestination;
            }
        }

        static List<ResultChainedPath> RebuildPath(DiscretizedNavigableSpace space, ChainedPathStore allocator, ChainedPath path, Vector3 destination)
        {
            DiscretizedNavigableSpace.Movement[] movements = space.GetMovements();
            List<ResultChainedPath> result = new List<ResultChainedPath>();
            // Destination does not have discretized position, so it's synthetically added to the result
            result.Add(new ResultChainedPath(destination, 0f));
            Vector3 waypoint = space.Reify(path.position);
            float remainingDistanceToDestination = Vector2.Distance(destination, waypoint);
            result.Add(new ResultChainedPath(waypoint, remainingDistanceToDestination));
            while (path.sourceChainedPathIndex >= 0)
            {
                remainingDistanceToDestination += movements[path.movementIndex].cost;
                path = allocator.Get(path.sourceChainedPathIndex);
                result.Add(new ResultChainedPath(space.Reify(path.position), remainingDistanceToDestination));
            }
            // We could synthetically add the source at the beginning of the result path, but who cares?
            result.Reverse();
            return result;
        }

        public PathFinding(DiscretizedNavigableSpace space)
        {
            this.space = space;
            store = new ChainedPathStore();
            openList = new PriorityQueue<ChainedPath>();
            spaceState = new DiscretizedSearchSpace();
        }

        public PathFindingResult RetryableFindShortestPath(Vector3 start, Vector3 destination, float maxLength, out List<ResultChainedPath> path, float weight = 1f)
        {
            bool isResumable = false;
            // Information too old, parameters changed or target has moved: we don't have a cached answer and can't resume computation
            if (Time.time >= cacheTTL
                || this.weight != weight
                || (destination - this.destination).sqrMagnitude > 4f)
            {
                // Indeed we must start over
            }
            else if (status == PathFindingResult.Success)
            {
                // Assuming we're following the path, can we return a suffix of foundPath?
                int firstForward = 0;
                // Look for the first step heading "forward"
                while (firstForward < foundPath.Count - 1 && 
                       Vector3.Dot(foundPath[firstForward].position - start, foundPath[firstForward + 1].position - foundPath[firstForward].position) < 0f)
                    firstForward++;
                float minSqrDist = (foundPath[firstForward].position - start).sqrMagnitude;
                if (minSqrDist < 3f)
                {
                    // maxLength may have changed, is the path still matching?
                    float pathLength = Mathf.Sqrt(minSqrDist) + foundPath[firstForward].remainingDistanceToDestination;
                    if (pathLength <= maxLength)
                    {
                        Debug.LogFormat("Pathfinding existing path ok, shortened by {0}", firstForward);

                        if (firstForward > 0)
                        {
                            foundPath = foundPath.GetRange(firstForward, foundPath.Count - firstForward);
                        }
                        path = foundPath;
                        return PathFindingResult.Success;
                    }
                    else
                        Debug.LogFormat("Pathfinding existing path too long");
                }
                else
                    Debug.LogFormat("Pathfinding strayed too far from existing path");
            }
            else if (status == PathFindingResult.Failure)
            {
                // Cached answer was that target was unreachable and nobody has moved much (destination checked above): probably still true
                if (this.maxLength >= maxLength && (start - this.start).sqrMagnitude < 4f)
                {
                    Debug.LogFormat("Pathfinding lack of path still valid");
                    path = null;
                    return PathFindingResult.Failure;
                }
            }
            else
            {
                // Query hasn't changed but we aren't done yet: let's continue
                if (inProgress)
                    isResumable = true;
            }

            if (!isResumable)
            {
                Debug.LogFormat("Pathfinding starting over");
                Initialization(start, destination, maxLength, weight);
                // Try to answer synchronously from Update()?
                inProgress = true;
                FindShortestPath();
                path = foundPath;
                return status;
            }
            else
                Debug.LogFormat("Pathfinding resuming");

            inProgress = true;
            path = null;
            return PathFindingResult.NotCompleted;
        }

        public void FixedUpdate()
        {
            if (inProgress)
            {
                FindShortestPath();
                if (status != PathFindingResult.NotCompleted)
                {
                    inProgress = false;
                }
            }
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
            spaceState.Clear();
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
                        spaceState.SetCellDone(destinationPosition);
                    }
                }
            }
        }

        public void FindShortestPath()
        {
            List<ChainedPath> newPathsBuffer = new List<ChainedPath>(26);
            bool overBudget = false;
            while (!overBudget && openList.Count() > 0)
            {
                if (!DiscretizedNavigableSpace.DecrCyclesBudget())
                {
                    overBudget = true;
                    break;
                }
                // Don't remove from openList just yet, in case we're interrupted by OverRaycastBudgetException
                ChainedPath Path = openList.Peek();
                int pathIndex = store.Add(Path);
                DiscretizedSearchSpace.CellState cellState = spaceState.GetCellState(Path.position);
                if (cellState != DiscretizedSearchSpace.CellState.Closed)
                {
                    PathFindingResult isNavigable = Path.sourceChainedPathIndex >= 0 
                        ? space.IsNavigableInt(store.Get(Path.sourceChainedPathIndex).position, Path.position, Path.movementIndex) 
                        // Start does not have discretized position, so it's special cased as "source index = -1"
                        : space.IsNavigable(start, space.Reify(Path.position));
                    if (isNavigable == PathFindingResult.NotCompleted)
                    {
                        overBudget = true;
                        break;
                    }
                    if (isNavigable == PathFindingResult.Success)
                    {
                        // Are we arrived yet?
                        if (cellState == DiscretizedSearchSpace.CellState.Done)
                        {
                            PathFindingResult isNavigableToDestination = space.IsNavigable(space.Reify(Path.position), destination);
                            if (isNavigableToDestination == PathFindingResult.NotCompleted)
                            {
                                overBudget = true;
                                break;
                            }
                            if (isNavigableToDestination == PathFindingResult.Success)
                            {
                                foundPath = RebuildPath(space, store, Path, destination);
                                status = PathFindingResult.Success;
                                return;
                            }
                        }

                        DiscretizedNavigableSpace.Movement[] movements = space.GetMovements();
                        for (int i = 0; i < movements.Length - 1; i++)
                        {
                            DiscretizedNavigableSpace.Movement movement = movements[i];
                            Vector3Int newPosition = Path.position + movement.delta;
                            float newCost = Path.cost + movement.cost;
                            if (newCost <= maxLength)
                            {
                                float heuristicCost = space.HeuristicCost(space.Reify(newPosition), destination);
//                                if (newCost + heuristicCost <= maxLength)
//                                {
                                    ChainedPath newPath = new ChainedPath(newPosition, newCost, newCost + heuristicCost * weight, i, pathIndex);
                                    newPathsBuffer.Add(newPath);
//                                }
                            }
                        }
                    }
                    openList.Dequeue();
                    foreach (ChainedPath newPath in newPathsBuffer)
                        openList.Enqueue(newPath);
                    newPathsBuffer.Clear();
                    spaceState.SetCellClosed(Path.position);
                }
                else
                    openList.Dequeue();
            }
            foundPath = null;
            status = overBudget ? PathFindingResult.NotCompleted : PathFindingResult.Failure;
            return;
        }
    }
}