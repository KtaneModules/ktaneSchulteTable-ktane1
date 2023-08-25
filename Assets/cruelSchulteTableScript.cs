using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class cruelSchulteTableScript : MonoBehaviour
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

    public Font[] fonts;
    public Material[] fontMats;
    public Font normalFont;
    public Material normalFontMat;
    private string[] fontNames = new string[] { "5Geomedings", "Art of Creation", "Barazhad", "Sheikah", "Cait Sith", "Catabase", "Leafs", "Deadspace", "Epta", "Glipervelz", "Jungle Slang", "Kakoulookiam", "KHScala", "Kilgish", "Meeksa", "Modern Cybertronic", "Morse01", "Square Things", "Stars 3D", "Yelekish", "Stylebats", "MeowsPhone", "Lucius Cipher", "Wavefont", "Binary Soldiers" };


    private static string actualLetters = "ABCDEFGHIJKLMNOPQRSTUVWXY";
    private string letters = "ABCDEFGHIJKLMNOPQRSTUVWXY";
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
            Debug.LogFormat("[Cruel Schulte Table #{0}] Module initiated, letters to be pressed in alphabetical order.", moduleId);
        }
        else
        {
            counter = 24;
            Debug.LogFormat("[Cruel Schulte Table #{0}] Module initiated, numbers to be pressed in reverse alphabetical order.", moduleId);
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
            moduleActivated = true;

            var sb = new StringBuilder();
            var a = letters.ToCharArray();
            a.Shuffle();
            foreach (char b in a) { sb.Append(b); }
            letters = sb.ToString();
            sb.Remove(0, sb.Length);

            int rf = UnityEngine.Random.Range(0, fonts.Length);
            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].GetComponent<TextMesh>().font = fonts[rf];
                screenText[i].GetComponent<TextMesh>().text = letters[i].ToString();
                if (rf == 3 || rf == 5) { screenText[i].GetComponent<TextMesh>().fontSize = 100; screenText[i].transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f); }
                else if (rf == 8 || rf == 10) { screenText[i].GetComponent<TextMesh>().fontSize = 700; screenText[i].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f); }
                else if (rf == 13 || rf == 23) { screenText[i].GetComponent<TextMesh>().fontSize = 500; screenText[i].transform.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f); }
                else if (rf == 6) { screenText[i].GetComponent<TextMesh>().fontSize = 900; }
                else { screenText[i].GetComponent<TextMesh>().fontSize = 500; screenText[i].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f); }
                screenText[i].GetComponent<MeshRenderer>().material = fontMats[rf];
            }
            Debug.LogFormat("[Cruel Schulte Table #{0}] Module activated! Letters displayed in this order: {1}", moduleId, letters);
            Debug.LogFormat("[Cruel Schulte Table #{0}] The encryption used on the module is {1}.", moduleId, fontNames[rf]);
            audio.PlaySoundAtTransform("start", transform);
            StartCoroutine("timer");
        }
        else
        {
            if (actualLetters.IndexOf(screenText[k].GetComponent<TextMesh>().text) == counter && ascend)
            {
                StartCoroutine(playSound(counter));
                counter++;
                correctPresses[k] = true;
                screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 1f, 0.5f, 1f);
                if (counter >= 25)
                {
                    module.HandlePass();
                    Debug.LogFormat("[Cruel Schulte Table #{0}] Correctly pressed all letters in order within the time limit! Module solved!", moduleId);
                    moduleSolved = true;
                    moduleActivated = false;
                    audio.PlaySoundAtTransform("solveCruel", transform);
                    timerStop();
                    StartCoroutine(solveAnim());
                }
            }
            else if (actualLetters.IndexOf(screenText[k].GetComponent<TextMesh>().text) == counter && !ascend)
            {
                StartCoroutine(playSound(25 - counter));
                counter--;
                correctPresses[k] = true;
                screens[k].GetComponent<MeshRenderer>().material.color = new Color(0.5f, 1f, 0.5f, 1f);
                if (counter < 0)
                {
                    module.HandlePass();
                    Debug.LogFormat("[Cruel Schulte Table #{0}] Correctly pressed all letters in order within the time limit! Module solved!", moduleId);
                    moduleSolved = true;
                    moduleActivated = false;
                    audio.PlaySoundAtTransform("solveCruel", transform);
                    timerStop();
                    StartCoroutine(solveAnim());
                }
            }

            else
            {
                module.HandleStrike();
                char l = actualLetters[counter];
                Debug.LogFormat("[Cruel Schulte Table #{0}] Incorrectly pressed {1} when you should press {2}, strike and module reset!", moduleId, screenText[k].GetComponent<TextMesh>().text, l);
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
            counter = 24;
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
        float d = select.length / 27;
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
        float timeLimit = 300f;
        if (TwitchPlaysActive)
        {
            timeLimit = 320f;
        }
        while (delta < 1f)
        {
            delta += Time.deltaTime * (1 / timeLimit);
            progressBar.transform.localScale = new Vector3(Mathf.Lerp(20 / 3f, 0f, delta), y, z);
            yield return null;
        }
        reset();
        Debug.LogFormat("[Cruel Schulte Table #{0}] Time limit reached! Module reset!", moduleId);
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

    IEnumerator solveAnim()
    {
        isAnimating = true;
        int[][] pattern = new int[][] { new int[] { 0 }, new int[] { 1, 5 }, new int[] { 2, 6, 10 }, new int[] { 3, 7, 11, 15 }, new int[] { 4, 8, 12, 16, 20 }, new int[] { 9, 13, 17, 21 }, new int[] { 14, 18, 22 }, new int[] { 19, 23 }, new int[] { 24 } };
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < pattern[i].Length; j++)
            {
                screenText[pattern[i][j]].GetComponent<TextMesh>().font = normalFont;
                screenText[pattern[i][j]].GetComponent<MeshRenderer>().material = normalFontMat;
                screenText[pattern[i][j]].GetComponent<TextMesh>().fontSize = 500;
                screenText[pattern[i][j]].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f);
            }
            yield return new WaitForSeconds(0.2f);
        }
        yield return null;
        isAnimating = false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"<!{0} start> to start the module, <!{0} press A1 B2 C3> to press the letters on the module, with letter being column, number being row, and top left is A1, <!{0} press 1 2 3> to press the letters denoted by their positions in reading order, top left being 1, for TP the time limit is extended to 320 seconds";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        List<string> parameters = command.ToLowerInvariant().Trim().Split(' ').ToList();
        command = parameters[0];
        parameters.RemoveAt(0);

        if (command == "start" || command == "begin")
        {
            if (isAnimating)     yield return "sendtochaterror!h The module is currently animating. The command was ignored.";
            if (moduleActivated) yield return "sendtochaterror!h The module has already been started. The command was ignored.";

            yield return null;
            screens[0].GetComponent<KMSelectable>().OnInteract();
        }
        else if (command == "press")
        {
            if (parameters.Count < 1) yield break;
            if (isAnimating)          yield return "sendtochaterror!h The module is currently animating. The command was ignored.";
            if (!moduleActivated)     yield return "sendtochaterror!h The module has not been started. The command was ignored.";

            int n;
            var presses = new List<KMSelectable>();
            foreach (string coord in parameters)
            {
                if (int.TryParse(coord, out n) && n >= 1 && n <= 25) // From 1-25, in reading order (A1 B1 C1 D1 E1 A2 ...)
                    presses.Add(screens[n - 1].GetComponent<KMSelectable>());
                else if (Regex.IsMatch(coord, @"^[a-e][1-5]$")) // Specified coordinate, column, then row
                    presses.Add(screens["abcde".IndexOf(coord[0]) + "12345".IndexOf(coord[1]) * 5].GetComponent<KMSelectable>());
                else
                    yield return String.Format("sendtochaterror!h Unrecognized coordinate '{0}', valid coordinates are A1-E5 (column, then row) or 1-25 (reading order).", coord);
            }

            yield return null;
            foreach (KMSelectable s in presses)
            {
                if (isAnimating) yield break; // Time expired mid-command

                s.OnInteract();
                yield return new WaitForSeconds(0.02f);
            }
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
                    index = letters.IndexOf(actualLetters[counter]);
                }
                else
                {
                    index = letters.IndexOf(actualLetters[counter]);
                }
                screens[index].GetComponent<KMSelectable>().OnInteract();
                yield return null;
            }
        }
        yield return null;
    }

}
