using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class schulteTableScript: MonoBehaviour
{
#pragma warning disable 0649
    private bool TwitchPlaysActive;
#pragma warning restore 0649

    public new KMAudio audio;
    public KMBombInfo bomb;
	public KMBombModule module;
    public AudioSource audioSource;

    public GameObject[] screens;
    public GameObject[] screenText;
    public GameObject progressBar;
    public AudioClip select;

    private int[] numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
    private bool[] correctPresses = new bool[25];
    private int counter;
    private bool ascend;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleActivated, moduleSolved, isAnimating; // Some helpful booleans

    void Awake()
    {
    	moduleId = moduleIdCounter++;

        for (int i = 0; i < screens.Length; i++)
        {
            int j = i;
            screens[i].GetComponent<KMSelectable>().OnHighlight += delegate ()
            {
                if (!moduleSolved && !isAnimating)
                {
                    screenHighlight(j);
                }
            };

            screens[i].GetComponent<KMSelectable>().OnHighlightEnded += delegate ()
            {
                if (!moduleSolved && !isAnimating)
                {
                    screenHighlightEnd(j);
                }
            };

            screens[i].GetComponent<KMSelectable>().OnInteract += () => { screenPress(j); return false; };
        }

    }

    void Start()
    {
        foreach (GameObject i in screenText)
        {
            i.GetComponent<TextMesh>().text = "";
        }
        var sn = bomb.GetSerialNumber();
        ascend = (sn[5] - '0') % 2 == 0;
        if (ascend)
        {
            counter = 0;
            Debug.LogFormat("[Schulte Table #{0}] Module initiated, numbers to be pressed in ascending order.", moduleId);
        }
        else
        {
            counter = 26;
            Debug.LogFormat("[Schulte Table #{0}] Module initiated, numbers to be pressed in descending order.", moduleId);
        }
    }

    void screenHighlight(int k)
    {
        screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.7f, 0.7f, 0.7f, 1f);
    }

    void screenHighlightEnd(int k)
    {
        if (correctPresses[k])
        {
            screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 1f, 0.5f, 1f);
        }
        else
        {
            screens[k].GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    void screenPress(int k)
    {
        if (moduleSolved || isAnimating) { return; }
        if (!moduleActivated)
        {
            var sb = new StringBuilder();
            numbers.Shuffle();
            moduleActivated = true;
            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].GetComponent<TextMesh>().text = numbers[i].ToString();
                sb.Append(numbers[i].ToString() + ", ");
                StartCoroutine(funnyRotate(i));
            }
            sb.Remove(sb.Length - 2, 2);
            Debug.LogFormat("[Schulte Table #{0}] Module activated! Numbers displayed in this order: {1}", moduleId, sb.ToString());
            audio.PlaySoundAtTransform("start", transform);
            StartCoroutine("timer");
        }
        else
        {
            if (screenText[k].GetComponent<TextMesh>().text == (counter + 1).ToString() && ascend)
            {
                StartCoroutine(playSound(counter));
                counter++;
                correctPresses[k] = true;
                screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 1f, 0.5f, 1f);
                if (counter >= 25)
                {
                    module.HandlePass();
                    Debug.LogFormat("[Schulte Table #{0}] Correctly pressed all numbers in order within the time limit! Module solved!", moduleId);
                    moduleSolved = true;
                    moduleActivated = false;
                    audio.PlaySoundAtTransform("solve", transform);
                    timerStop();
                }
            }
            else if (screenText[k].GetComponent<TextMesh>().text == (counter - 1).ToString() && !ascend)
            {
                StartCoroutine(playSound(26 - counter));
                counter--;
                correctPresses[k] = true;
                screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 1f, 0.5f, 1f);
                if (counter <= 1)
                {
                    module.HandlePass();
                    Debug.LogFormat("[Schulte Table #{0}] Correctly pressed all numbers in order within the time limit! Module solved!", moduleId);
                    moduleSolved = true;
                    moduleActivated = false;
                    audio.PlaySoundAtTransform("solve", transform);
                    timerStop();
                }
            }

            else
            {
                module.HandleStrike();
                int l = 0;
                if (ascend) { l = counter + 1; } else { l = counter - 1; }
                Debug.LogFormat("[Schulte Table #{0}] Incorrectly pressed {1} when you should press {2}, strike and module reset!", moduleId, screenText[k].GetComponent<TextMesh>().text, l);
                timerStop();
                reset();

            }
        }
    }

    void reset()
    {
        audio.PlaySoundAtTransform("strike", transform);
        if (ascend)
            counter = 0;
        else
            counter = 26;
        correctPresses = new bool[25];
        foreach (GameObject i in screenText)
        {
            i.GetComponent<TextMesh>().text = "";
        }
        moduleActivated = false;
        StartCoroutine(strikeAnim());
    }

    void timerStop()
    {
        StopCoroutine("timer");
        float y = progressBar.transform.localScale.y;
        float z = progressBar.transform.localScale.z;
        progressBar.transform.localScale = new Vector3(20 / 3f, y, z);
    }

    private AudioClip MakeSubclip(AudioClip clip, float start, float timeLength)
    {
        /* Create a new audio clip */
        int frequency = clip.frequency;
        int samplesLength = (int)(frequency * timeLength * clip.channels);
        AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, clip.channels, frequency, false);
        /* Create a temporary buffer for the samples */
        float[] data = new float[samplesLength];
        /* Get the data from the original clip */
        clip.GetData(data, (int)(frequency * start));
        /* Transfer the data to the new clip */
        newClip.SetData(data, 0);
        /* Return the sub clip */
        return newClip;
    }

    IEnumerator playSound(int counter)
    {
        float d = select.length / 26;
        audioSource.clip = MakeSubclip(select, d * (counter + 1), 0.02f);
        audioSource.Play();
        while (audioSource.isPlaying)
        {
            if (audioSource.time >= 0.05 * (counter + 1))
            {
                audioSource.Stop();
            }
            yield return null;
        }
    }

    IEnumerator timer()
    {
        float delta = 0f;
        float y = progressBar.transform.localScale.y;
        float z = progressBar.transform.localScale.z;
        float timeLimit = 30f;
        if (TwitchPlaysActive)
        {
            timeLimit = 50f;
        }
        while (delta < 1f)
        {
            delta += Time.deltaTime * (1 / timeLimit);
            progressBar.transform.localScale = new Vector3(Mathf.Lerp(20 / 3f, 0f, delta), y, z);
            yield return null;
        }
        reset();
        Debug.LogFormat("[Schulte Table #{0}] 30 second time limit reached! Module reset!", moduleId);
    }

    IEnumerator funnyRotate(int k)
    {
        float rotateSpeed = UnityEngine.Random.Range(1f, 3f);
        int d = UnityEngine.Random.Range(0, 2);
        while (moduleActivated)
        {
            float delta = 0f;
            while (delta < 1f)
            {
                delta += Time.deltaTime * (1 / rotateSpeed);
                if (d == 0)
                {
                    screenText[k].transform.localEulerAngles = new Vector3(90, Mathf.Lerp(0f, 360f, delta), 0f);
                }
                else
                {
                    screenText[k].transform.localEulerAngles = new Vector3(90, Mathf.Lerp(360f, 0f, delta), 0f);
                }
                yield return null;
            }
        }
    }

    IEnumerator strikeAnim()
    {
        isAnimating = true;
        for (int j = 0; j < 2; j++)
        {
            foreach (GameObject i in screens)
            {
                i.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.5f, 0.5f, 1f);
            }
            yield return new WaitForSeconds(0.2f);
            foreach (GameObject i in screens)
            {
                i.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 1f);
            }
            yield return new WaitForSeconds(0.2f);
        }
        isAnimating = false;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} start> to start the module, <!{0} press A1 B2 C3> to press the numbers on the module, with letter being column, number being row, and top left is A1, <!{0} press 1 2 3> to press the numbers denoted by their positions in reading order, top left being 1, for TP the time limit is extended to 50 seconds";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        string[] parameters = command.Split(' ');
        yield return null;
        if (Regex.IsMatch(command, @"^\s*start\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (isAnimating) { yield return "sendtochaterror Animation, command ignored"; yield break; }
            screens[0].GetComponent<KMSelectable>().OnInteract();
            yield return null;
        }
        else if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length < 2) { yield return "sendtochaterror Invalid command"; yield break; }
            if (isAnimating) { yield return "sendtochaterror Animation, command ignored"; yield break; }
            if (!moduleActivated) { yield return "sendtochaterror Module not started, command ignored"; yield break; }
            var presses = new List<KMSelectable>();
            for (int i = 1; i < parameters.Length; i++)
            {
                int n = 0;
                bool c = int.TryParse(parameters[i], out n);
                if (c)
                {
                    if (n < 1 || n > 36) { yield return "sendtochaterror Invalid command"; yield break; }
                    presses.Add(screens[n - 1].GetComponent<KMSelectable>());
                }
                else
                {
                    if (parameters[i].Length != 2 || !"abcde".Contains(parameters[i][0]) || !"12345".Contains(parameters[i][1]))
                    {
                        yield return "sendtochaterror Invalid command"; yield break;
                    }
                    presses.Add(screens["abcde".IndexOf(parameters[i][0]) + "12345".IndexOf(parameters[i][1]) * 5].GetComponent<KMSelectable>());
                }
            }
            foreach (KMSelectable s in presses) { s.OnInteract(); yield return new WaitForSeconds(0.02f); }
        }
        else
        {
            yield return "sendtochaterror Invalid command"; yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (isAnimating) { yield return null; }
            else if (!moduleActivated) { screens[0].GetComponent<KMSelectable>().OnInteract(); yield return null; }
            else
            {
                int index = 0;
                if (ascend)
                {
                    index = Array.IndexOf(numbers, counter + 1);
                }
                else
                {
                    index = Array.IndexOf(numbers, counter - 1);
                }
                screens[index].GetComponent<KMSelectable>().OnInteract();
                yield return null;
            }
        }
        yield return null;
    }
 
}
