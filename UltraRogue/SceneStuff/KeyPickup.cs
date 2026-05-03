using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrarogue.SceneStuff
{
    public class KeyPickup : MonoBehaviour
    {
        bool pickedUp = false;
        void Update()
        {
            if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= 2f && !pickedUp)
            {
                pickedUp = true;
                RogueDifficultyManager.Instance.Keys++;
                Destroy(gameObject);
            }
        }

        public static void CreatePickup(Vector3 position)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pickup.GetComponent<Collider>().enabled = false;
            pickup.AddComponent<KeyPickup>();
            Material mat = new Material(DefaultReferenceManager.Instance.masterShader);
            pickup.GetComponent<MeshRenderer>().material = mat;
            pickup.transform.position = position + Vector3.up * 3;
            pickup.transform.localScale *= 3;
        }
    }
}
