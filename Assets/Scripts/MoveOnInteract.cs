using UnityEngine;

/// <summary>
/// Interactable that toggles the object between two positions.
/// Clicking moves it up, clicking again moves it back down.
/// </summary>
public class MoveOnInteract : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "[LMB] Move Cube";

    public Vector3 moveOffset = new Vector3(0f, 2f, 0f);
    public float moveSpeed = 3f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movedUp = false;
    private bool isMoving = false;

    void Start()
    {
        startPosition = transform.position;
        targetPosition = startPosition;
    }

    public void Interact()
    {
        if (isMoving) return;

        movedUp = !movedUp;
        targetPosition = movedUp ? startPosition + moveOffset : startPosition;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            isMoving = true;
            transform.position = Vector3.MoveTowards(
                transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            isMoving = false;
            transform.position = targetPosition;
        }
    }
}

