using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Evento de historia de una sola vez: al entrar a la escena o al pisar una zona,
// si se cumple la condición, reproduce un diálogo y activa un flag.
public class StoryTrigger : MonoBehaviour
{
    public enum Mode { OnSceneStart, OnPlayerEnter }

    [SerializeField] private Mode mode = Mode.OnPlayerEnter;
    [SerializeField] private FlagCondition condition;
    [SerializeField] private DialogueData dialogue;
    [Tooltip("Flag que se activa al terminar. También evita que se repita.")]
    [SerializeField] private string setFlag;
    [SerializeField] private float startDelay = 0.6f;
    [SerializeField] private UnityEvent onFinished;

    private bool running;

    public void Configure(Mode triggerMode, FlagCondition when, DialogueData data, string flag)
    {
        mode = triggerMode;
        condition = when;
        dialogue = data;
        setFlag = flag;
    }

    private bool Ready => !running && condition.IsMet && (string.IsNullOrEmpty(setFlag) || !StoryFlags.Has(setFlag));

    private IEnumerator Start()
    {
        if (mode != Mode.OnSceneStart) yield break;
        // Espera a que termine el fundido de entrada
        yield return new WaitForSecondsRealtime(startDelay);
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsBusy) yield return null;
        if (Ready) Run();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (mode != Mode.OnPlayerEnter || !Ready) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        Run();
    }

    private void Run()
    {
        running = true;
        DialogueUI.Play(dialogue, () =>
        {
            StoryFlags.Set(setFlag);
            running = false;
            onFinished?.Invoke();
        });
    }
}
