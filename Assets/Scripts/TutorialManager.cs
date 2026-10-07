using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("UI references")]
    public GameObject tutorialPanel;         
    public TextMeshProUGUI tutorialText;     

    [Header("Časy")]
    public float moveToJumpDelay = 3f;        
    public float jumpToInteractDelay = 3f;    
    public float interactVisibleDuration = 3f; 

    private enum Step
    {
        Move,
        Jump,
        Interact,
        Done
    }

    private Step currentStep = Step.Move;
    private bool isActive = false;
    private bool moveDetected = false;
    private bool jumpDetected = false;
    private Coroutine runningCoroutine;

    void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        switch (currentStep)
        {
            case Step.Move:
                HandleMoveStep();
                break;
            case Step.Jump:
                HandleJumpStep();
                break;
            case Step.Interact:
                break;
            case Step.Done:
                break;
        }
    }

    public void StartTutorial()
    {
        isActive = true;
        currentStep = Step.Move;
        moveDetected = false;
        jumpDetected = false;

        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        ShowMoveText();
    }

    void HandleMoveStep()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (!moveDetected && (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f))
        {
            moveDetected = true;

            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);

            runningCoroutine = StartCoroutine(MoveToJumpTimer());
        }
    }

    System.Collections.IEnumerator MoveToJumpTimer()
    {
        yield return new WaitForSeconds(moveToJumpDelay);
        currentStep = Step.Jump;
        ShowJumpText();
        runningCoroutine = null;
    }

    void HandleJumpStep()
    {
        if (!jumpDetected && Input.GetKeyDown(KeyCode.Space))
        {
            jumpDetected = true;

            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);

            runningCoroutine = StartCoroutine(JumpToInteractTimer());
        }
    }

    System.Collections.IEnumerator JumpToInteractTimer()
    {
        yield return new WaitForSeconds(jumpToInteractDelay);

        currentStep = Step.Interact;
        ShowInteractText();

        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);

        runningCoroutine = StartCoroutine(HideAfterInteract());
    }

    System.Collections.IEnumerator HideAfterInteract()
    {
        yield return new WaitForSeconds(interactVisibleDuration);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        currentStep = Step.Done;
        isActive = false;
        runningCoroutine = null;
    }

    void ShowMoveText()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null)
            tutorialText.text = "Find the green glowing book";
    }

    void ShowJumpText()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null)
            tutorialText.text = "Click to kill enemies";
    }

    void ShowInteractText()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialText != null)
            tutorialText.text = "Press i to open book";
    }
}
