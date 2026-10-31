using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The test sheet. Each click reveals the next word of the essay
/// and moves the hand to the end of that word. Only works while the answer paper is out
/// and the player has enough stamina to write.
/// </summary>
public class TestPaper : MonoBehaviour, IInteractable
{
    public string InteractionPrompt
    {
        get
        {
            if (stamina != null && stamina.IsExhausted)
                return "Hand exhausted! Rest to recover...";
            return "";
        }
    }

    public TextAsset essay;
    public TMP_Text text;
    public Hand hand;
    public AnswerPaper answerPaper;

    [Header("Stamina Settings")]
    public StaminaSystem stamina;
    public float staminaCostPerWord = 12f;

    // Progress properties for ExamManager / UI
    public int WordsWritten => wordsWritten;
    public int TotalWords => wordEnds.Count;
    public float Progress => wordEnds.Count > 0 ? (float)wordsWritten / wordEnds.Count : 0f;

    // Character index of the last character of each word
    private readonly List<int> wordEnds = new List<int>();
    private int wordsWritten = 0;

    void Start()
    {
        if (stamina == null)
        {
            stamina = StaminaSystem.Instance;
        }

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
        // Don't write if exam has ended (Won, Time Up, or Caught)
        if (ExamManager.Instance != null && (ExamManager.Instance.IsGameOver || ExamManager.Instance.IsWon))
            return;

        if (!answerPaper.IsOut) return;
        if (wordsWritten >= wordEnds.Count) return;

        // Check and apply stamina cost
        if (stamina == null) stamina = StaminaSystem.Instance;

        if (stamina != null)
        {
            if (!stamina.CanWrite(staminaCostPerWord))
            {
                if (hand != null) hand.TriggerExhaustedShake();
                return;
            }

            stamina.UseStamina(staminaCostPerWord);
        }

        int lastChar = wordEnds[wordsWritten];
        wordsWritten++;
        text.maxVisibleCharacters = lastChar + 1;

        Vector3 wordEnd = text.textInfo.characterInfo[lastChar].bottomRight;
        hand.MoveTo(text.transform.TransformPoint(wordEnd));

        // Trigger victory if all words from the answer sheet are written!
        if (wordsWritten >= wordEnds.Count)
        {
            if (ExamManager.Instance != null)
            {
                ExamManager.Instance.TriggerWin();
            }
        }
    }
}
