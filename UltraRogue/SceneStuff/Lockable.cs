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
            Transform keyLock = transform.parent.Find("LockObject");

            if(keyLock != null)
                keyLock.gameObject.SetActive(true);
        }
        void Update()
        {
            if (Room.isFighting) return;
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f && locked && RogueDifficultyManager.Instance.Keys >= 1)
            {
                locked = false;
                RogueDifficultyManager.Instance.Keys--;
                if(gameObject.TryGetComponent<Door>(out Door door))
                {
                    door.Unlock();
                }
                Transform keyLock = transform.parent.Find("LockObject"); 

                if (keyLock != null)
                    keyLock.gameObject.SetActive(false);

                Instantiate(AssetsManager.BreakParticle, transform.position, Quaternion.identity);
            }
        }
    }
}
