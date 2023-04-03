using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class MassiveAmountOfWiresScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMRuleSeedable ruleSeed;
    public KMSelectable moduleSel;
    public GameObject wirePrefab;
    public Material[] colorMats;

    private string[] colorNames = { "White", "Grey", "Black", "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose" };
    private int[] colCutOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
    private int[] wireCols;
    private bool[] wireCut;
    private int currentCutCol;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Start()
    {
        moduleId = moduleIdCounter++;
        List<KMSelectable> children = new List<KMSelectable>();
        int numOfWires = UnityEngine.Random.Range(150, 201);
        wireCols = new int[numOfWires];
        wireCut = new bool[numOfWires];
        for (int i = 0; i < numOfWires; i++)
        {
            int j = i;
            GameObject wire = Instantiate(wirePrefab);
            wire.transform.parent = transform;
            wire.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.07f, 0.07f), 0, UnityEngine.Random.Range(-0.07f, 0.07f));
            wire.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);
            KMSelectable wireSel = wire.GetComponent<KMSelectable>();
            wireSel.Parent = moduleSel;
            wireSel.OnInteract += delegate () { CutWire(wireSel, j); return false; };
            children.Add(wireSel);
            wireCols[i] = UnityEngine.Random.Range(0, 15);
            wire.transform.GetChild(1).GetComponent<MeshRenderer>().material = colorMats[wireCols[i]];
            wire.transform.GetChild(2).GetComponent<MeshRenderer>().material = colorMats[wireCols[i]];
            wire.transform.localScale *= transform.lossyScale.x;
        }
        moduleSel.Children = children.ToArray();
        moduleSel.UpdateChildrenProperly();
        var rnd = ruleSeed.GetRNG();
        if (rnd.Seed != 1)
            colCutOrder = rnd.ShuffleFisherYates(colCutOrder);
        checkNoWires:
        List<int> indexesToCheck = new List<int>();
        for (int i = 0; i < wireCols.Length; i++)
        {
            if (wireCols[i] == colCutOrder[currentCutCol])
                indexesToCheck.Add(i);
        }
        if (indexesToCheck.Count == 0)
        {
            currentCutCol++;
            goto checkNoWires;
        }
        Debug.LogFormat("[A Massive Amount of Wires #{0}] Using rule seed: {1}", moduleId, rnd.Seed);
        Debug.LogFormat("[A Massive Amount of Wires #{0}] Generated {1} wires", moduleId, numOfWires);
        Debug.LogFormat("[A Massive Amount of Wires #{0}] Wire color distribution:", moduleId);
        for (int i = 0; i < 15; i++)
            Debug.LogFormat("[A Massive Amount of Wires #{0}] {1} - {2}", moduleId, colorNames[i], wireCols.Count(x => x == i));
    }

    void CutWire(KMSelectable wire, int index)
    {
        if (moduleSolved != true)
        {
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, wire.transform);
            wire.transform.GetChild(0).gameObject.SetActive(false);
            wire.transform.GetChild(1).gameObject.SetActive(true);
            wire.transform.GetChild(2).gameObject.SetActive(false);
            wireCut[index] = true;
            if (wireCols[index] != colCutOrder[currentCutCol])
            {
                Debug.LogFormat("[A Massive Amount of Wires #{0}] A {1} wire was cut when a {2} wire was expected, strike", moduleId, colorNames[wireCols[index]], colorNames[colCutOrder[currentCutCol]]);
                GetComponent<KMBombModule>().HandleStrike();
            }
            checkNoWires:
            List<int> indexesToCheck = new List<int>();
            for (int i = 0; i < wireCols.Length; i++)
            {
                if (wireCols[i] == colCutOrder[currentCutCol])
                    indexesToCheck.Add(i);
            }
            bool moveOn = true;
            for (int i = 0; i < indexesToCheck.Count; i++)
            {
                if (!wireCut[indexesToCheck[i]])
                    moveOn = false;
            }
            if (moveOn)
            {
                if (indexesToCheck.Count != 0)
                    Debug.LogFormat("[A Massive Amount of Wires #{0}] All {1} wires cut successfully", moduleId, colorNames[colCutOrder[currentCutCol]]);
                currentCutCol++;
                if (currentCutCol != 15)
                    goto checkNoWires;
            }
            if (currentCutCol == 15)
            {
                moduleSolved = true;
                Debug.LogFormat("[A Massive Amount of Wires #{0}] All wires cut successfully, module solved", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                audio.PlaySoundAtTransform("victory", transform);
            }
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} cut <color> [Cuts a wire with the specified color] | Cuts can be chained using spaces | Valid colors are Red, Orange, Yellow, Lime, Green, Jade, Cyan, Azure, Blue, Violet, Magenta, roSe, White, grEy, and blacK";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*cut\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield break;
            string[] names = { "white", "grey", "black", "red", "orange", "yellow", "lime", "green", "jade", "cyan", "azure", "blue", "violet", "magenta", "rose" };
            string[] namesShort = { "w", "e", "k", "r", "o", "y", "l", "g", "j", "c", "a", "b", "v", "m", "s" };
            for (int i = 1; i < parameters.Length; i++)
            {
                if (!names.Contains(parameters[i].ToLowerInvariant()) && !namesShort.Contains(parameters[i].ToLowerInvariant()))
                    yield break;
            }
            yield return null;
            for (int i = 1; i < parameters.Length; i++)
            {
                int index;
                if (parameters[i].Length == 1)
                    index = Array.IndexOf(namesShort, parameters[i].ToLowerInvariant());
                else
                    index = Array.IndexOf(names, parameters[i].ToLowerInvariant());
                if (!wireCols.Contains(index))
                {
                    yield return "unsubmittablepenalty";
                    yield break;
                }
                for (int j = 0; j < wireCols.Length; j++)
                {
                    if (!wireCut[j] && wireCols[j] == index)
                    {
                        moduleSel.Children[j].OnInteract();
                        yield return new WaitForSeconds(.1f);
                        break;
                    }
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            for (int i = 0; i < wireCols.Length; i++)
            {
                if (!wireCut[i] && wireCols[i] == colCutOrder[currentCutCol])
                {
                    moduleSel.Children[i].OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }
}