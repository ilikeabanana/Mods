using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class LogicButtocksHaHa : MonoBehaviour
{
    private Transform modelParent;
    private Animator animator;
    private List<AnimationClip> animationClips;
    private RectTransform uiRect;
    private Vector2 lastMousePos;
    private Vector3 lastRotationDirection = Vector3.up;
    private float lastInteractionTime;
    private bool isDragging = false;
    private bool isClick = false;
    private const float clickThreshold = 5f;
    private bool isSpinning = true;
    private float lastPoseChangeTime = 0f;
    private const float poseChangeCooldown = 0.2f;

    private Vector3 originalScale = new Vector3(0.7f, 0.7f, 1f);
    private Vector3 targetScale;
    private float scaleLerpSpeed = 2f;
    private float scaleHoldTime = 0f;
    private float scaleStartTime = 0f;

    private AnimationClip lastPlayedClip;
    private int retryCount = 0;

    private PlayableGraph playableGraph;
    private AnimationPlayableOutput playableOutput;
    private AnimationMixerPlayable mixerPlayable;
    private AnimationClipPlayable posePlayable;

    public void Initialize(Transform modelParent, Animator animator, List<AnimationClip> animationClips, RectTransform uiRect)
    {
        this.modelParent = modelParent;
        this.animator = animator;
        this.animationClips = animationClips;
        this.uiRect = uiRect;
        lastInteractionTime = Time.time;
        targetScale = originalScale;

        SetupPlayables();
    }

    private void Awake()
    {
        lastInteractionTime = Time.time;
        isSpinning = true;
        targetScale = originalScale;
    }

    private void SetupPlayables()
    {
        playableGraph = PlayableGraph.Create("PoseGraph");
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
        mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 1);
        playableOutput.SetSourcePlayable(mixerPlayable);

        playableGraph.Play();
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

                if (animationClips.Count > 0 && Time.time - lastPoseChangeTime > poseChangeCooldown)
                {
                    AnimationClip chosenClip = ChooseAnimationClip();
                    if (chosenClip != null)
                    {
                        SwapAnimationClip(chosenClip);
                        lastInteractionTime = Time.time;
                        isSpinning = false;
                        lastPoseChangeTime = Time.time;
                        lastPlayedClip = chosenClip;
                    }
                }
            }
        }

        if (targetScale != originalScale && Time.time - scaleStartTime > scaleHoldTime)
            targetScale = originalScale;

        if (!isDragging && Time.time - lastInteractionTime > 10f)
            isSpinning = true;

        if (isSpinning)
            modelParent.Rotate(lastRotationDirection, Time.deltaTime * 10f);
    }

    private AnimationClip ChooseAnimationClip()
    {
        if (animationClips.Count == 1)
            return animationClips[0];

        retryCount = 0;
        while (retryCount < 3)
        {
            AnimationClip chosenClip = animationClips[Random.Range(0, animationClips.Count)];
            if (chosenClip != lastPlayedClip)
                return chosenClip;
            retryCount++;
        }
        return null;
    }

    private void SwapAnimationClip(AnimationClip newClip)
    {
        if (newClip == null) return;

        if (posePlayable.IsValid())
        {
            posePlayable.Destroy();
        }

        posePlayable = AnimationClipPlayable.Create(playableGraph, newClip);
        playableGraph.Connect(posePlayable, 0, mixerPlayable, 0);
        mixerPlayable.SetInputWeight(0, 1f);

        posePlayable.Play();
    }

    private void OnDestroy()
    {
        if (playableGraph.IsValid())
        {
            playableGraph.Destroy();
        }
    }
}