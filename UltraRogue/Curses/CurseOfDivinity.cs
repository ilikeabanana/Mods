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
        bool _happenin;
        public override void OnApply()
        {
            RogueDifficultyManager.Instance.StartCoroutine(SpawnGabe());
            _happenin = true;
        }
        public override void OnRemove()
        {
            _happenin = false;
        }
        IEnumerator SpawnGabe()
        {
            
            yield return new WaitForSeconds(2.5f);

            GameObject gab = CreateGabe();

            while (_happenin)
            {
                if (gab == null)
                    gab = CreateGabe();

                if (gab.GetComponent<EnemyIdentifier>() != null && gab.GetComponent<EnemyIdentifier>().dead)
                    gab = CreateGabe();
            }

            GameObject.Destroy(gab);
        }
        GameObject CreateGabe()
        {
            GameObject gabeprefab = AssetsManager.GetEnemiesOfType(EnemyType.Gabriel)[0].gameObject;
            GameObject gabriel = GameObject.Instantiate(gabeprefab, NewMovement.Instance.transform.position, Quaternion.identity);
            Enemy e = Room.FindEnemyComponent(gabriel);
            EnemyIdentifier eid = gabriel.GetComponent<EnemyIdentifier>();
            eid.health = int.MaxValue;
            e.health = int.MaxValue;
            e.originalHealth = int.MaxValue;
            return gabriel;
        }
    }
}
