# Emoji Sprite Replacer for TMP

A high-performance Unicode emoji replacement system for TextMeshPro using sprite assets. This package enables mobile-optimized rendering of composite emojis including ZWJ sequences, skin tone modifiers, and regional indicators.

## Features

✨ **High-Performance Emoji Rendering**
- Utilizes TextMeshPro's sprite system (`<sprite name=>`, `<sprite index=>`) for efficient emoji display
- Fast tokenization algorithm to detect and replace composite emoji sequences
- Optimized for mobile platforms with minimal overhead

🎨 **Comprehensive Emoji Support**
- Zero-Width Joiner (ZWJ) sequences (e.g., 👨‍👩‍👧‍👦)
- Skin tone modifiers (e.g., 👋🏻, 👋🏿)
- Regional indicator symbols (flags: 🇰🇷, 🇺🇸)
- Full Unicode emoji standard compliance

🛠️ **Easy-to-Use Tools**
- Built-in sprite asset generator
- Sample scenes with Twemoji integration
- Runtime API for dynamic emoji replacement

## Installation

### Via Git URL (Package Manager)

1. Open `Window > Package Manager`
2. Click `+` button (top-left)
3. Select `Add package from git URL...`
4. Enter: `https://github.com/yourname/emoji-replacer.git`
5. Click `Add`

### Via manifest.json

Add this line to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.yourname.emoji-replacer": "https://github.com/yourname/emoji-replacer.git"
  }
}
```

## Quick Start

### 1. Generate TMP Sprite Asset

Use the built-in generator to create a sprite asset from your emoji images:

1. Go to `Tools > Emoji > Generate TMP Sprite Asset`
2. Configure your sprite source folder and output settings
3. Click `Generate`

The generator will automatically create:
- TMP Sprite Asset
- Sprite Atlas Texture
- Material

### 2. Initialize the Emoji Replacer
```csharp
using UnityEngine;
using TMPro;

public class EmojiExample : MonoBehaviour
{
    [SerializeField] private TMP_SpriteAsset emojiSpriteAsset;
    [SerializeField] private TMP_Text textComponent;
    
    void Start()
    {
        // Initialize the replacer with your sprite asset
        EmojiReplacer.Initialize(emojiSpriteAsset);
        
        // Replace emojis in text
        string text = "Hello 👋 World 🌍";
        string replaced = EmojiReplacer.Replace(text);
        
        textComponent.text = replaced;
        // Output: "Hello <sprite name="1f44b"> World <sprite name="1f30d">"
    }
}
```

### 3. Real-time Input Field Integration
```csharp
using UnityEngine;
using TMPro;

public class EmojiInputField : MonoBehaviour
{
    [SerializeField] private TMP_SpriteAsset emojiSpriteAsset;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text displayText;
    
    void Start()
    {
        EmojiReplacer.Initialize(emojiSpriteAsset);
        inputField.onValueChanged.AddListener(OnInputChanged);
    }
    
    void OnInputChanged(string text)
    {
        // Replace emojis in real-time
        string replaced = EmojiReplacer.Replace(text);
        displayText.text = replaced;
    }
}
```

## How It Works

The Emoji Sprite Replacer uses an advanced **tokenization algorithm** to efficiently detect and replace emoji sequences:

1. **Text Tokenization**: Input text is tokenized into individual characters and emoji sequences
2. **Composite Emoji Detection**: The system identifies composite emojis including:
   - Multi-codepoint sequences (ZWJ sequences)
   - Skin tone modifiers
   - Regional indicators
   - Variation selectors
3. **Sprite Tag Replacement**: Detected emojis are replaced with TextMeshPro sprite tags (`<sprite name="unicode">`)
4. **TMP Rendering**: TextMeshPro renders the sprite tags using the assigned sprite asset

This approach ensures **fast, mobile-optimized rendering** while maintaining full Unicode emoji compatibility.

## Samples

The package includes sample scenes demonstrating emoji replacement functionality.

### Twemoji Sample

The sample uses [Twemoji](https://github.com/twitter/twemoji) graphics to provide a comprehensive emoji sprite library.

**To import the sample:**

1. Open `Window > Package Manager`
2. Select `Emoji Sprite Replacer for TMP`
3. Expand `Samples` section
4. Click `Import` on the Twemoji sample

**What's included:**
- Pre-baked TMP Sprite Asset with 3,000+ emojis
- Demo scenes showing real-time emoji replacement
- Example scripts for InputField integration

### Updating or Adding Emojis

If you need to add new emojis or update existing ones:

1. Place your emoji sprite images in a folder (PNG format, named with Unicode codepoints)
2. Go to `Tools > Emoji > Generate TMP Sprite Asset`
3. Select your sprite folder as the source
4. Configure atlas size and sprite resolution
5. Click `Generate` to create/update your sprite asset

The generator supports automatic detection of:
- Single emojis (`1f600.png` → 😀)
- ZWJ sequences (`1f468-200d-1f469-200d-1f467.png` → 👨‍👩‍👧)
- Skin tones (`1f44b-1f3fb.png` → 👋🏻)

## Native Mobile Input (Recommended)

For optimal mobile input field experience, we recommend using [**UMI (Unity Mobile Input)**](https://github.com/mopsicus/umi.git) alongside this package.

### Why UMI?

- **Native keyboard integration** on iOS and Android
- Proper handling of composite emoji input from system keyboards
- Improved mobile text input experience

### Installation

Add UMI via Package Manager:
```json
{
  "dependencies": {
    "com.mopsicus.umi": "https://github.com/mopsicus/umi.git"
  }
}
```

UMI is **optional** but highly recommended for production mobile applications.

## API Reference

### EmojiReplacer
```csharp
// Initialize with sprite asset (call once at startup)
EmojiReplacer.Initialize(TMP_SpriteAsset spriteAsset);

// Replace emojis in text
string replaced = EmojiReplacer.Replace(string text);

// Check if text contains emojis
bool hasEmojis = EmojiReplacer.ContainsEmoji(string text);
```

## Performance

- **Fast tokenization**: ~0.1ms for typical chat messages (100 characters)
- **Memory efficient**: Minimal allocations during replacement
- **Mobile optimized**: Designed for 60fps+ on mobile devices
- **Sprite atlasing**: Single draw call for all emojis in a text component

## Requirements

- Unity 2021.3 or higher
- TextMeshPro 3.0.6 or higher

## License

MIT License - See LICENSE file for details

## Credits

- Emoji graphics in samples: [Twemoji by Twitter](https://github.com/twitter/twemoji) (CC-BY 4.0)
- Recommended mobile input: [UMI by Mopsicus](https://github.com/mopsicus/umi)

## Support

For issues, questions, or contributions, please visit:
- GitHub Issues: [Your Repository URL]
- Documentation: [Your Docs URL]

---

Made with ❤️ for the Unity community