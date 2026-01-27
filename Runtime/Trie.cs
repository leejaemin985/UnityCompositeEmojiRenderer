using System;

public sealed class Trie
{
    private readonly TrieNode _root = new TrieNode();

    public void Insert(int[] codePoints, string spriteTag)
    {
        if (codePoints == null || codePoints.Length == 0)
            return;

        var node = _root;
        foreach (var cp in codePoints)
        {
            node = node.GetOrAddChild(cp);
        }
        node.IsEnd = true;
        node.SpriteTag = spriteTag;
    }

    public bool TryMatchLongest(Span<int> buffer, int start, int length, out int matchLength, out string spriteTag)
    {
        matchLength = 0;
        spriteTag = null;

        if (start >= length)
            return false;

        var node = _root;
        int lastMatchLength = 0;
        string lastMatchTag = null;

        for (int i = start; i < length; i++)
        {
            node = node.GetChild(buffer[i]);
            if (node == null)
                break;

            if (node.IsEnd)
            {
                lastMatchLength = i - start + 1;
                lastMatchTag = node.SpriteTag;
            }
        }

        if (lastMatchTag != null)
        {
            matchLength = lastMatchLength;
            spriteTag = lastMatchTag;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _root.Children?.Clear();
    }
}
