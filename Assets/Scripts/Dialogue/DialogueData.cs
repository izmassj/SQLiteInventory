using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Scriptable Object/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Dialogues")]
    [TextArea(2, 10)]
    public string[] pages;

    [Header("Yes / No Choice")]
    public bool hasChoice;
    public int choicePageIndex;
    public UnityEvent onYes;
    public UnityEvent onNo;

    [Header("Dialogue Finished")]
    public UnityEvent onDialogueEnd;
}