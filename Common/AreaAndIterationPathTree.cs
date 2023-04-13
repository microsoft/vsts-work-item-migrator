using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common
{
    public class AreaAndIterationPathTree
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<AreaAndIterationPathTree>();
        public ISet<string> AreaPathList { get; } = new HashSet<string>();
        public ISet<string> IterationPathList { get; } = new HashSet<string>();

        public AreaAndIterationPathTree(IList<WorkItemClassificationNode> nodeList)
        {
            if (nodeList != null)
            {
                foreach (var node in nodeList)
                {
                    if (node.StructureType == TreeNodeStructureType.Area)
                    {
                        CreateAreaPathList(node);
                    }
                    else if (node.StructureType == TreeNodeStructureType.Iteration)
                    {
                        CreateIterationPathList(node);
                    }
                }
            }
            else
            {
                //this should never happen 
                Logger.LogError(LogDestination.All, "Critial error in retrieving Area and Iteration path from target");
                throw new ArgumentNullException("Critial error in retrieving Area and Iteration path from target");
            }
        }

        //TODO: find a better place to put this, Get Unit Tests for this again
        public static string ReplaceLeadingProjectName(string input, string sourceProject, string targetProject)
        {
            string replacedInput;
            if (!TryReplaceLeadingProjectName(input, sourceProject, targetProject, out replacedInput))
            {
                // unexpected, the source/target project should have already been validated
                throw new ArgumentException($"Could not find the source project name to replace in the following field value: {input}. Please make sure all team project values match the project name of your source and all area and iteration path values start with the project name of your source.");
            }

            return replacedInput;
        }

        public static bool TryReplaceLeadingProjectName(string input, string sourceProject, string targetProject, out string replacedInput)
        {
            replacedInput = null;
            // Handles case when System.AreaPath or System.IteraionPath consist of only sourceProject
            if (input.Equals(sourceProject, StringComparison.OrdinalIgnoreCase))
            {
                replacedInput = targetProject;
            }
            else if (Regex.IsMatch(input, $@"^{sourceProject}\\", RegexOptions.IgnoreCase))
            {
                replacedInput = Regex.Replace(input, $@"^{sourceProject}\\", $"{targetProject}\\", RegexOptions.IgnoreCase);
            }

            return replacedInput != null;
        }

        public static void ReplaceRemainingPathComponents(string input, Dictionary<string, string> mappings, out string replaced)
        {
            replaced = input;

            if (mappings == null || mappings.Count == 0)
            {
                return;
            }

            foreach (var map in mappings)
            {
                var pattern = Regex.Escape($"\\{map.Key}");
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                {
                    replaced = Regex.Replace(input, pattern, $"\\{map.Value}", RegexOptions.IgnoreCase);
                    break;
                }
            }
        }

        private void CreateAreaPathList(WorkItemClassificationNode headnode)
        {
            //node is the headnode
            if (headnode == null)
            {
                return;
            }
            //path for the headnode is null
            ProcessNode(null, headnode, this.AreaPathList);
        }

        private void CreateIterationPathList(WorkItemClassificationNode headnode)
        {
            //node is the headnode
            if (headnode == null)
            {
                return;
            }
            //path for the headnode is null
            ProcessNode(null, headnode, this.IterationPathList);
        }

        private void ProcessNode(string path, WorkItemClassificationNode node, ISet<string> pathList)
        {
            if (node == null)
            {
                return;
            }
            string currentpath;
            if (path != null)
            {
                currentpath = $"{path}\\{node.Name}";
            }
            else
            {
                //very first node will have null path, so it will be just a node name
                currentpath = node.Name;
            }

            pathList.Add(currentpath);

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    ProcessNode(currentpath, child, pathList);
                }
            }
        }
    }
}
