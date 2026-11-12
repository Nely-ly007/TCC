using UnityEngine;

namespace PopAdventure.Fase3
{
    /// <summary>
    /// Portão que desliza para cima ao abrir. Usado nas grades da arena do boss e na porta de ressonância.
    /// A posição em que você deixa o "leaf" no editor é a posição FECHADA.
    /// </summary>
    public class GGGate : MonoBehaviour
    {
        [SerializeField] Transform leaf;
        [SerializeField] Collider2D blocker;
        [SerializeField] Vector3 openOffset = new Vector3(0f, 4f, 0f);
        [SerializeField] float speed = 7f;
        [SerializeField] bool startClosed = false;

        Vector3 closedPos;
        bool closed;

        public bool IsClosed { get { return closed; } }

        void Awake()
        {
            if (leaf == null) leaf = transform;
            closedPos = leaf.localPosition;
            closed = startClosed;
            if (!closed) leaf.localPosition = closedPos + openOffset;
            if (blocker != null) blocker.enabled = closed;
        }

        public void SetClosed(bool value)
        {
            closed = value;
            if (blocker != null && value) blocker.enabled = true;
        }

        void Update()
        {
            Vector3 target = closed ? closedPos : closedPos + openOffset;
            leaf.localPosition = Vector3.MoveTowards(leaf.localPosition, target, speed * Time.deltaTime);
            if (blocker != null && !closed && (leaf.localPosition - target).sqrMagnitude < 0.0004f)
                blocker.enabled = false;
        }
    }
}
