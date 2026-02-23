using System.Collections;
using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DialogueData _dialogue;
    [SerializeField] private DialogueUIController _dialogueController;

    [Header("Settings")]
    [SerializeField] private float _gridSize = 1f;

    private bool _isInteracting;

    public void TryInteract(PlayerMovementController player)
    {
        if (_isInteracting) return;

        StartCoroutine(StartInteraction(player));
    }

    private IEnumerator StartInteraction(PlayerMovementController player)
    {
        _isInteracting = true;

        player.StopMovementCoroutine();
        player.SetCurrentPlayerState(PlayerState.Dialogue);

        yield return new WaitForSeconds(0.1f);

        _dialogueController.StartDialogue(_dialogue);

        StartCoroutine(WaitForDialogueEnd(player));
    }

    private IEnumerator WaitForDialogueEnd(PlayerMovementController player)
    {
        while (_dialogueController.IsActive())
            yield return null;

        _isInteracting = false;
    }
}