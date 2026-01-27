using System;
using System.Buffers;
using System.Text;

using TMPro;

using UnityEngine;

public static class EmojiReplacer
{
    private static readonly Trie _trie = new Trie();
    private static bool _initialized;

    [ThreadStatic] private static StringBuilder _sharedSb;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AwakeEmojiSystem()
    {
        Initialize(TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets[2]);
    }

    public static void Initialize(TMP_SpriteAsset spriteAsset)
    {
        Debug.Log($"Test - Emoji Initialize(asset name: {spriteAsset.name})");
        if (_initialized) return;

        var asset = spriteAsset != null ? spriteAsset : TMP_Settings.defaultSpriteAsset;
        if (asset == null) return;

        var table = asset.spriteCharacterTable;
        if (table == null) return;

        // Trie 구축 - sprite tag를 직접 저장
        for (int i = 0; i < table.Count; i++)
        {
            var sc = table[i];
            if (sc == null || string.IsNullOrEmpty(sc.name)) continue;

            int[] codePoints = ParseName(sc.name);
            if (codePoints != null)
            {
                string spriteTag = $"<sprite name=\"{sc.name}\">";
                _trie.Insert(codePoints, spriteTag);
            }
        }

        _initialized = true;
    }

    private static int[] ParseName(string name)
    {
        var parts = name.Split('-');
        var parsed = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out parsed[i]))
                return null;
        }
        return parsed;
    }

    private const int STACK_ALLOC_THRESHOLD = 256;

    public static string Replace(string input)
    {
        if (string.IsNullOrEmpty(input) || !_initialized)
            return input;

        int maxCodePoints = input.Length;
        if (maxCodePoints <= STACK_ALLOC_THRESHOLD)
        {
            Span<int> buffer = stackalloc int[maxCodePoints];
            return ReplaceCore(input, buffer);
        }
        else
        {
            int[] rentedArray = ArrayPool<int>.Shared.Rent(maxCodePoints);
            try
            {
                return ReplaceCore(input, rentedArray.AsSpan(0, maxCodePoints));
            }
            finally
            {
                ArrayPool<int>.Shared.Return(rentedArray);
            }
        }
    }

    private static string ReplaceCore(string input, Span<int> codePointBuffer)
    {
        int cpCount = ToCodePointsSpan(input, codePointBuffer);

        var sb = _sharedSb ??= new StringBuilder(512);
        sb.Clear();
        sb.EnsureCapacity(input.Length + 64);

        int i = 0;
        while (i < cpCount)
        {
            if (_trie.TryMatchLongest(codePointBuffer, i, cpCount, out int matchLength, out string spriteTag))
            {
                sb.Append(spriteTag);
                i += matchLength;
            }
            else
            {
                AppendCodePoint(sb, codePointBuffer[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private static int ToCodePointsSpan(string s, Span<int> buffer)
    {
        int count = 0;
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                buffer[count++] = char.ConvertToUtf32(c, s[i + 1]);
                i += 2;
            }
            else
            {
                buffer[count++] = c;
                i++;
            }
        }
        return count;
    }

    private static void AppendCodePoint(StringBuilder sb, int cp)
    {
        if (cp <= 0xFFFF)
        {
            char c = (char)cp;
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default: sb.Append(c); break;
            }
        }
        else
        {
            sb.Append(char.ConvertFromUtf32(cp));
        }
    }
}
