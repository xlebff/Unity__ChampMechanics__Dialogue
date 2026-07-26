using System.Collections;
using UnityEngine;

public class FastShowAndHide : MonoBehaviour
{
    private static readonly int HideHash = Animator.StringToHash("Hide");
    private static readonly int ShowHash = Animator.StringToHash("Show");

    [SerializeField] private GameObject child;
    [SerializeField] private Animator animator;

    private Coroutine activeCoroutine;

    public void Show()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        child.SetActive(true);
        animator.Play(ShowHash);
    }

    public void Hide()
    {
        activeCoroutine = StartCoroutine(Hiding());
    }

    private IEnumerator Hiding()
    {
        animator.Play(HideHash);
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float duration = stateInfo.length;
        yield return new WaitForSeconds(duration);
        child.SetActive(false);

        activeCoroutine = null;
    }
}
