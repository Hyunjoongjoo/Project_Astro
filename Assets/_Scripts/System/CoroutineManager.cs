using UnityEngine;
using System.Collections.Generic;

//코루틴 캐싱 클래스
public class CoroutineManager
{
    public static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();

    private static readonly Dictionary<float, WaitForSeconds> _waitForSeconds = new Dictionary<float, WaitForSeconds>();

    public static WaitForSeconds waitForSeconds(float seconds)
    {
        if (!_waitForSeconds.TryGetValue(seconds, out var _seconds))
        {
            _waitForSeconds.Add(seconds, _seconds = new WaitForSeconds(seconds));
        }
        return _seconds;
    }
}
