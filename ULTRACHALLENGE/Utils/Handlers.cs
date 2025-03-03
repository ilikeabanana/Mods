using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace ULTRACHALLENGE.Utils
{
    public class Handlers
    {

        public static bool canUseThing()
        {
            if(MonoSingleton<OptionsManager>.Instance.paused) return false;
            string[] source = new string[]
            {
                "Intro",
                "Bootstrap",
                "Main Menu",
                "Level 2-S",
                "Intermission1",
                "Intermission2"
            };
            return !source.Contains(SceneHelper.CurrentScene);
        }

        public static float GetValueForHandling(float initialAmount, ChallengeSetting setting)
        {
            float toReturn = initialAmount;
            int sceneNum = SceneManager.GetActiveScene().buildIndex;
            switch (setting.weDoALittleMath.value)
            {
                case math.Set:
                    toReturn = setting.amount.value;
                    break;
                case math.Increase:
                    toReturn += setting.amount.value;
                    break;
                case math.Decrease:
                    toReturn -= setting.amount.value;
                    break;
                case math.Divide:
                    toReturn /= setting.amount.value;
                    break;
                case math.Multiply:
                    toReturn *= setting.amount.value;
                    break;
                case math.Complex:
                    toReturn = MathParser.HandleComplexMath(setting.amountComplexMath.value, toReturn, sceneNum);
                    break;
            }
            return toReturn;
        }
        public static void HandleSpeed(ChallengeSetting setting)
        {
            MonoSingleton<NewMovement>.Instance.walkSpeed = GetValueForHandling(MonoSingleton<NewMovement>.Instance.walkSpeed, setting);

            if (setting.saveValue.value)
            {
                setting.savedValue = MonoSingleton<NewMovement>.Instance.walkSpeed;
                setting.ValueAlreadySet = true;
            }
        }

        public static void HandleJump(ChallengeSetting setting)
        {
            MonoSingleton<NewMovement>.Instance.jumpPower = GetValueForHandling(MonoSingleton<NewMovement>.Instance.jumpPower, setting);

            if (setting.saveValue.value)
            {
                setting.savedValue = MonoSingleton<NewMovement>.Instance.jumpPower;
                setting.ValueAlreadySet = true;
            }
        }

        public static float GetMaxValueOfLink(Linkable linkable)
        {
            switch (linkable)
            {
                case Linkable.Health:
                    return 100;
                case Linkable.Speed:
                    return 750;
                case Linkable.JumpHeight:
                    return 90;
                case Linkable.Pixelization:
                    return 720;
                case Linkable.FOV:
                    return 165;
                case Linkable.VertexWarping:
                    return 400;
                case Linkable.Gamma:
                    return 2;
                case Linkable.Velocity:
                    return 16.5f * (MonoSingleton<NewMovement>.Instance.walkSpeed / 750);
                case Linkable.FrameRate:
                    return 288;
                case Linkable.Gravity:
                    return -40f;
                case Linkable.Time:
                    return 1;
                case Linkable.Stamina:
                    return 300;
                case Linkable.Sensitivity:
                    return 200;
                default:
                    return 0;
            }
        }

        public static float getValue(Linkable link)
        {
            switch (link)
            {
                case Linkable.Health:
                    return MonoSingleton<NewMovement>.Instance.hp;
                case Linkable.Speed:
                    return MonoSingleton<NewMovement>.Instance.walkSpeed;
                case Linkable.JumpHeight:
                    return MonoSingleton<NewMovement>.Instance.jumpPower;
                case Linkable.Pixelization:
                    return Shader.GetGlobalFloat("_ResY");
                case Linkable.FOV:
                    return Camera.main.fieldOfView;
                case Linkable.VertexWarping:
                    return Shader.GetGlobalFloat("_VertexWarping");
                case Linkable.Gamma:
                    return Shader.GetGlobalFloat("_Gamma");
                case Linkable.Velocity:
                    return MonoSingleton<PlayerTracker>.Instance.GetPlayerVelocity(true).magnitude;
                case Linkable.FrameRate:
                    return 1f / Time.unscaledDeltaTime;
                case Linkable.Gravity:
                    return Physics.gravity.y;
                case Linkable.Time:
                    return Time.timeScale;
                case Linkable.Stamina:
                    return MonoSingleton<NewMovement>.Instance.boostCharge;
                case Linkable.Sensitivity:
                    return MonoSingleton<OptionsManager>.Instance.mouseSensitivity;
                default:
                    return 0;
            }
        }

        public static float GetValueCalculated(float value, float maxValue, float otherMaxValue, bool reverse)
        {
            float result = (value / maxValue) * otherMaxValue;
            return reverse ? otherMaxValue - result : result;
        }

        public static void setLinkables(Linkable linkable, Linkable link2, bool reverse, float multiplier, float offset)
        {
            if (!canUseThing()) return;
            float sourceMax = GetMaxValueOfLink(linkable);  // Max value of the source (link1)
            float targetMax = GetMaxValueOfLink(link2);     // Max value of the target (link2)
            float sourceValue = getValue(linkable);         // Current value of the source

            // Calculate the proportional value
            float valueEnd = (GetValueCalculated(sourceValue, sourceMax, targetMax, reverse) * multiplier) + offset;
            switch (link2)
            {
                case Linkable.Health:
                    MonoSingleton<NewMovement>.Instance.hp = (int)valueEnd;
                    break;
                case Linkable.Speed:
                    MonoSingleton<NewMovement>.Instance.walkSpeed = valueEnd;
                    break;
                case Linkable.JumpHeight:
                    MonoSingleton<NewMovement>.Instance.jumpPower = valueEnd;
                    break;
                case Linkable.Pixelization:
                    float amount = valueEnd;
                    Shader.SetGlobalFloat("_ResY", amount);
                    PostProcessV2_Handler instance = MonoSingleton<PostProcessV2_Handler>.Instance;
                    if (instance)
                    {
                        instance.downscaleResolution = amount;
                    }
                    DownscaleChangeSprite[] array = UnityEngine.Object.FindObjectsOfType<DownscaleChangeSprite>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].CheckScale();
                    }
                    break;
                case Linkable.FOV:
                    Camera.main.fieldOfView = valueEnd;
                    break;
                case Linkable.VertexWarping:
                    Shader.SetGlobalFloat("_VertexWarping", valueEnd);
                    break;
                case Linkable.Gamma:
                    Shader.SetGlobalFloat("_Gamma", valueEnd);
                    break;
                case Linkable.Velocity:
                    MonoSingleton<NewMovement>.Instance.rb.velocity = MonoSingleton<CameraController>.Instance.transform.forward * valueEnd;
                    break;
                case Linkable.FrameRate:
                    if(QualitySettings.vSyncCount == 1)
                        QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = (int)Math.Ceiling(valueEnd);
                    break;
                case Linkable.Gravity:
                    Physics.gravity = new Vector3(0, valueEnd, 0);
                    break;
                case Linkable.Time:
                    Time.timeScale = valueEnd;
                    break;
                case Linkable.Stamina:
                    MonoSingleton<NewMovement>.Instance.boostCharge = valueEnd;
                    break;
                case Linkable.Sensitivity:
                    MonoSingleton<OptionsManager>.Instance.mouseSensitivity = valueEnd;
                    break;
            }
        }

        public static void HandleThing(ChallengeSetting setting, Transform triggerLocation)
        {
            if (!canUseThing()) return;
            if (setting.challengeType.value != challengeTypes.OnAction) return;
            
            if(setting.ChanceField.value < 100)
            {
                float randomNum = Random.Range(0, 100);
                if (randomNum > setting.ChanceField.value) return;
            }

            Vector3 locationSpawn = setting.spawnLocation.value == spawnLocation.player ? MonoSingleton<NewMovement>.Instance.transform.position : triggerLocation.position;

            switch (setting.whatShouldItDo.value)
            {
                case whatShouldHappen.Damage:
                    MonoSingleton<NewMovement>.Instance.GetHurt(setting.amount.value, false);
                    break;
                case whatShouldHappen.Speed:
                    HandleSpeed(setting);
                    break;
                case whatShouldHappen.JumpPower:
                    HandleJump(setting);
                    break;
                case whatShouldHappen.SpawnExplosion:
                    GameObject.Instantiate(setting.selectedExplosion, locationSpawn, Quaternion.identity);
                    break;
                case whatShouldHappen.KillEnemy:
                    for (int i = 0; i < setting.amount.value; i++)
                    {
                        KillEnemy(setting);
                    }
                    
                    break;
                case whatShouldHappen.BuffEnemy:
                    for (int i = 0; i < setting.amount.value; i++)
                    {
                        BuffEnemy(setting);
                    }
                    
                    break;
                case whatShouldHappen.DupeEnemy:
                    for (int i = 0; i < setting.amount.value; i++)
                    {
                        DupeEnemy(setting);
                    }
                    
                    break;
                case whatShouldHappen.Pixelization:
                    float amount = GetValueForHandling(Shader.GetGlobalFloat("_ResY"), setting);
                    Shader.SetGlobalFloat("_ResY", amount);
                    PostProcessV2_Handler instance = MonoSingleton<PostProcessV2_Handler>.Instance;
                    if (instance)
                    {
                        instance.downscaleResolution = amount;
                    }
                    DownscaleChangeSprite[] array = UnityEngine.Object.FindObjectsOfType<DownscaleChangeSprite>();
                    for (int i = 0; i < array.Length; i++)
                    {
                        array[i].CheckScale();
                    }
                    break;
                case whatShouldHappen.FOV:
                    Camera.main.fieldOfView = GetValueForHandling(Camera.main.fieldOfView, setting);
                    break;
                case whatShouldHappen.VertexWarping:
                    Shader.SetGlobalFloat("_VertexWarping", GetValueForHandling(Shader.GetGlobalFloat("_VertexWarping"), setting));
                    break;
                case whatShouldHappen.Gamma:
                    Shader.SetGlobalFloat("_Gamma", GetValueForHandling(Shader.GetGlobalFloat("_Gamma"), setting));
                    break;
                case whatShouldHappen.ChangeToRandomLevel:
                    SelectRandomLevel();
                    break;
                case whatShouldHappen.ChangeLevel:
                    SceneHelper.LoadScene(GetMissionName.GetSceneName(setting.amount.value), false);
                    break;
                case whatShouldHappen.RestartLevel:
                    MonoSingleton<OptionsManager>.Instance.RestartMission();
                    break;
                case whatShouldHappen.Framerate:
                    if (QualitySettings.vSyncCount == 1)
                        QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = (int)Math.Ceiling(GetValueForHandling(Application.targetFrameRate, setting));
                    break;
                case whatShouldHappen.RemoveGameObject:
                    for (int i = 0; i < setting.amount.value; i++)
                    {
                        RemoveObject(setting);
                    }
                    
                    break;
                case whatShouldHappen.Gravity:
                    Physics.gravity = new Vector3(0, GetValueForHandling(Physics.gravity.y, setting));
                    break;
                case whatShouldHappen.Quit:
                    Application.Quit();
                    break;
                case whatShouldHappen.SpawnAddressable:
                        var loadOperation = Addressables.LoadAssetAsync<GameObject>(ULTRACHALLENGEPlugin.addressables.Find((x) => x.shortPath == setting.addressablePath.value).fullPath);
                        loadOperation.Completed += (operation) =>
                        {
                            if (operation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                            {
                                for (int i = 0; i < setting.amount.value; i++)
                                {
                                    GameObject.Instantiate(operation.Result, locationSpawn, Quaternion.identity);
                                }
                            }
                        };
                    
                    break;
                case whatShouldHappen.RemoveTriggerer:
                    if(triggerLocation != MonoSingleton<NewMovement>.Instance.transform)
                    {
                        GameObject.Destroy(triggerLocation.gameObject);
                    }
                    break;

            }
        }

        static void RemoveObject(ChallengeSetting setting)
        {
            GameObject playerTransform = MonoSingleton<NewMovement>.Instance.gameObject;
            List<GameObject> theObjects = GameObject.FindObjectsOfType<GameObject>().OrderBy((x) => Vector3.Distance(x.transform.position, MonoSingleton<NewMovement>.Instance.transform.position)).ToList();
            GameObject selectedObject = null;
            if (theObjects.Count == 0) return;
            HashSet<GameObject> objectsToRemove = new HashSet<GameObject>();

            if (playerTransform != null)
            {
                // Get all children recursively
                GetAllChildren(playerTransform.transform, objectsToRemove);
                objectsToRemove.Add(playerTransform);
            }

            // Filter out the objects to exclude
            theObjects.RemoveAll(obj => objectsToRemove.Contains(obj.gameObject));

            switch (setting.distanceField.value)
            {
                case distance.Closest:
                    selectedObject = theObjects[0];
                    break;
                case distance.Furthest:
                    selectedObject = theObjects.Last();
                    break;
                case distance.Inbetween:
                    selectedObject = theObjects[Mathf.FloorToInt(theObjects.Count / 2)];
                    break;
                case distance.Random:
                    selectedObject = theObjects[Random.Range(0, theObjects.Count)];
                    break;
            }
            GameObject.Destroy(selectedObject);
        }

        static void GetAllChildren(Transform parent, HashSet<GameObject> list)
        {
            foreach (Transform child in parent)
            {
                list.Add(child.gameObject);
                GetAllChildren(child, list);
            }
        }

        static void KillEnemy(ChallengeSetting setting)
        {
            EnemyIdentifier eid = GetEnemy(setting);
            if(eid != null)
                eid.InstaKill();
        }
        static void DupeEnemy(ChallengeSetting setting)
        {
            EnemyIdentifier eid = GetEnemy(setting);
            if (eid != null)
                GameObject.Instantiate(eid.gameObject, eid.transform.position, eid.transform.rotation);
        }
        static void BuffEnemy(ChallengeSetting setting)
        {
            EnemyIdentifier eid = GetEnemy(setting);
            if (eid != null)
                eid.BuffAll();
        }
        static EnemyIdentifier GetEnemy(ChallengeSetting setting)
        {
            List<EnemyIdentifier> enemies = MonoSingleton<EnemyTracker>.Instance.GetCurrentEnemies().OrderBy((x) => Vector3.Distance(x.transform.position, MonoSingleton<NewMovement>.Instance.transform.position)).ToList();
            EnemyIdentifier selectedEnemy = null;
            if (enemies.Count == 0) return null;
            switch (setting.distanceField.value)
            {
                case distance.Closest:
                    selectedEnemy = enemies[0];
                    break;
                case distance.Furthest:
                    selectedEnemy = enemies.Last();
                    break;
                case distance.Inbetween:
                    selectedEnemy = enemies[Mathf.FloorToInt(enemies.Count / 2)];
                    break;
                case distance.Random:
                    selectedEnemy = enemies[Random.Range(0, enemies.Count)];
                    break;
            }
            return selectedEnemy;
        }


        private static readonly List<string> validLevels = new List<string>
        {
            // Regular levels
            "Level 0-1", "Level 0-2", "Level 0-3", "Level 0-4", "Level 0-5",
            "Level 1-1", "Level 1-2", "Level 1-3", "Level 1-4",
            "Level 2-1", "Level 2-2", "Level 2-3", "Level 2-4",
            "Level 3-1", "Level 3-2",
            "Level 4-1", "Level 4-2", "Level 4-3", "Level 4-4",
            "Level 5-1", "Level 5-2", "Level 5-3", "Level 5-4",
            "Level 6-1", "Level 6-2",
            "Level 7-1", "Level 7-2", "Level 7-3", "Level 7-4",

            // Secret levels
            "Level 0-S", "Level 1-S", "Level 2-S", "Level 4-S", "Level 5-S", "Level 7-S",

            // P-levels
            "Level P-1", "Level P-2"
        };

        public static void SelectRandomLevel()
        {
            SceneHelper.LoadScene(validLevels[Random.Range(0, validLevels.Count)], false);
        }

        public static bool IsBloodNearby(float radius = 5f)
        {
            var bsm = MonoSingleton<BloodsplatterManager>.Instance;
            if (bsm == null) return false;

            Vector3 playerPos = MonoSingleton<NewMovement>.Instance.transform.position;
            float3 playerPosFloat3 = new float3(playerPos.x, playerPos.y, playerPos.z);

            // Check each blood stain
            for (int i = 0; i < bsm.props.Length; i++)
            {
                var bloodStain = bsm.props[i];

                // Skip empty/invalid blood stains
                if (bloodStain.pos.Equals(default(float3)))
                    continue;

                float distance = Unity.Mathematics.math.distance(playerPosFloat3, bloodStain.pos);
                if (distance <= radius)
                    return true;
            }

            return false;
        }
    }
}