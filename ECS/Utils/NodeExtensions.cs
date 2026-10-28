using Godot;

namespace ECS.Utils;

/// <summary>
/// Extension methods for Godot Node.
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// Subscribes to sibling nodes of type TSibling entering/leaving the scene.
    /// </summary>
    public static void TrackSiblings<TSibling>(
        this Node self,
        Action<TSibling> onTracked,
        Action<TSibling> onUntracked,
        string? name = null,
        bool direct = false,
        bool unique = false
    )
        where TSibling : Node =>
            NodeExtensions.TrackNodes<TSibling>(self, onTracked, onUntracked, name, direct, unique, (node) => node.Owner);

    /// <summary>
    /// Subscribes to child nodes of type TChild entering/leaving the scene.
    /// </summary>
    public static void TrackChildren<TChild>(
        this Node self,
        Action<TChild> onTracked,
        Action<TChild> onUntracked,
        string? name = null,
        bool direct = false,
        bool unique = false
    )
        where TChild : Node =>
            NodeExtensions.TrackNodes<TChild>(self, onTracked, onUntracked, name, direct, unique, (node) => node);

    private static void TrackNodes<TNode>(
        Node self,
        Action<TNode> onTracked,
        Action<TNode> onUntracked,
        string? name,
        bool direct,
        bool unique,
        Func<Node, Node?> getRoot
    )
        where TNode : Node
    {
        if (self.IsInsideTree())
            throw new InvalidOperationException($"The node '{self.Name}' shouldn't been inside the three when tracking nodes");

        var processedNodes = new HashSet<Node>();
        var root = default(Node);
        var found = default(TNode);

        void OnTreeEntered()
        {
            root = getRoot(self);

            if (root != null)
                OnChildEnteredTree(root);
        }

        void TrackNode(Node node)
        {
            if (direct && node.GetParent() != root)
                return;

            if (node is not TNode sibling)
                return;

            if (name != null && sibling.Name != name)
                return;

            if (unique && found != null)
                throw new InvalidOperationException($"The node '{root.Name}' already contains a component of type '{typeof(TNode).Name}'");

            found = sibling;
            onTracked(sibling);
        }

        void OnChildEnteredTree(Node node)
        {
            if (!node.IsInsideTree())
                return;

            if (!processedNodes.Add(node))
                return;

            node.ChildEnteredTree += OnChildEnteredTree;
            node.ChildExitingTree += OnChildExitingTree;

            TrackNode(node);

            foreach (var child in node.GetChildren())
                OnChildEnteredTree(child);
        }

        void OnChildExitingTree(Node node)
        {
            if (!node.IsInsideTree())
                return;

            if (!processedNodes.Remove(node))
                return;

            foreach (var child in node.GetChildren())
                OnChildExitingTree(child);

            UntrackNode(node);

            node.ChildExitingTree -= OnChildExitingTree;
            node.ChildEnteredTree -= OnChildEnteredTree;
        }

        void UntrackNode(Node node)
        {
            if (direct && node.GetParent() != root)
                return;

            if (node is not TNode sibling)
                return;

            if (name != null && sibling.Name != name)
                return;

            if (unique && found != sibling)
                throw new InvalidOperationException($"The node '{root.Name}' already contains a component of type '{typeof(TNode).Name}'");

            found = null;
            onUntracked(sibling);
        }

        void OnTreeExiting()
        {
            if (root != null)
                OnChildExitingTree(root);

            root = null;
        }

        self.TreeEntered += OnTreeEntered;
        self.TreeExiting += OnTreeExiting;
    }
}
