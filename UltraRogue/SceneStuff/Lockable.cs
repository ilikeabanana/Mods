using UnityEngine;

namespace Ultrarogue.SceneStuff
{
    public class Lockable : MonoBehaviour
    {
        public bool locked = true;
        void Awake()
        {
            if (gameObject.TryGetComponent<Door>(out Door door))
            {
                door.Lock();
            }
        }
        void Update()
        {
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f && locked && RogueDifficultyManager.Instance.Keys >= 1)
            {
                locked = false;
                RogueDifficultyManager.Instance.Keys--;
                if(gameObject.TryGetComponent<Door>(out Door door))
                {
                    door.Unlock();
                }
            }
        }
    }
}
