# Emoji Sprite Replacer for TMP

High-performance Unicode emoji replacement system for TextMeshPro using sprite assets. Optimized for mobile with full support for composite emojis, skin tones, and flags.

## Features

- ✨ Fast tokenization algorithm for composite emoji detection
- 🎨 ZWJ sequences, skin tone modifiers, regional indicators
- 🛠️ Built-in sprite asset generator
- 📱 Mobile-optimized rendering

## Installation

**Via Package Manager:**
1. `Window > Package Manager` → `+` → `Add package from git URL...`
2. Enter: `https://github.com/leejaemin985/UnityCompositeEmojiRenderer.git`

**Via manifest.json:**
```json
{
  "dependencies": {
    "com.leejaemin.composite-emoji-replacer": "https://github.com/leejaemin985/UnityCompositeEmojiRenderer.git"
  }
}
```

## Quick Start

### 1. Generate Sprite Asset

`Tools > Emoji > Generate TMP Sprite Asset`
- Select folder with emoji PNG files (named as Unicode codepoints: `1f600.png`)
- Configure atlas size
- Click Generate

### 2. Use in Code
```csharp
using TMPro;

public class Example : MonoBehaviour
{
    [SerializeField] private TMP_SpriteAsset emojiSpriteAsset;
    [SerializeField] private TMP_Text textComponent;
    
    void Start()
    {
        EmojiReplacer.Initialize(emojiSpriteAsset);
        
        string text = "Hello 👋 World 🌍";
        textComponent.text = EmojiReplacer.Replace(text);
    }
}
```

### 3. InputField Integration
```csharp
inputField.onValueChanged.AddListener(text => {
    displayText.text = EmojiReplacer.Replace(text);
});
```

## How It Works

1. **Tokenization** - Detects emoji sequences (including ZWJ, skin tones, flags)
2. **Replacement** - Converts to TMP sprite tags (`<sprite name="1f600">`)
3. **Rendering** - TextMeshPro displays sprites efficiently

## Samples

Import via Package Manager → Samples section:
Basic Sample: minimal demo scene and generated TMP SpriteAsset example.
Emoji artwork is not bundled. Use Twemoji or your own emoji PNG set to regenerate assets.

## Adding/Updating Emojis

1. Add PNG files to folder (naming: `1f600.png`, `1f468-200d-1f4bb.png`)
2. Run `Tools > Emoji > Generate TMP Sprite Asset`
3. Regenerate with same output path

**Naming conventions:**
- Single: `1f600.png` → 😀
- ZWJ: `1f468-200d-1f469-200d-1f467.png` → 👨‍👩‍👧
- Skin tone: `1f44b-1f3fb.png` → 👋🏻
- Flags: `1f1f0-1f1f7.png` → 🇰🇷

## Native Mobile Input (Optional)

For better mobile keyboard support, install [UMI](https://github.com/mopsicus/umi.git):
```json
"com.mopsicus.umi": "https://github.com/mopsicus/umi.git"
```

## API
```csharp
EmojiReplacer.Initialize(TMP_SpriteAsset spriteAsset);
string replaced = EmojiReplacer.Replace(string text);
bool hasEmojis = EmojiReplacer.ContainsEmoji(string text);
```

## Requirements

- Unity 2021.3+
- TextMeshPro 3.0.6+

## License

MIT License

## Credits

- Twemoji graphics: [Twitter](https://github.com/twitter/twemoji) (CC-BY 4.0)
- Recommended input: [UMI by Mopsicus](https://github.com/mopsicus/umi)

---

Made with ❤️ for Unity
