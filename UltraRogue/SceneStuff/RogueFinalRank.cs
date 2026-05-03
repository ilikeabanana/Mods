using Newtonsoft.Json;
using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using Ultrarogue;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Token: 0x020001FC RID: 508
public class RogueFinalRank : MonoBehaviour
{
    public static RogueFinalRank Instance { get; private set; }
    public const string PREF_KEY = "RogueModeBest";
    void Awake()
    {
        Instance = this;
        GameObject[] array = this.toAppear;
        for (int i = 0; i < array.Length; i++)
        {
            array[i].SetActive(false);
        }
        gameObject.SetActive(false);

    }

    public RogueSaveData GetBest()
    {
        string pref = PlayerPrefs.GetString(PREF_KEY, "");
        RogueSaveData? save = JsonConvert.DeserializeObject<RogueSaveData>(pref);
        RogueSaveData toReturn = new RogueSaveData();
        if (save == null)
        {
            toReturn.datas = new System.Collections.Generic.List<RogueSaveDataData>();
            toReturn.BestRun = new RogueSaveDataData(); // all fields default to 0
        }
        else
        {
            toReturn = save;
        }
        return toReturn;
    }


    // Token: 0x06000B51 RID: 2897 RVA: 0x0004E034 File Offset: 0x0004C234
    public void GameOver()
    {
        if (!this.gameOver)
        {
            gameObject.SetActive(true);
            if (this.sman == null)
                this.sman = MonoSingleton<StatsManager>.Instance;

            int @int = MonoSingleton<PrefsManager>.Instance.GetInt("difficulty", 0);
            this.gameOver = true;
            this.sman.StopTimer();
            this.sman.HideShit();
            MonoSingleton<TimeController>.Instance.controlTimeScale = false;
            this.savedTime = this.sman.seconds;
            this.savedKills = this.sman.kills;
            this.savedStyle = Plugin.items.Count;
            if (this.savedStyle < 0)
                this.savedStyle = 0;

            ActivateNextWave activateNextWave = Object.FindObjectOfType<ActivateNextWave>();
            this.savedWaves = RogueDifficultyManager.Instance.floor;
            this.previousBest = GetBest();

            // FIX 1: Use previousBest.BestRun.Floor, not hardcoded 1
            this.bestWaveText.text = Mathf.FloorToInt(this.previousBest.BestRun.Floor).ToString()
                + string.Format("\n<color=#616161><size=20>{0}%</size></color>", this.CalculatePerc(this.previousBest.BestRun.Floor));
            this.bestKillsText.text = (this.previousBest.BestRun.Kills.ToString() ?? "");
            this.bestStyleText.text = (this.previousBest.BestRun.ItemsGotten.ToString() ?? "");

            // FIX 2: Loop must start from previousBest time, not 0
            int num = 0;
            float num2;
            for (num2 = this.previousBest.BestRun.Time; num2 >= 60f; num2 -= 60f)
            {
                num++;
            }
            this.bestTimeText.text = num.ToString() + ":" + num2.ToString("00.000");

            if (this.sman.majorUsed || MonoSingleton<AssistController>.Instance.cheatsEnabled)
                return;

            if (this.savedWaves > this.previousBest.BestRun.Floor)
            {
                this.NewBest();
                return;
            }
            if (this.savedWaves < this.previousBest.BestRun.Floor)
                return;

            if (this.savedKills > this.previousBest.BestRun.Kills)
            {
                this.NewBest();
                return;
            }
            if (this.savedKills < this.previousBest.BestRun.Kills)
                return;

            if (this.savedStyle > this.previousBest.BestRun.ItemsGotten)
            {
                this.NewBest();
                return;
            }
        }
    }

    private void NewBest()
    {
        previousBest.BestRun.Kills = this.savedKills;
        previousBest.BestRun.ItemsGotten = this.savedStyle;
        previousBest.BestRun.Floor = (int)this.savedWaves;
        previousBest.BestRun.Time = this.savedTime; // make sure RogueSaveDataData has a Time field

        // FIX 4: Actually persist the new best to PlayerPrefs
        PlayerPrefs.SetString(PREF_KEY, JsonConvert.SerializeObject(this.previousBest));
        PlayerPrefs.Save();

        this.newBest = true;
    }

