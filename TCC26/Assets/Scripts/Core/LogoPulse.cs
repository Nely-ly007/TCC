using System.Collections;
using UnityEngine;

public class LogoPulse : MonoBehaviour
{
    void OnEnable()  => RhythmManager.OnBeatStatic += Pulse;
    void OnDisable() => RhythmManager.OnBeatStatic -= Pulse;

    void Pulse() => StartCoroutine(ScaleBounce());

    IEnumerator ScaleBounce() {
        float t = 0;
        while (t < 0.12f) {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, t / 0.12f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
