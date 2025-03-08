using UnityEngine;
using UnityEngine.UI;

public class LogicButtocksHaHa : MonoBehaviour
{
    private Transform modelParent;
    private Animator animator;
    private RectTransform uiRect;
    private Vector2 lastMousePos;
    private Vector3 lastRotationDirection = Vector3.up;
    private float lastInteractionTime;
    private bool isDragging = false;
    private bool isClick = false;
    private const float clickThreshold = 5f;
    private bool isSpinning = true;

    private Vector3 originalScale = new Vector3(0.7f, 0.7f, 1f);
    private Vector3 targetScale;
    private float scaleLerpSpeed = 2f;
    private float scaleHoldTime = 0f;
    private float scaleStartTime = 0f;

    public void Initialize(Transform modelParent, Animator animator, RectTransform uiRect)
    {
        this.modelParent = modelParent;
        this.animator = animator;
        this.uiRect = uiRect;
        lastInteractionTime = Time.time;
        targetScale = originalScale;
    }

    private void Awake()
    {
        lastInteractionTime = Time.time;
        isSpinning = true;
        targetScale = originalScale;
    }

    private bool IsMouseOverUI()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRect, Input.mousePosition, null, out Vector2 localMousePos);
        return uiRect.rect.Contains(localMousePos);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);

        if (IsMouseOverUI())
        {
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePos = Input.mousePosition;
                lastInteractionTime = Time.time;
                isClick = true;
            }

            if (Input.GetMouseButton(0))
            {
                Vector2 currentMousePos = Input.mousePosition;
                float deltaX = currentMousePos.x - lastMousePos.x;

                if (isClick && Vector2.Distance(currentMousePos, lastMousePos) > clickThreshold)
                    isClick = false;

                if (!isClick)
                {
                    lastRotationDirection = deltaX > 0 ? Vector3.up : Vector3.down;
                    modelParent.Rotate(Vector3.up, deltaX * 0.5f);
                    isDragging = true;
                    lastInteractionTime = Time.time;
                    isSpinning = false;
                }

                lastMousePos = currentMousePos;
            }
            else isDragging = false;

            if (Input.GetMouseButtonUp(0) && isClick)
            {
                targetScale = originalScale * 1.25f;
                scaleStartTime = Time.time;
            }
        }

        if (targetScale != originalScale && Time.time - scaleStartTime > scaleHoldTime)
            targetScale = originalScale;

        if (!isDragging && Time.time - lastInteractionTime > 10f)
            isSpinning = true;

        if (isSpinning)
            modelParent.Rotate(lastRotationDirection, Time.deltaTime * 10f);
    }
}