using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer
{
    private readonly record struct FileInfo(string Directory, string Name);

    private readonly record struct NodePath(string Path, int Depth);

    private class Node
    {
        private readonly List<Leaf> _layouts = [];
        private readonly List<Leaf> _leaves  = [];
        private readonly List<Node> _nodes   = [];

        private bool _hasLayout;

        public IReadOnlyList<Leaf> Layouts => _layouts;
        public IReadOnlyList<Leaf> Leaves  => _leaves;
        public IReadOnlyList<Node> Nodes   => _nodes;

        public Node()
        {
        }

        public Node(Node parent) => _layouts.AddRange(parent._layouts);

        public void AddNode(Node node) => _nodes.Add(node);

        public void AddLeaf(Leaf leaf, bool isLayout)
        {
            if (isLayout)
            {
                if (_hasLayout)
                    throw new ArgumentException(@"Cannot have two layouts in the same directory", nameof(isLayout));

                _hasLayout = isLayout;
                _layouts.Add(leaf);
            }
            else
            {
                _leaves.Add(leaf);
            }
        }
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