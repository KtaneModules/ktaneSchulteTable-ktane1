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
    private string[] fontNames = new string[] { "5Geomedings", "Art of Creation", "Barazhad", "Sheikah", "Cait Sith", "Catabase", "Daedra", "Deadspace", "Epta", "Glipervelz", "Jungle Slang", "Kakoulookiam", "KHScala", "Kilgish", "Meeksa", "Modern Cybertronic", "Morse01", "Square Things", "Stars 3D", "Star Things", "Stylebats", "Templar Cipher", "Tile Things", "Wavefont", "Zuptype Pica" };


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

            for (int i = 0; i < screenText.Length; i++)
            {
                screenText[i].GetComponent<TextMesh>().text = letters[i].ToString();
                int rf = UnityEngine.Random.Range(0, fonts.Length);
                screenText[i].GetComponent<TextMesh>().font = fonts[rf];

                if (rf == 3 || rf == 5) { screenText[i].GetComponent<TextMesh>().fontSize = 300; screenText[i].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f); }
                else if (rf == 8 || rf == 10) { screenText[i].GetComponent<TextMesh>().fontSize = 700; screenText[i].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f); }
                else if (rf == 13 || rf == 23) { screenText[i].GetComponent<TextMesh>().fontSize = 500; screenText[i].transform.localScale = new Vector3(0.0008f, 0.0008f, 0.0008f); }
                else { screenText[i].GetComponent<TextMesh>().fontSize = 500; screenText[i].transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f); }

                sb.Append(fontNames[rf] + ", ");
                screenText[i].GetComponent<MeshRenderer>().material = fontMats[rf];
            }
            sb.Remove(sb.Length - 2, 2);
            Debug.LogFormat("[Cruel Schulte Table #{0}] Module activated! Letters displayed in this order: {1}", moduleId, letters);
            Debug.LogFormat("[Cruel Schulte Table #{0}] The encryptions used are (in reading order): {1}", moduleId, sb.ToString());
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
                    audio.PlaySoundAtTransform("solve", transform);
                    timerStop();
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
        float timeLimit = 150f;
        if (TwitchPlaysActive)
        {
            timeLimit = 200f;
        }
        while (delta < 1f)
        {
            delta += Time.deltaTime * (1 / timeLimit);
            progressBar.transform.localScale = new Vector3(Mathf.Lerp(20 / 3f, 0f, delta), y, z);
            yield return null;
        }
        reset();
        Debug.LogFormat("[Cruel Schulte Table #{0}] 30 second time limit reached! Module reset!", moduleId);
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
    private readonly string TwitchHelpMessage = @"<!{0} start> to start the module, <!{0} press A1 B2 C3> to press the letters on the module, with letter being column, number being row, and top left is A1, <!{0} press 1 2 3> to press the letters denoted by their positions in reading order, top left being 1, for TP the time limit is extended to 200 seconds";
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
