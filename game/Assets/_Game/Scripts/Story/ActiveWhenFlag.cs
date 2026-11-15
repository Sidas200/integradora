using UnityEngine;

// Activa o desactiva objetos según un flag de historia (al cargar y cuando cambia).
public class ActiveWhenFlag : MonoBehaviour
{
    [SerializeField] private string flag;
    [SerializeField] private bool activeWhenSet = true;
    [SerializeField] private GameObject[] targets;

    public void Setup(string flagName, bool whenSet, params GameObject[] objects)
    {
        flag = flagName;
        activeWhenSet = whenSet;
        targets = objects;
    }

    private void OnEnable() => StoryFlags.Changed += OnFlag;
    private void OnDisable() => StoryFlags.Changed -= OnFlag;
    private void Start() => Apply();

    private void OnFlag(string f)
    {
        if (f == flag) Apply();
    }

    private void Apply()
    {
        bool active = StoryFlags.Has(flag) == activeWhenSet;
        foreach (var t in targets)
            if (t != null) t.SetActive(active);
    }
}
