using Godot;

namespace ECS.Utils;

/// <summary>
/// Watches a Godot node subtree and maintains a live set of descendant nodes of type <typeparamref name="TNode"/>.
/// </summary>
public sealed class NodesTracker<TNode>
    where TNode : Node
{
    /// <summary>
    /// Raised when a node of type <typeparamref name="TNode"/> enters the tracked subtree.
    /// </summary>
    public event Action<TNode>? NodeTracked;

    /// <summary>
    /// Raised when a node of type <typeparamref name="TNode"/> exits the tracked subtree.
    /// </summary>
    public event Action<TNode>? NodeUntracked;

    /// <summary>
    /// Live set of all nodes of type <typeparamref name="TNode"/> currently in the tracked subtree.
    /// </summary>
    public IReadOnlySet<TNode> Nodes => this._nodes;

    /// <summary>
    /// When <c>true</c>, only direct children of the root are tracked; no recursion. Defaults to <c>false</c>.
    /// </summary>
    public bool DirectChildren { get; init; }

    private readonly HashSet<TNode> _nodes = [];

    private Node? _root;

    /// <summary>
    /// Starts watching <paramref name="root"/> and all its descendants; the root itself is excluded from <see cref="Nodes"/>.
    /// </summary>
    public void Track(Node root)
    {
        if (this._root != null)
            throw new InvalidOperationException($"You need to call '{nameof(this.Untrack)}' before calling '{nameof(this.Track)}'");

        this._root = root;

        this.OnChildEnteredTree(this._root);
    }

    private void OnChildEnteredTree(Node node)
    {
        node.ChildEnteredTree += this.OnChildEnteredTree;
        node.ChildExitingTree += this.OnChildExitingTree;

        if (node != this._root && node is TNode match)
        {
            if (!this._nodes.Add(match))
                throw new InvalidOperationException($"Node {match.Name} has already being tracked");

            this.NodeTracked?.Invoke(match);
        }

        if (!this.DirectChildren)
            foreach (var child in node.GetChildren())
                this.OnChildEnteredTree(child);
    }

    private void OnChildExitingTree(Node node)
    {
        if (!this.DirectChildren)
            foreach (var child in node.GetChildren())
                this.OnChildExitingTree(child);

        if (node != this._root && node is TNode match)
        {
            if (!this._nodes.Remove(match))
                throw new InvalidOperationException($"Node {match.Name} hasn't been tracked before");

            this.NodeUntracked?.Invoke(match);
        }

        node.ChildExitingTree -= this.OnChildExitingTree;
        node.ChildEnteredTree -= this.OnChildEnteredTree;
    }

    /// <summary>
    /// Stops watching the subtree and cleans up all event subscriptions.
    /// </summary>
    public void Untrack()
    {
        if (this._root == null)
            throw new InvalidOperationException($"You need to call '{nameof(this.Track)}' before calling '{nameof(this.Untrack)}'");

        this.OnChildExitingTree(this._root);

        this._root = null;
    }
}
