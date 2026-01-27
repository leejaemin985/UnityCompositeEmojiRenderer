This sample does not include emoji artwork.

To test composite emoji rendering:

1. Download emoji artwork (e.g. Twemoji)
   https://github.com/twitter/twemoji

2. Import the desired emoji images into your Unity project.

3. Use the provided editor tool to generate a TextMeshPro Sprite Asset:
   - Open the menu: Tools → Emoji → Generate TMP Sprite Asset
   - Select the imported emoji texture folder
   - Generate a TMP Sprite Asset

4. Assign the generated Sprite Asset as a fallback sprite asset in TextMeshPro,
   then use this package to render composite emojis.