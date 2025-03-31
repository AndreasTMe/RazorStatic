using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer
{
    private readonly record struct FileInfo(string Directory, string Name);

    private readonly record struct NodePath(string Path, int Depth);

    private class Node
    {
        private readonly List<Leaf> _leaves = [];
        private readonly List<Node> _nodes  = [];

        public IReadOnlyList<Leaf> Leaves => _leaves;
        public IReadOnlyList<Node> Nodes  => _nodes;

        public void AddNode(Node node) => _nodes.Add(node);

        public void AddLeaf(Leaf leaf) => _leaves.Add(leaf);
    }

    private partial class Leaf
    {
        public string FullPath      { get; }
        public bool   IsDynamicPath { get; }

        public Leaf(string fullPath)
        {
            FullPath      = fullPath;
            IsDynamicPath = IsDynamicPathRegex().Match(fullPath).Success;
        }

        [GeneratedRegex(@"\\\[[a-zA-Z]([a-zA-Z0-9_]?)+\]\.razor$")]
        private static partial Regex IsDynamicPathRegex();
    }
}