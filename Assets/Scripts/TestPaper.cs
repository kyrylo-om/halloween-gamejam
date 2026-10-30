using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The test sheet. Each click reveals the next word of the essay
/// and moves the hand to the end of that word. Only works while the answer paper is out.
/// </summary>
public class TestPaper : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "";

    public TextAsset essay;
    public TMP_Text text;
    public Hand hand;
    public AnswerPaper answerPaper;

    // Character index of the last character of each word
    private readonly List<int> wordEnds = new List<int>();
    private int wordsWritten = 0;

    void Start()
    {
        // The whole essay is laid out up front and revealed character by character,
        // so words never reflow as you write
        text.text = essay.text;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();

        TMP_CharacterInfo[] chars = text.textInfo.characterInfo;
        int count = text.textInfo.characterCount;
        for (int i = 0; i < count; i++)
        {
            bool isEnd = !char.IsWhiteSpace(chars[i].character) &&
                         (i == count - 1 || char.IsWhiteSpace(chars[i + 1].character));
            if (isEnd) wordEnds.Add(i);
        }
    }

    public void Interact()
    {
        if (!answerPaper.IsOut) return;
        if (wordsWritten >= wordEnds.Count) return;

        int lastChar = wordEnds[wordsWritten];
        wordsWritten++;
        text.maxVisibleCharacters = lastChar + 1;

        Vector3 wordEnd = text.textInfo.characterInfo[lastChar].bottomRight;
        hand.MoveTo(text.transform.TransformPoint(wordEnd));
    }
}
