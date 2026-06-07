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

        public override bool CanApply()
        {
            return RogueDifficultyManager.Instance.floor >= 9;
        }
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
                yield return null;

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

            eid.Bless();
            return gabriel;
        }
    }
}
