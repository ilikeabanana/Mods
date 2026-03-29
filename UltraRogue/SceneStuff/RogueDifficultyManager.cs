using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// This is actually just the rogue manager... but eh, too lazy to change lol
public class RogueDifficultyManager : MonoBehaviour
{
    public static RogueDifficultyManager Instance { get; private set; }

    public float Difficulty;

    public int Gold;

    float difficultyScaleMult = 1f;

    void Awake()
    {
        Instance = this;
        Difficulty = 1;
    }

    void Update()
    {
        Difficulty += (Time.deltaTime / 180) * difficultyScaleMult;
    }

    public void MoveStage()
    {
        Difficulty *= 1.2f;
        difficultyScaleMult *= 1.34f * ((PlayerPrefs.GetInt("difficulty", 1) + 1) / 3);
        // Harmless = 0,3333333333333333 = 0,4466666666666666 per stage
        // Lenient = 0,6666666666666667 = 0,8933333333333334 per stage
        // Standard = 1 = 1.34 per stage
        // Violent = 1,333333333333333 = 1,786666666666666 per stage
        // Brutal = 1,666666666666667 = 2,233333333333334 per stage
        // UKMD = 2 = 2.68 per stage
    }

    public int GetCountBeforeRadiance(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Filth:
                return 10;
            case EnemyType.Stray:
            case EnemyType.Schism:
            case EnemyType.Streetcleaner:
            case EnemyType.Drone:
                return 5;
            case EnemyType.Idol:
            case EnemyType.Deathcatcher:
                return 3;
            case EnemyType.Turret:
            case EnemyType.Cerberus:
            case EnemyType.Virtue:
            case EnemyType.Gutterman:
            case EnemyType.Guttertank:
                return 4;
        }
        return 2;
    }

    public int GetCost(EnemyType enemyType)
    {
        switch(enemyType)
        {
            case EnemyType.Filth:
                return 1;
            case EnemyType.Stray:
                return 2;
            case EnemyType.Schism:
                return 5;
            case EnemyType.Streetcleaner:
                return 5;
            case EnemyType.Cerberus:
                return 16;
            case EnemyType.Swordsmachine:
                return 20;
            case EnemyType.Drone:
                return 4;
            case EnemyType.HideousMass:
                return 40;
            case EnemyType.V2:
                return 175;
            case EnemyType.V2Second:
                return 200;
            case EnemyType.SisyphusPrime:
                return 400;
            case EnemyType.MinosPrime:
                return 350;
            case EnemyType.Minotaur:
                return 250;
            case EnemyType.Gabriel:
                return 325;
            case EnemyType.GabrielSecond:
                return 335;
            case EnemyType.Soldier:
                return 6;
            case EnemyType.Mindflayer:
                return 55;
            case EnemyType.Sisyphus:
                return 75;
            case EnemyType.Providence:
                return 30;
            case EnemyType.Turret:
                return 30;
            case EnemyType.Stalker:
                return 20;
            case EnemyType.Gutterman:
                return 45;
            case EnemyType.Virtue:
                return 20;
            case EnemyType.Idol:
                return 30;
            case EnemyType.Deathcatcher:
                return 50;
            case EnemyType.Guttertank:
                return 40;
            case EnemyType.Mannequin:
                return 25;
            case EnemyType.MirrorReaper:
                return 125;
            case EnemyType.Ferryman:
                return 55;
            case EnemyType.MaliciousFace:
                return 13;
            case EnemyType.Power:
                return 100;
            
        }
        return int.MaxValue - 1;
    }

}

