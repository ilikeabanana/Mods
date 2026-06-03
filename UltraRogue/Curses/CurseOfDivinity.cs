using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ultrarogue.Curses
{
    public class CurseOfDivinity : BaseCurse
    {
        public override string CurseName => "Curse of Divinity";
        GameObject gabriel;
        public override void OnApply()
        {
            RogueDifficultyManager.Instance.StartCoroutine(SpawnGabe());
        }
        public override void OnRemove()
        {
            if(gabriel != null)
            {
                GameObject.Destroy(gabriel);
            }
        }
        IEnumerator SpawnGabe()
        {
            GameObject gabeprefab = AssetsManager.GetEnemiesOfType(EnemyType.Gabriel)[0].gameObject;
            yield return new WaitForSeconds(2.5f);

            gabriel = GameObject.Instantiate(gabeprefab, NewMovement.Instance.transform.position, Quaternion.identity);
            Enemy e = Room.FindEnemyComponent(gabriel);
            EnemyIdentifier eid = gabriel.GetComponent<EnemyIdentifier>();
            eid.health = int.MaxValue;
            e.health = int.MaxValue;
            e.originalHealth = int.MaxValue;
        }
    }
}
