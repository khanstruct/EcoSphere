using TMPro;
using UnityEngine;

namespace EcoSphere
{
    public sealed class LaserSphere : MonoBehaviour
    {
        public Transform mechanism;
        public TextMeshPro countdown;
        public int turns = 3;
        public int placedTurn;
        public int directionIndex;
        public Vector2 LocalDirection => Quaternion.Euler(0, 0, -60 * directionIndex) * Vector2.right;

        public void RotateClockwise()
        {
            directionIndex = (directionIndex + 1) % 6;
            mechanism.localRotation = Quaternion.Euler(0, 0, -90 - 60 * directionIndex);
        }

        void LateUpdate()
        {
            countdown.text = turns.ToString();
            countdown.transform.rotation = Quaternion.identity;
        }
    }
}
