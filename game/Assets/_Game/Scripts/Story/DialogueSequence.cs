using System.Collections;
using UnityEngine;

// Escenas de narración (intro y cierre): reproduce un diálogo al empezar y luego continúa el flujo.
public class DialogueSequence : MonoBehaviour
{
    public enum Then { StartChapter, ShowObject, ReturnToMenu }

    [SerializeField] private DialogueData dialogue;
    [SerializeField] private Then then;
    [Tooltip("Para ShowObject: lo que aparece al terminar (p. ej. la tarjeta final).")]
    [SerializeField] private GameObject showWhenDone;
    [SerializeField] private string setFlag;

    public void Setup(DialogueData data, Then next, GameObject show, string flag)
    {
        dialogue = data;
        then = next;
        showWhenDone = show;
        setFlag = flag;
        if (showWhenDone != null) showWhenDone.SetActive(false);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsBusy) yield return null;
        // Con un StoryBook en el mismo objeto la narración se lee como cuento; si no, en la caja de diálogo
        var book = GetComponent<StoryBook>();
        if (book != null) book.Play(dialogue, Finish);
        else DialogueUI.Play(dialogue, Finish);
    }

    private void Finish()
    {
        StoryFlags.Set(setFlag);
        switch (then)
        {
            case Then.StartChapter: GameFlow.Instance.StartChapter(); break;
            case Then.ReturnToMenu: GameFlow.Instance.ReturnToMenu(); break;
            case Then.ShowObject: if (showWhenDone != null) showWhenDone.SetActive(true); break;
        }
    }
}
