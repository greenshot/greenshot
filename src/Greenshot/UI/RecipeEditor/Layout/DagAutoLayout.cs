using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Greenshot.UI.RecipeEditor.ViewModels;

namespace Greenshot.UI.RecipeEditor.Layout
{
    /// <summary>
    /// Computes hierarchical branch-aligned DAG auto-layout for recipe nodes.
    /// Preserves parent columns for parallel execution pipelines and centers join nodes.
    /// Eliminates overlaps and aligns vertically connected nodes in straight columns.
    /// </summary>
    public static class DagAutoLayout
    {
        private const double StartX = 400;
        private const double StartY = 80;
        private const double LevelYGap = 210;
        private const double SiblingXGap = 300;

        public static void ApplyLayout(
            IEnumerable<StepNodeViewModel> nodes,
            IEnumerable<StepConnectionViewModel> connections,
            string startNodeId)
        {
            var nodeList = nodes.ToList();
            var connList = connections.ToList();
            if (nodeList.Count == 0) return;

            var adj = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var parentsMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var nodeMap = new Dictionary<string, StepNodeViewModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var n in nodeList)
            {
                adj[n.Id] = new List<string>();
                parentsMap[n.Id] = new List<string>();
                inDegree[n.Id] = 0;
                nodeMap[n.Id] = n;
            }

            foreach (var c in connList)
            {
                if (c.SourceNode != null && c.TargetNode != null)
                {
                    if (adj.ContainsKey(c.SourceNode.Id))
                    {
                        adj[c.SourceNode.Id].Add(c.TargetNode.Id);
                    }
                    if (parentsMap.ContainsKey(c.TargetNode.Id))
                    {
                        parentsMap[c.TargetNode.Id].Add(c.SourceNode.Id);
                    }
                    if (inDegree.ContainsKey(c.TargetNode.Id))
                    {
                        inDegree[c.TargetNode.Id]++;
                    }
                }
            }

            // Find root nodes (startNode first, then 0 in-degree nodes)
            var roots = new List<string>();
            if (!string.IsNullOrEmpty(startNodeId) && nodeMap.ContainsKey(startNodeId))
            {
                roots.Add(startNodeId);
            }
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0 && !roots.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                {
                    roots.Add(kvp.Key);
                }
            }
            if (roots.Count == 0 && nodeList.Count > 0)
            {
                roots.Add(nodeList[0].Id);
            }

            // Assign Levels using longest path to ensure dependencies are always strictly above
            var nodeLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();

            foreach (var r in roots)
            {
                nodeLevels[r] = 0;
                queue.Enqueue(r);
            }

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();
                int curLvl = nodeLevels[u];
                if (adj.TryGetValue(u, out var children))
                {
                    foreach (var v in children)
                    {
                        int nextLvl = curLvl + 1;
                        if (!nodeLevels.ContainsKey(v) || nodeLevels[v] < nextLvl)
                        {
                            nodeLevels[v] = nextLvl;
                            queue.Enqueue(v);
                        }
                    }
                }
            }

            // Unreachable nodes get level 0
            foreach (var n in nodeList)
            {
                if (!nodeLevels.ContainsKey(n.Id))
                {
                    nodeLevels[n.Id] = 0;
                }
            }

            // Group by Level
            var levelGroups = new Dictionary<int, List<string>>();
            foreach (var kvp in nodeLevels)
            {
                if (!levelGroups.TryGetValue(kvp.Value, out var list))
                {
                    list = new List<string>();
                    levelGroups[kvp.Value] = list;
                }
                list.Add(kvp.Key);
            }

            // Compute X positions level by level
            var computedX = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var sortedLevels = levelGroups.Keys.OrderBy(k => k).ToList();

            foreach (var lvl in sortedLevels)
            {
                var ids = levelGroups[lvl];
                if (lvl == 0)
                {
                    double totalWidth = (ids.Count - 1) * SiblingXGap;
                    double firstX = StartX - (totalWidth / 2.0);
                    for (int i = 0; i < ids.Count; i++)
                    {
                        computedX[ids[i]] = firstX + (i * SiblingXGap);
                    }
                }
                else
                {
                    // Compute initial desired X based on upstream parents
                    var nodePositions = new List<(string Id, double DesiredX)>();

                    // Find all parents from previous levels
                    var parentGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    var multiParentOrNoParent = new List<string>();

                    foreach (var id in ids)
                    {
                        var directParents = parentsMap[id];
                        if (directParents.Count == 1)
                        {
                            var p = directParents[0];
                            if (!parentGroups.TryGetValue(p, out var siblings))
                            {
                                siblings = new List<string>();
                                parentGroups[p] = siblings;
                            }
                            siblings.Add(id);
                        }
                        else
                        {
                            multiParentOrNoParent.Add(id);
                        }
                    }

                    // Sibling forks: symmetrically center children around parent X
                    foreach (var kvp in parentGroups)
                    {
                        var parentId = kvp.Key;
                        var siblings = kvp.Value;
                        double parentX = computedX.TryGetValue(parentId, out var px) ? px : StartX;

                        if (siblings.Count == 1)
                        {
                            nodePositions.Add((siblings[0], parentX));
                        }
                        else
                        {
                            double forkWidth = (siblings.Count - 1) * SiblingXGap;
                            double forkStartX = parentX - (forkWidth / 2.0);
                            for (int i = 0; i < siblings.Count; i++)
                            {
                                nodePositions.Add((siblings[i], forkStartX + (i * SiblingXGap)));
                            }
                        }
                    }

                    // Multi-parent (joins) or unparented nodes
                    foreach (var id in multiParentOrNoParent)
                    {
                        var directParents = parentsMap[id];
                        if (directParents.Count > 1)
                        {
                            // Join node: center between all parents
                            double avgX = directParents.Average(p => computedX.TryGetValue(p, out var px) ? px : StartX);
                            nodePositions.Add((id, avgX));
                        }
                        else
                        {
                            nodePositions.Add((id, StartX));
                        }
                    }

                    // Sort by desired X
                    nodePositions = nodePositions.OrderBy(np => np.DesiredX).ToList();

                    // Collision / overlap resolution (ensure minimum SiblingXGap between nodes on the same level)
                    var currentPositions = new double[nodePositions.Count];
                    for (int i = 0; i < nodePositions.Count; i++)
                    {
                        currentPositions[i] = nodePositions[i].DesiredX;
                    }

                    // Forward pass: push right to resolve overlaps
                    for (int i = 1; i < currentPositions.Length; i++)
                    {
                        if (currentPositions[i] < currentPositions[i - 1] + SiblingXGap)
                        {
                            currentPositions[i] = currentPositions[i - 1] + SiblingXGap;
                        }
                    }

                    // Backward pass: pull back left if shifted too far and room exists
                    for (int i = currentPositions.Length - 2; i >= 0; i--)
                    {
                        double maxAllowed = currentPositions[i + 1] - SiblingXGap;
                        if (currentPositions[i] > maxAllowed)
                        {
                            currentPositions[i] = maxAllowed;
                        }
                    }

                    for (int i = 0; i < nodePositions.Count; i++)
                    {
                        computedX[nodePositions[i].Id] = currentPositions[i];
                    }
                }
            }

            // Apply calculated positions to ViewModels
            foreach (var n in nodeList)
            {
                double x = computedX.TryGetValue(n.Id, out var cx) ? cx : StartX;
                double y = StartY + (nodeLevels.TryGetValue(n.Id, out var lvl) ? lvl : 0) * LevelYGap;
                n.Location = new Point(Math.Max(50, x), Math.Max(50, y));
            }
        }
    }
}
