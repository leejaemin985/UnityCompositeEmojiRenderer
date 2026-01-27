using System.Collections.Generic;

public sealed class TrieNode
{
    public Dictionary<int, TrieNode> Children;
    public bool IsEnd;
    public string SpriteTag;

    public TrieNode()
    {
        Children = null;
        IsEnd = false;
        SpriteTag = null;
    }

    public TrieNode GetChild(int codePoint)
    {
        if (Children == null) return null;
        Children.TryGetValue(codePoint, out var child);
        return child;
    }

    public TrieNode GetOrAddChild(int codePoint)
    {
        Children ??= new Dictionary<int, TrieNode>();
        if (!Children.TryGetValue(codePoint, out var child))
        {
            child = new TrieNode();
            Children[codePoint] = child;
        }
        return child;
    }
}
