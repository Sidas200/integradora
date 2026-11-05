using System.Collections;
using UnityEngine;

// Efectos de los ataques del guardián: llamas que brotan en la zona marcada durante el aviso,
// estallido de fuego, anillo de polvo y escombros en el golpe, polvo y chispas en la embestida.
// Los sistemas de partículas los crea el builder; aquí solo se emiten partículas sueltas.
public class BossFx : MonoBehaviour
{
    [SerializeField] private ParticleSystem fire;
    [SerializeField] private ParticleSystem dust;
    [SerializeField] private ParticleSystem debris;
    [SerializeField] private ParticleSystem sparks;

    private static readonly Color FireCore = new Color(1f, 0.85f, 0.35f);
    private static readonly Color DustColor = new Color(0.78f, 0.72f, 0.64f, 0.85f);
    private static readonly Color Gold = new Color(1f, 0.86f, 0.45f);

    public void Setup(ParticleSystem fireSystem, ParticleSystem dustSystem, ParticleSystem debrisSystem, ParticleSystem sparkSystem)
    {
        fire = fireSystem;
        dust = dustSystem;
        debris = debrisSystem;
        sparks = sparkSystem;
    }

    private void Awake()
    {
        // Los sistemas viven sueltos en el mundo: así no se mueven ni se ordenan con el guardián
        foreach (var ps in new[] { fire, dust, debris, sparks })
            if (ps != null) ps.transform.SetParent(null, true);
    }

    private void OnDestroy()
    {
        foreach (var ps in new[] { fire, dust, debris, sparks })
            if (ps != null) Destroy(ps.gameObject);
    }

    // ---------------------------------------------------------------- avisos

    // Llamas que brotan dentro de la elipse marcada mientras dura el aviso.
    public void TelegraphFlames(Vector2 center, float radius, float duration) =>
        StartCoroutine(FlamesRoutine(duration, () =>
        {
            var p = Random.insideUnitCircle;
            return center + new Vector2(p.x * radius, p.y * radius * 0.5f);
        }));

    // Llamas a lo largo de la franja de la embestida.
    public void LineFlames(Vector2 start, Vector2 dir, float length, float width, float duration)
    {
        var side = new Vector2(-dir.y, dir.x);
        StartCoroutine(FlamesRoutine(duration, () =>
            start + dir * Random.Range(0.1f, length) + side * Random.Range(-width, width) * 0.5f));
    }

    private IEnumerator FlamesRoutine(float duration, System.Func<Vector2> point)
    {
        float acc = 0f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            // Cada vez más llamas a medida que se acerca el golpe
            acc += Time.deltaTime * Mathf.Lerp(30f, 110f, t / duration);
            while (acc >= 1f)
            {
                acc -= 1f;
                Emit(fire, point(), new Vector2(0f, Random.Range(0.2f, 0.45f)), Random.Range(0.09f, 0.15f), Random.Range(0.4f, 0.65f), FireCore);
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------------- impactos

    public void SlamImpact(Vector2 center, float radius)
    {
        // Anillo de fuego que se abre desde el centro
        for (int i = 0; i < 60; i++)
        {
            float a = i / 60f * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f);
            Emit(fire, center + dir * radius * Random.Range(0.2f, 0.5f), dir * Random.Range(0.9f, 1.6f) + Vector2.up * 0.2f,
                Random.Range(0.13f, 0.2f), Random.Range(0.4f, 0.6f), FireCore);
        }
        // Polvo en el borde de la zona
        for (int i = 0; i < 36; i++)
        {
            float a = i / 36f * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f);
            Emit(dust, center + dir * radius, dir * Random.Range(0.25f, 0.5f), Random.Range(0.1f, 0.16f), Random.Range(0.6f, 0.9f), DustColor);
        }
        // Escombros que saltan
        for (int i = 0; i < 14; i++)
        {
            var dir = Random.insideUnitCircle.normalized;
            Emit(debris, center + dir * 0.1f, new Vector2(dir.x * Random.Range(0.6f, 1.2f), Random.Range(1.2f, 2f)),
                Random.Range(0.04f, 0.07f), Random.Range(0.5f, 0.8f), Color.white);
        }
        CameraShake.Shake(0.05f, 0.35f);
    }

    // Llamarlo en cada paso de la embestida: polvo detrás y chispas en el suelo.
    public void ChargeStep(Vector2 position, Vector2 dir)
    {
        var back = -dir;
        Emit(dust, position + back * 0.1f + Random.insideUnitCircle * 0.05f, back * 0.3f + Vector2.up * 0.1f,
            Random.Range(0.07f, 0.12f), Random.Range(0.4f, 0.6f), DustColor);
        if (Random.value < 0.5f)
            Emit(sparks, position, back * Random.Range(0.6f, 1.2f) + Random.insideUnitCircle * 0.4f,
                0.03f, Random.Range(0.2f, 0.35f), new Color(1f, 0.6f, 0.25f));
    }

    public void ChargeEnd(Vector2 position)
    {
        for (int i = 0; i < 16; i++)
        {
            var dir = Random.insideUnitCircle.normalized;
            Emit(dust, position, new Vector2(dir.x, dir.y * 0.5f) * Random.Range(0.3f, 0.6f), Random.Range(0.08f, 0.13f), 0.6f, DustColor);
        }
        CameraShake.Shake(0.03f, 0.2f);
    }

    // Despertar: chispas doradas que suben y un temblor largo.
    public void WakeBurst(Vector2 position)
    {
        for (int i = 0; i < 40; i++)
            Emit(sparks, position + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0f, 0.4f)),
                new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.5f, 1.3f)), Random.Range(0.025f, 0.045f), Random.Range(0.6f, 1.1f), Gold);
        CameraShake.Shake(0.04f, 0.8f);
    }

    private static void Emit(ParticleSystem ps, Vector2 position, Vector2 velocity, float size, float life, Color color)
    {
        if (ps == null) return;
        var p = new ParticleSystem.EmitParams
        {
            position = new Vector3(position.x, position.y, 0f),
            velocity = velocity,
            startSize = size,
            startLifetime = life,
            startColor = color,
            applyShapeToPosition = false
        };
        ps.Emit(p, 1);
    }
}
