using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CompositeEmoji.Sample
{
    public class SampleLogic : MonoBehaviour
    {
        [SerializeField] private TMP_SpriteAsset spriteAsset;

        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text inputFieldTMP;

        private bool isUpdating;

        private void Start()
        {
            EmojiReplacer.Initialize(spriteAsset);
            inputField.onValueChanged.AddListener(SetInputFieldText);
        }

        private void SetInputFieldText(string text)
        {
            string replaced = EmojiReplacer.Replace(text);
            inputFieldTMP.text = replaced;
        }
    }
}