    // Token: 0x06000B53 RID: 2899 RVA: 0x0004E31C File Offset: 0x0004C51C
    private void Update()
    {
        if (this.gameOver)
        {
            if (this.timeController == null)
            {
                this.timeController = MonoSingleton<TimeController>.Instance;
            }
            if (this.timeController.timeScale > 0f)
            {
                this.timeController.timeScale = Mathf.MoveTowards(this.timeController.timeScale, 0f, Time.unscaledDeltaTime * (this.timeController.timeScale + 0.01f));
                Time.timeScale = this.timeController.timeScale * this.timeController.timeScaleModifier;
                MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allPitch", this.timeController.timeScale);
                if (this.timeController.timeScale < 0.1f)
                {
                    MonoSingleton<AudioMixerController>.Instance.forceOff = true;
                    MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allVolume", MonoSingleton<AudioMixerController>.Instance.CalculateVolume(this.timeController.timeScale * 10f * MonoSingleton<AudioMixerController>.Instance.sfxVolume));
                    MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("allVolume", MonoSingleton<AudioMixerController>.Instance.CalculateVolume(this.timeController.timeScale * 10f * MonoSingleton<AudioMixerController>.Instance.musicVolume));
                }
                MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("allPitch", this.timeController.timeScale);
                MonoSingleton<MusicManager>.Instance.volume = 0.5f + this.timeController.timeScale / 2f;
                if (this.timeController.timeScale <= 0f)
                {
                    this.Appear();
                    MonoSingleton<MusicManager>.Instance.forcedOff = true;
                    MonoSingleton<MusicManager>.Instance.StopMusic();
                }
            }
        }
        if (this.countTime)
        {
            if (this.savedTime >= this.checkedSeconds)
            {
                if (this.savedTime > this.checkedSeconds)
                {
                    float num = this.savedTime - this.checkedSeconds;
                    this.checkedSeconds += Time.unscaledDeltaTime * 20f + Time.unscaledDeltaTime * num * 1.5f;
                    this.seconds += Time.unscaledDeltaTime * 20f + Time.unscaledDeltaTime * num * 1.5f;
                }
                if (this.checkedSeconds >= this.savedTime || this.skipping)
                {
                    this.checkedSeconds = this.savedTime;
                    this.seconds = this.savedTime;
                    this.minutes = 0f;
                    while (this.seconds >= 60f)
                    {
                        this.seconds -= 60f;
                        this.minutes += 1f;
                    }
                    this.countTime = false;
                    this.timeText.GetComponent<AudioSource>().Stop();
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                }
                if (this.seconds >= 60f)
                {
                    this.seconds -= 60f;
                    this.minutes += 1f;
                }
                this.timeText.text = this.minutes.ToString() + ":" + this.seconds.ToString("00.000");
            }
        }
        else if (this.countWaves)
        {
            if (this.savedWaves >= this.checkedWaves)
            {
                if (this.savedWaves > this.checkedWaves)
                {
                    this.checkedWaves += Time.unscaledDeltaTime * 20f + Time.unscaledDeltaTime * (this.savedWaves - this.checkedWaves) * 1.5f;
                }
                if (this.checkedWaves >= this.savedWaves || this.skipping)
                {
                    this.checkedWaves = this.savedWaves;
                    this.countWaves = false;
                    this.waveText.GetComponent<AudioSource>().Stop();
                    this.totalPoints += Mathf.FloorToInt(this.savedWaves) * 100;
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                }
                else
                {
                }
                this.waveText.text = Mathf.FloorToInt(this.checkedWaves).ToString() + string.Format("\n<color=#616161><size=20>{0}%</size></color>", this.CalculatePerc(this.savedWaves));
            }
        }
        else if (this.countKills)
        {
            if ((float)this.savedKills >= this.checkedKills)
            {
                if ((float)this.savedKills > this.checkedKills)
                {
                    this.checkedKills += Time.unscaledDeltaTime * 20f + Time.unscaledDeltaTime * ((float)this.savedKills - this.checkedKills) * 1.5f;
                }
                if (this.checkedKills >= (float)this.savedKills || this.skipping)
                {
                    this.checkedKills = (float)this.savedKills;
                    this.countKills = false;
                    this.killsText.GetComponent<AudioSource>().Stop();
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                }
                this.killsText.text = this.checkedKills.ToString("0");
            }
        }
        else if (this.countStyle && (float)this.savedStyle >= this.checkedStyle)
        {
            float num3 = this.checkedStyle;
            if ((float)this.savedStyle > this.checkedStyle)
            {
                this.checkedStyle += Time.unscaledDeltaTime * 2500f + Time.unscaledDeltaTime * ((float)this.savedStyle - this.checkedStyle) * 1.5f;
            }
            if (this.checkedStyle >= (float)this.savedStyle || this.skipping)
            {
                this.checkedStyle = (float)this.savedStyle;
                this.countStyle = false;
                this.styleText.GetComponent<AudioSource>().Stop();
                base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                this.totalPoints += this.savedStyle;
            }
            else
            {
            }
            this.styleText.text = this.checkedStyle.ToString("0");
        }
        if (this.flashFade)
        {
            this.flashColor.a = Mathf.MoveTowards(this.flashColor.a, 0f, Time.unscaledDeltaTime * 0.5f);
            this.flashPanel.color = this.flashColor;
            if (this.flashColor.a <= 0f)
            {
                this.flashFade = false;
            }
        }
        if (this.gameOver)
        {
            if (this.timeController == null)
            {
                this.timeController = MonoSingleton<TimeController>.Instance;
            }
            if (this.opm == null)
            {
                this.opm = MonoSingleton<OptionsManager>.Instance;
            }
            if (this.opm.paused && !this.wasPaused)
            {
                this.wasPaused = true;
            }
            else if (!this.opm.paused && this.wasPaused)
            {
                this.wasPaused = false;
                MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allPitch", 0f);
                MonoSingleton<AudioMixerController>.Instance.allSound.SetFloat("allVolume", MonoSingleton<AudioMixerController>.Instance.CalculateVolume(this.timeController.timeScale * 10f * MonoSingleton<AudioMixerController>.Instance.sfxVolume));
                MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("allPitch", 0f);
                MonoSingleton<AudioMixerController>.Instance.musicSound.SetFloat("allVolume", MonoSingleton<AudioMixerController>.Instance.CalculateVolume(this.timeController.timeScale * 10f * MonoSingleton<AudioMixerController>.Instance.musicVolume));
            }
            if (!MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() &&
            (MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame ||
             MonoSingleton<InputManager>.Instance.InputSource.Jump.WasPerformedThisFrame) &&
            this.complete && !this.opm.paused)
            {
                SceneHelper.LoadScene("Main Menu");
                return;
            }

            else if (this.timeController.timeScale <= 0f && !MonoSingleton<InputManager>.Instance.PerformingCheatMenuCombo() && (MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame || MonoSingleton<InputManager>.Instance.InputSource.Jump.WasPerformedThisFrame) && !this.complete && !this.opm.paused)
            {
                this.skipping = true;
                this.timeBetween = 0.01f;
            }
        }
    }

