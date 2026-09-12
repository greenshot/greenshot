/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Expressions;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Recipes;
using log4net;

namespace Greenshot.Pipeline
{
    /// <summary>
    /// Executes Directed Acyclic Graph (DAG) capture recipes.
    /// Handles asynchronous node execution, concurrent branch splitting (fork),
    /// dependency join synchronization (merge), and dynamic expression resolution.
    /// </summary>
    public class DagExecutionEngine
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DagExecutionEngine));

        private readonly Func<RecipeNodeConfig, ICaptureStep> _stepFactory;

        public DagExecutionEngine(Func<RecipeNodeConfig, ICaptureStep> stepFactory)
        {
            _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        }

        /// <summary>
        /// Executes the DAG recipe to completion or until cancelled/aborted.
        /// </summary>
        public async Task ExecuteAsync(CaptureRecipe recipe, CaptureFlowContext context, CancellationToken cancellationToken = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (recipe.Nodes == null || recipe.Nodes.Count == 0)
            {
                context.LogStep("DAG execution finished: recipe has no nodes.");
                return;
            }

            var nodesById = new Dictionary<string, RecipeNodeConfig>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in recipe.Nodes)
            {
                if (!string.IsNullOrWhiteSpace(node?.Id))
                {
                    nodesById[node.Id] = node;
                }
            }

            var flow = recipe.Flow ?? new RecipeFlowConfig();
            var transitions = flow.GetUnifiedTransitions();
            var startNodes = flow.GetEffectiveStartNodes();

            if (startNodes.Count == 0 && recipe.Nodes.Count > 0)
            {
                startNodes.Add(recipe.Nodes[0].Id);
            }

            // Compute incoming predecessors for each node
            var incomingMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var nodeId in nodesById.Keys)
            {
                incomingMap[nodeId] = new List<string>();
            }

            foreach (var kvp in transitions)
            {
                string from = kvp.Key;
                foreach (var to in kvp.Value)
                {
                    if (incomingMap.TryGetValue(to, out var predecessors))
                    {
                        if (!predecessors.Contains(from, StringComparer.OrdinalIgnoreCase))
                        {
                            predecessors.Add(from);
                        }
                    }
                }
            }

            // Track pending predecessor completion counts for join synchronization
            var pendingIncoming = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in incomingMap)
            {
                pendingIncoming[kvp.Key] = kvp.Value.Count;
            }

            var completedNodes = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var syncLock = new object();
            var runningTasks = new List<Task>();

            Log.InfoFormat("Starting DAG execution for recipe '{0}' ({1} node(s), {2} start node(s))",
                recipe.Name, nodesById.Count, startNodes.Count);

            context.LogStep($"DAG Execution started with {startNodes.Count} entry node(s)");

            // Asynchronous recursive node runner
            async Task RunNodeAsync(string nodeId)
            {
                if (context.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!nodesById.TryGetValue(nodeId, out var nodeConfig))
                {
                    Log.WarnFormat("DAG node '{0}' not found in recipe.", nodeId);
                    context.LogStep($"Warning: DAG node '{nodeId}' not found.");
                    return;
                }

                // Check node enabled state
                if (nodeConfig.Enabled)
                {
                    try
                    {
                        // Dynamically resolve expressions in node parameters prior to execution
                        var resolvedConfig = nodeConfig.Clone();
                        resolvedConfig.Parameters = ExpressionEvaluator.Instance.ResolveParameters(nodeConfig.Parameters, context);

                        var step = _stepFactory(resolvedConfig);
                        if (step == null)
                        {
                            Log.WarnFormat("Could not resolve executable step for node '{0}' [{1}]", nodeConfig.Id, nodeConfig.StepType);
                            context.LogStep($"Warning: unresolved step factory for node '{nodeConfig.Id}' [{nodeConfig.StepType}]");
                        }
                        else
                        {
                            context.LogStep($"Executing node: [{nodeConfig.Id}] {step.Name}");
                            Log.InfoFormat("Executing DAG node: [{0}] '{1}' [{2}]", nodeConfig.Id, step.Name, nodeConfig.StepType);
                            await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
                            Log.InfoFormat("Finished DAG node: [{0}] '{1}'", nodeConfig.Id, step.Name);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        context.Abort($"Node '{nodeConfig.Id}' cancelled.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Node '{nodeConfig.Id}' failed with exception", ex);
                        context.Fail($"Node '{nodeConfig.Id}' failed: {ex.Message}", ex);
                        return;
                    }
                }
                else
                {
                    context.LogStep($"Skipping disabled node: [{nodeConfig.Id}] {nodeConfig.Name}");
                }

                completedNodes[nodeId] = true;

                if (context.IsAborted || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Find next ready downstream nodes
                var nextToLaunch = new List<string>();
                if (transitions.TryGetValue(nodeId, out var nextNodes) && nextNodes != null)
                {
                    lock (syncLock)
                    {
                        foreach (var nextId in nextNodes)
                        {
                            if (pendingIncoming.TryGetValue(nextId, out int remaining))
                            {
                                remaining--;
                                pendingIncoming[nextId] = remaining;
                                if (remaining <= 0 && !completedNodes.ContainsKey(nextId))
                                {
                                    nextToLaunch.Add(nextId);
                                }
                            }
                        }
                    }
                }

                // Split / Fork to all ready downstream nodes
                if (nextToLaunch.Count > 0)
                {
                    var childTasks = nextToLaunch.Select(RunNodeAsync).ToArray();
                    await Task.WhenAll(childTasks).ConfigureAwait(false);
                }
            }

            // Launch all start nodes
            var initialTasks = startNodes.Where(id => nodesById.ContainsKey(id)).Select(RunNodeAsync).ToArray();
            if (initialTasks.Length > 0)
            {
                await Task.WhenAll(initialTasks).ConfigureAwait(false);
            }
        }
    }
}
