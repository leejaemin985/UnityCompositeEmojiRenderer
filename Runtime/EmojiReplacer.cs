using System;
using System.Buffers;
using System.Text;

using TMPro;

using UnityEngine;

namespace CompositeEmoji
{
    /// <summary>
    /// 입력 문자열에서 이모지 시퀀스를 찾아 TMP sprite 태그(&lt;sprite name="..."&gt;)로 치환한다.
    /// 시퀀스 매칭은 Trie 기반 longest-match를 사용한다.
    /// 사용 흐름: Initialize(spriteAsset) → Replace(input)
    /// </summary>
    public static class EmojiReplacer
    {
        // 코드포인트 시퀀스 → sprite 태그 문자열을 저장하는 매칭 트리
        private static readonly Trie _trie = new Trie();

        // Initialize() 1회 실행 가드. 이미 초기화된 경우 재호출 무시.
        private static bool _initialized;

        // Replace() 호출마다 새 StringBuilder를 할당하지 않기 위해 스레드별로 재사용.
        [ThreadStatic] private static StringBuilder _sharedSb;

        /// <summary>
        /// TMP_SpriteAsset의 캐릭터 테이블을 훑어 Trie를 구축한다.
        /// 호출할 때마다 기존 Trie를 비우고 전달된 에셋으로 다시 빌드한다.
        /// spriteAsset이 null이면 TMP_Settings.defaultSpriteAsset로 폴백.
        /// 유효한 에셋이 없으면 Trie를 비운 채 비초기화 상태로 둔다.
        /// </summary>
        public static void Initialize(TMP_SpriteAsset spriteAsset)
        {
            // 이전 호출의 잔재 제거 — 다른 에셋으로 다시 부르거나 같은 에셋이라도 변경된 경우 대비.
            _trie.Clear();
            _initialized = false;

            var asset = spriteAsset != null ? spriteAsset : TMP_Settings.defaultSpriteAsset;
            if (asset == null) return;

            var table = asset.spriteCharacterTable;
            if (table == null) return;

            // 각 스프라이트 이름("1f468-200d-1f4bb" 형태)을 코드포인트 배열로 파싱해 Trie에 삽입.
            // 매칭 시 바로 사용할 sprite 태그 문자열을 값으로 함께 저장(런타임 문자열 빌드 회피).
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

        /// <summary>
        /// "1f468-200d-1f4bb" 같은 하이픈 구분 hex 문자열을 코드포인트 배열로 변환.
        /// 하나라도 hex 파싱 실패 시 null 반환(해당 스프라이트는 Trie에서 제외).
        /// </summary>
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

        // 짧은 문자열은 stackalloc, 긴 문자열은 ArrayPool로 분기. 256은 char 기준 임계값.
        private const int STACK_ALLOC_THRESHOLD = 256;

        /// <summary>
        /// 입력 문자열의 이모지 시퀀스를 sprite 태그로 치환해 반환.
        /// 초기화 전이거나 입력이 비어 있으면 입력을 그대로 반환.
        /// </summary>
        public static string Replace(string input)
        {
            if (string.IsNullOrEmpty(input) || !_initialized)
                return input;

            // 코드포인트 버퍼 크기 상한: 입력 char 수(서로게이트 페어는 1 codepoint로 줄어듦).
            int maxCodePoints = input.Length;
            if (maxCodePoints <= STACK_ALLOC_THRESHOLD)
            {
                // 작은 입력: 스택 할당으로 GC 압력 0.
                Span<int> buffer = stackalloc int[maxCodePoints];
                return ReplaceCore(input, buffer);
            }
            else
            {
                // 큰 입력: ArrayPool에서 빌려 쓰고 finally에서 반납.
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

        /// <summary>
        /// 입력 문자열에 sprite로 치환 가능한 이모지 시퀀스가 하나라도 있는지 검사.
        /// Replace를 돌려 결과를 비교할 필요 없이 빠르게 가부만 확인할 때 사용.
        /// 초기화 전이거나 입력이 비어 있으면 false.
        /// </summary>
        public static bool ContainsEmoji(string input)
        {
            if (string.IsNullOrEmpty(input) || !_initialized)
                return false;

            int maxCodePoints = input.Length;
            if (maxCodePoints <= STACK_ALLOC_THRESHOLD)
            {
                Span<int> buffer = stackalloc int[maxCodePoints];
                return ContainsEmojiCore(input, buffer);
            }
            else
            {
                int[] rentedArray = ArrayPool<int>.Shared.Rent(maxCodePoints);
                try
                {
                    return ContainsEmojiCore(input, rentedArray.AsSpan(0, maxCodePoints));
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(rentedArray);
                }
            }
        }

        /// <summary>
        /// 실제 치환 루프. 1) 코드포인트로 디코드 → 2) Trie longest-match로 진행하며
        /// 매칭되면 sprite 태그 출력, 아니면 원본 코드포인트 1개 출력.
        /// </summary>
        private static string ReplaceCore(string input, Span<int> codePointBuffer)
        {
            int cpCount = ToCodePointsSpan(input, codePointBuffer);

            // 스레드별 StringBuilder 재사용 (없으면 lazy 생성). 사용 전 Clear 필수.
            var sb = _sharedSb ??= new StringBuilder(512);
            sb.Clear();
            sb.EnsureCapacity(input.Length + 64);

            int i = 0;
            while (i < cpCount)
            {
                // 현재 위치에서 가능한 가장 긴 매칭 시도. ZWJ 시퀀스 등 합성 이모지를 한 번에 처리하기 위함.
                if (_trie.TryMatchLongest(codePointBuffer, i, cpCount, out int matchLength, out string spriteTag))
                {
                    sb.Append(spriteTag);
                    i += matchLength;
                }
                else
                {
                    // 매칭 실패: 원본 코드포인트 한 글자만 출력하고 다음 위치로.
                    AppendCodePoint(sb, codePointBuffer[i]);
                    i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// ContainsEmoji 전용 루프. 매칭 1개라도 찾으면 즉시 true 반환(early-out).
        /// 치환 결과는 만들지 않으므로 StringBuilder도 쓰지 않는다.
        /// </summary>
        private static bool ContainsEmojiCore(string input, Span<int> codePointBuffer)
        {
            int cpCount = ToCodePointsSpan(input, codePointBuffer);

            for (int i = 0; i < cpCount; i++)
            {
                if (_trie.TryMatchLongest(codePointBuffer, i, cpCount, out int _, out string _))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// UTF-16 string을 코드포인트 배열로 디코드.
        /// 서로게이트 페어(BMP 밖 문자)는 두 char를 하나의 코드포인트로 합친다.
        /// 반환값은 buffer에 실제로 기록한 코드포인트 개수.
        /// </summary>
        private static int ToCodePointsSpan(string s, Span<int> buffer)
        {
            int count = 0;
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
                {
                    // 서로게이트 페어 → 단일 코드포인트(U+10000 이상)
                    buffer[count++] = char.ConvertToUtf32(c, s[i + 1]);
                    i += 2;
                }
                else
                {
                    // BMP 내 단일 char.
                    buffer[count++] = c;
                    i++;
                }
            }
            return count;
        }

        /// <summary>
        /// 코드포인트 하나를 StringBuilder에 다시 써넣는다.
        /// BMP 밖이면 서로게이트 페어로 변환.
        /// </summary>
        private static void AppendCodePoint(StringBuilder sb, int cp)
        {
            if (cp <= 0xFFFF)
                sb.Append((char)cp);
            else
                sb.Append(char.ConvertFromUtf32(cp));
        }
    }
}
