using UnityEngine;
using System.Collections;

/// <summary>
/// Microfone que concede uma vida extra ao jogador.
/// Também informa a HUD para mostrar o ícone do microfone.
/// </summary>
public class CollectibleMicrophone : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;

    [Header("Effect")]
    [SerializeField] private ParticleSystem collectEffect;

    [Header("Animation")]
    [SerializeField] private float floatHeight = 0.15f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 30f;

    private bool isCollected;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        StartCoroutine(FloatEffect());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        if (!other.CompareTag("Player"))
            return;

        isCollected = true;

        // Dá a vida extra ao jogador
        PlayerController.Instance?.PickupMicrophone();

        // Mostra o microfone na HUD
        HUDUIToolkit.Instance?.ShowMicrophone();

        // Som
        if (collectSFX != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSFX,
                transform.position
            );
        }

        // Efeito
        if (collectEffect != null)
        {
            ParticleSystem fx = Instantiate(
                collectEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(fx.gameObject, 2f);
        }

        Destroy(gameObject);
    }

    private IEnumerator FloatEffect()
    {
        float time = 0f;

        while (!isCollected)
        {
            time += Time.deltaTime;

            transform.position =
                startPosition +
                Vector3.up *
                Mathf.Sin(time * floatSpeed) *
                floatHeight;

            transform.Rotate(
                Vector3.forward,
                rotationSpeed * Time.deltaTime
            );

            yield return null;
        }
    }
}
