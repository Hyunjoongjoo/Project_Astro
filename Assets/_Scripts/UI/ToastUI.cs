using System.Collections;
using UnityEngine;

public class ToastUI : BaseUI
{
    public void Show(float delay = 2.0f)
    {
        Open();

        StopAllCoroutines();
        StartCoroutine(CO_CloseAfterDelay(delay));
    }

    private IEnumerator CO_CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Close(false); 
    }
}
