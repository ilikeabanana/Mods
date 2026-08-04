using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class KeyEater : MonoBehaviour // Yum
{
    public UnityEvent OnKeyGotten;
    public UnityEvent OnNoKey;

    float DistanceForActivation = 2f;

    float cooldown = 1;
    float c = 0;

    bool canUse = true;

    float chanceToPayOut = 0.05f;

    float chanceToExplode = 0.01f;

    void Update()
    {
        c -= Time.deltaTime;
        if (Vector3.Distance(NewMovement.Instance.transform.position, transform.position) <= DistanceForActivation &&
            c <= 0 && canUse)
        {

            if(RogueDifficultyManager.Instance.Keys <= 0)
            {
                c = 2; // Purely so that it doesnt activate it constantly
                OnNoKey?.Invoke();
                HudMessageReceiver.Instance.SendHudMessage("NO KEYS TO GIVE");
                return;
            }
            KeyThings();
            OnKeyGotten?.Invoke();
            canUse = false;
        }
    }
    Transform getPlc()
    {
        Vector3 itemPos = transform.position;
        GameObject plc = new GameObject("ItemDropAnchor");
        plc.transform.position = itemPos;
        plc.transform.parent = transform;
        plc.transform.position += transform.forward * 2f;
        return plc.transform;
    }


    void KeyThings()
    {
        RogueDifficultyManager.Instance.Keys--;

        chanceToPayOut += 0.13f;
        if (RogueDifficultyManager.KeyEaterRNG.NextDouble() <= chanceToPayOut)
        {
            chanceToPayOut = 0.05f;
            GameObject chest = Chest.CreateChest(getPlc(), 1);
             
            StartCoroutine(ApplyForce(chest.GetComponent<Rigidbody>()));
        }


    }

    IEnumerator ApplyForce(Rigidbody rb)
    {
        yield return new WaitForSeconds(0.5f);

        // Random horizontal arc between -35 and +35 degrees.
        float randomAngle = UnityEngine.Random.Range(-35f, 35f);

        // Rotate the forward direction by the random angle.
        Vector3 launchDirection = Quaternion.Euler(0f, randomAngle, 0f) * transform.forward;

        rb.AddForce(launchDirection * 150f, ForceMode.VelocityChange);
    }

    public void AllowUsageAgain()
    {
        canUse = true;
    }
}