    // Token: 0x06000B54 RID: 2900 RVA: 0x0004EF69 File Offset: 0x0004D169
    private int CalculatePerc(float value)
    {
        return Mathf.FloorToInt((value - (float)Mathf.FloorToInt(value)) * 100f);
    }


    // Token: 0x06000B56 RID: 2902 RVA: 0x0004EFB7 File Offset: 0x0004D1B7
    private static string TruncateUsername(string value, int maxChars)
    {
        if (value.Length > maxChars)
        {
            return value.Substring(0, maxChars);
        }
        return value;
    }

    // Token: 0x06000B57 RID: 2903 RVA: 0x0004EFCC File Offset: 0x0004D1CC
    public void Appear()
    {
        if (this.i < this.toAppear.Length)
        {
            if (this.skipping)
            {
                HudOpenEffect component = this.toAppear[this.i].GetComponent<HudOpenEffect>();
                if (component != null)
                {
                    component.skip = true;
                }
            }
            if (this.toAppear[this.i] == this.timeText.gameObject)
            {
                if (this.skipping)
                {
                    this.checkedSeconds = this.savedTime;
                    this.seconds = this.savedTime;
                    this.minutes = 0f;
                    while (this.seconds >= 60f)
                    {
                        this.seconds -= 60f;
                        this.minutes += 1f;
                    }
                    this.timeText.GetComponent<AudioSource>().SetPlayOnAwake(false);
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                    this.timeText.text = this.minutes.ToString() + ":" + this.seconds.ToString("00.000");
                }
                else
                {
                    this.countTime = true;
                }
            }
            else if (this.toAppear[this.i] == this.killsText.gameObject)
            {
                if (this.skipping)
                {
                    this.checkedKills = (float)this.savedKills;
                    this.killsText.GetComponent<AudioSource>().SetPlayOnAwake(false);
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                    this.killsText.text = this.checkedKills.ToString("0");
                }
                else
                {
                    this.countKills = true;
                }
            }
            else if (this.toAppear[this.i] == this.waveText.gameObject)
            {
                if (this.skipping)
                {
                    this.checkedWaves = this.savedWaves;
                    this.waveText.GetComponent<AudioSource>().SetPlayOnAwake(false);
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                    this.waveText.text = Mathf.FloorToInt(this.savedWaves).ToString() + string.Format("\n<color=#616161><size=20>{0}%</size></color>", this.CalculatePerc(this.savedWaves));
                }
                else
                {
                    this.countWaves = true;
                }
            }
            else if (this.toAppear[this.i] == this.styleText.gameObject)
            {
                if (this.skipping)
                {
                    this.checkedStyle = (float)this.savedStyle;
                    this.styleText.text = this.checkedStyle.ToString("0");
                    this.styleText.GetComponent<AudioSource>().SetPlayOnAwake(false);
                    base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween * 2f));
                }
                else
                {
                    this.countStyle = true;
                }
            }
            else
            {
                base.StartCoroutine(this.InvokeRealtimeCoroutine(new UnityAction(this.Appear), this.timeBetween));
            }
            this.toAppear[this.i].gameObject.SetActive(true);
            this.i++;
            return;
        }
        if (this.newBest)
        {
            GameObject gameObject = this.bestWaveText.transform.parent.parent.parent.GetChild(1).gameObject;
            this.FlashPanel(gameObject);
            gameObject.GetComponent<AudioSource>().Play(true);
            this.bestWaveText.text = this.waveText.text;
            this.bestKillsText.text = this.killsText.text;
            this.bestStyleText.text = this.styleText.text;
            this.bestTimeText.text = this.timeText.text;
        }
        if (!this.complete)
        {
            this.complete = true;
            GameProgressSaver.AddMoney(this.totalPoints);
        }
    }

    // Token: 0x06000B58 RID: 2904 RVA: 0x0004F410 File Offset: 0x0004D610
    public void FlashPanel(GameObject panel)
    {
        if (this.flashFade)
        {
            this.flashColor.a = 0f;
            this.flashPanel.color = this.flashColor;
        }
        this.flashPanel = panel.GetComponent<Image>();
        this.flashColor = this.flashPanel.color;
        this.flashColor.a = 1f;
        this.flashPanel.color = this.flashColor;
        this.flashFade = true;
    }


    // Token: 0x06000B5A RID: 2906 RVA: 0x0004F5B3 File Offset: 0x0004D7B3
    private IEnumerator InvokeRealtimeCoroutine(UnityAction action, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (action != null)
        {
            action();
        }
        yield break;
    }

    // Token: 0x04000DB1 RID: 3505
    public TMP_Text waveText;

    // Token: 0x04000DB2 RID: 3506
    public TMP_Text killsText;

    // Token: 0x04000DB3 RID: 3507
    public TMP_Text styleText;

    // Token: 0x04000DB4 RID: 3508
    public TMP_Text timeText;

    // Token: 0x04000DB5 RID: 3509
    public TMP_Text bestWaveText;

    // Token: 0x04000DB6 RID: 3510
    public TMP_Text bestKillsText;

    // Token: 0x04000DB7 RID: 3511
    public TMP_Text bestStyleText;

    // Token: 0x04000DB8 RID: 3512
    public TMP_Text bestTimeText;


    // Token: 0x04000DBA RID: 3514
    public int totalPoints;

    // Token: 0x04000DBB RID: 3515
    public GameObject[] toAppear;

    // Token: 0x04000DBC RID: 3516
    private bool skipping;

    // Token: 0x04000DBD RID: 3517
    private float timeBetween = 0.25f;

    // Token: 0x04000DBE RID: 3518
    private bool countTime;

    // Token: 0x04000DBF RID: 3519
    public float savedTime;

    // Token: 0x04000DC0 RID: 3520
    private float checkedSeconds;

    // Token: 0x04000DC1 RID: 3521
    private float seconds;

    // Token: 0x04000DC2 RID: 3522
    private float minutes;

    // Token: 0x04000DC3 RID: 3523
    private bool countWaves;

    // Token: 0x04000DC4 RID: 3524
    public float savedWaves;

    // Token: 0x04000DC5 RID: 3525
    private float checkedWaves;

    // Token: 0x04000DC6 RID: 3526
    private bool countKills;

    // Token: 0x04000DC7 RID: 3527
    public int savedKills;

    // Token: 0x04000DC8 RID: 3528
    private float checkedKills;

    // Token: 0x04000DC9 RID: 3529
    private bool countStyle;

    // Token: 0x04000DCA RID: 3530
    public int savedStyle;

    // Token: 0x04000DCB RID: 3531
    private float checkedStyle;

    // Token: 0x04000DCC RID: 3532
    private bool flashFade;

    // Token: 0x04000DCD RID: 3533
    private Color flashColor;

    // Token: 0x04000DCE RID: 3534
    private Image flashPanel;

    // Token: 0x04000DCF RID: 3535
    private int i;

    // Token: 0x04000DD0 RID: 3536
    private bool gameOver;

    // Token: 0x04000DD1 RID: 3537
    private bool complete;

    // Token: 0x04000DD2 RID: 3538
    private RogueSaveData previousBest;

    // Token: 0x04000DD3 RID: 3539
    private bool newBest;

    // Token: 0x04000DD4 RID: 3540
    private TimeController timeController;

    // Token: 0x04000DD5 RID: 3541
    private OptionsManager opm;

    // Token: 0x04000DD6 RID: 3542
    private bool wasPaused;

    // Token: 0x04000DD7 RID: 3543
    private StatsManager sman;
}
