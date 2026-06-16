using UnityEngine;

/// <summary>
/// POP ADVENTURE - LoadingSpinner
/// Gira o disco de loading continuamente.
/// Adicione no objeto Image do spinner dentro do LoadingScreen.
/// </summary>
public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f; // graus/segundo

    void Update()
    {
        transform.Rotate(Vector3.forward, -rotateSpeed * Time.deltaTime);
    }
}
