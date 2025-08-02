using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer
{
    private readonly record struct FileInfo(string Directory, string Name);

    private readonly record struct NodePath(string Path, int Depth);

    private sealed class Node
    {
        private readonly List<Leaf> _leaves = [];
        private readonly List<Node> _nodes  = [];

        public IReadOnlyList<Leaf> Leaves => _leaves;
        public IReadOnlyList<Node> Nodes  => _nodes;

        public void AddNode(Node node) => _nodes.Add(node);

        public void AddLeaf(Leaf leaf) => _leaves.Add(leaf);
    }

    private sealed class Leaf
    {
        public string FullPath      { get; }
        public bool   IsDynamicPath { get; }

        public Leaf(string fullPath)
        {
            FullPath      = fullPath;
            IsDynamicPath = RegexHelpers.IsDynamicPathRegex().Match(fullPath).Success;
        }
    }

    private static partial class RegexHelpers
    {
        [GeneratedRegex(@"\\\[[a-zA-Z]([a-zA-Z0-9_]?)+\]\.razor$")]
        internal static partial Regex IsDynamicPathRegex();
    }
}