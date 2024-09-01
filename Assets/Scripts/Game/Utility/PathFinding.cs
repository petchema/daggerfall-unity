using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class PathFinding
    {
        protected class ChainedPath : IComparable<ChainedPath> 
        {
            public readonly Vector3Int position;
            public readonly float cost;
            public readonly float estimatedTotalCost; // cost + heuristic cost of remaining path
            public readonly ChainedPath source;

            public ChainedPath(Vector3Int position, float cost, float estimatedTotalCost, ChainedPath source)
            {
                this.position = position;
                this.cost = cost;
                this.estimatedTotalCost = estimatedTotalCost;
                this.source = source;
            }

            public int CompareTo(ChainedPath otherPath)
            {
                return estimatedTotalCost.CompareTo(otherPath.estimatedTotalCost);
            }
        }

        public bool FindShortestPath(DiscretizedSpace space, Vector3 start, Vector3 destination, float maxLength, out List<Vector3> path, float weight = 1f)
        {
            PriorityQueue<ChainedPath> openList = new PriorityQueue<ChainedPath>();
            ISet<Vector3> closedList = new HashSet<Vector3>();
            ISet<Vector3> destinationList = new HashSet<Vector3>();
            Vector3Int discretizedStart = space.Discretize(start);
            Vector3Int discretizedDestination = space.Discretize(destination);
            for (int x = 0; x <= 1; x++) 
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3Int startPosition = new Vector3Int(discretizedStart.x + x, discretizedStart.y + y, discretizedStart.z + z);
                        openList.Enqueue(new ChainedPath(startPosition, 0f, space.MeasuredCost(start, space.Reify(startPosition)) * weight, null));
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
                    if (!closedList.Contains(Path.position))
                    {
                        Vector3 source = Path.source != null ? space.Reify(Path.source.position) : start;
                        if (space.IsNavigable(source, space.Reify(Path.position)))
                        {
                            // Are we arrived yet?
                            if (destinationList.Contains(Path.position))
                            {
                                if (space.IsNavigable(space.Reify(Path.position), destination))
                                {
                                    path = space.RebuildPath(Path);
                                    path.Add(destination);
                                    return true;
                                }
                            }

                            foreach (DiscretizedSpace.Movement movement in space.GetMovements())
                            {
                                Vector3Int newPosition = Path.position + movement.delta;
                                float newCost = Path.cost + movement.cost;
                                if (newCost <= maxLength)
                                    openList.Enqueue(new ChainedPath(newPosition, newCost, newCost + space.HeuristicCost(space.Reify(newPosition), destination) * weight, Path));
                            }
                        }
                        closedList.Add(Path.position);
                    }
                }
                 
            } 
            catch(DiscretizedSpace.OverRaycastBudget)
            {
                // do nothing, just return path couldn't be found
            }
            path = null;
            return false;
        }
        
    }
}