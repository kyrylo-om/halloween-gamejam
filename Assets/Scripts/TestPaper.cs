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

    [Header("Writing")]
    [Tooltip("How fast the pen writes out a word, in characters per second.")]
    public float charsPerSecond = 30f;

    // Progress properties for ExamManager / UI
    public int WordsWritten => wordsWritten;
    public int TotalWords => wordEnds.Count;
    public float Progress => wordEnds.Count > 0 ? (float)wordsWritten / wordEnds.Count : 0f;
    public bool IsWriting => writeEnd >= 0;

    // Character index of the last character of each word
    private readonly List<int> wordEnds = new List<int>();
    private int wordsWritten = 0;

    // Pen progress through the word being written, in characters
    private float writeCursor;
    private int writeEnd = -1;

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

        // Finish the previous word instantly if the player clicks faster than the pen writes
        if (writeEnd >= 0) text.maxVisibleCharacters = writeEnd + 1;

        TMP_CharacterInfo[] chars = text.textInfo.characterInfo;
        int start = wordsWritten == 0 ? 0 : wordEnds[wordsWritten - 1] + 1;
        while (char.IsWhiteSpace(chars[start].character)) start++;

        writeEnd = wordEnds[wordsWritten];
        writeCursor = start;
        wordsWritten++;

        // Lift the pen over to the start of the word; Update then drags it along the letters
        hand.MoveTo(PenPoint(start, 0f));

        // Trigger victory if all words from the answer sheet are written!
        if (wordsWritten >= wordEnds.Count)
        {
            if (ExamManager.Instance != null)
            {
                ExamManager.Instance.TriggerWin();
            }
        }
    }

    void Update()
    {
        if (writeEnd < 0) return;

        writeCursor = Mathf.MoveTowards(writeCursor, writeEnd + 1, charsPerSecond * Time.deltaTime);

        // Each letter appears as the pen starts on it
        int i = Mathf.Min((int)writeCursor, writeEnd);
        text.maxVisibleCharacters = Mathf.CeilToInt(writeCursor);
        hand.Track(PenPoint(i, writeCursor - i));

        if (writeCursor >= writeEnd + 1) writeEnd = -1;
    }

    // World point on the baseline, t of the way across character i
    private Vector3 PenPoint(int i, float t)
    {
        TMP_CharacterInfo c = text.textInfo.characterInfo[i];
        Vector3 local = new Vector3(Mathf.Lerp(c.bottomLeft.x, c.bottomRight.x, t), c.baseLine, 0f);
        return text.transform.TransformPoint(local);
    }
}
