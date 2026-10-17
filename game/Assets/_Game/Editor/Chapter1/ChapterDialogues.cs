using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools.Chapter1
{
    // Textos del capítulo 1. Genera un DialogueData por diálogo en Data/Chapter1/Dialogues.
    // Para editar un diálogo a mano: márcalo como "Locked By Designer" y el builder no lo tocará.
    public static class ChapterDialogues
    {
        public const string Folder = "Assets/_Game/Data/Chapter1/Dialogues";

        // Nombres propuestos (ver docs/claude/pending-names.md)
        public const string Tsuki = "Tsuki";
        public const string Kaede = "Kaede";
        public const string Unknown = "???";
        public const string Aoi = "Aoi";
        public const string Hikari = "Hikari";
        public const string Varn = "Varn";
        public const string Narrator = "";

        private static readonly Dictionary<string, DialogueData> cache = new Dictionary<string, DialogueData>();

        public static DialogueData Get(string id)
        {
            if (cache.TryGetValue(id, out var d) && d != null) return d;
            d = AssetDatabase.LoadAssetAtPath<DialogueData>($"{Folder}/{id}.asset");
            if (d == null) Debug.LogError("[Chapter1] Falta el diálogo " + id);
            cache[id] = d;
            return d;
        }

        [MenuItem("Tools/Claude/Chapter 1/3b - Generate Dialogues")]
        public static void GenerateAll()
        {
            EnsureFolder();
            cache.Clear();

            // ---------------- Intro
            Make("intro", new[]
            {
                L(Narrator, "Hace mil años, un rey desafió a los dioses para salvar a la humanidad."),
                L(Narrator, "Lo acompañaron un pecador, una bestia y cuatro doncellas que daban poder a su espada."),
                L(Narrator, "Al final, las doncellas quedaron selladas en la espada. Al rey lo llamaron Rulebreaker."),
                L(Narrator, "Los humanos bendecidos fueron aislados en islas del cielo. Y el mundo siguió adelante."),
                L(Narrator, "Mil años después..."),
                L(Tsuki, "¡Te tengo! ...¿Estás bien? Caíste del cielo."),
                L(Hikari, "Gracias por atraparme. Me llamo Hikari."),
                L(Tsuki, "Esa marca en tu mano... ¿te duele?"),
                L(Hikari, "Dicen que es una sentencia. Pero contigo cerca no tengo miedo."),
                L(Tsuki, "Entonces no me voy a ir. Te lo prometo: siempre vamos a estar juntos."),
                L(Narrator, "Un rayo de luz cayó del cielo. Una figura sin rostro tomó a Hikari y subió con ella."),
                L(Tsuki, "¡Hikari!"),
                L(Tsuki, "Te hice una promesa... y si para cumplirla tengo que desafiar al mismo cielo, lo haré."),
                L(Narrator, "Hay promesas tan importantes que obligan a desafiar al mismo cielo."),
                L(Narrator, "Tsuki saltó hacia la luz."),
            });

            // ---------------- Isla de las Flores
            Make("island_wake", new[]
            {
                L(Tsuki, "...¿Dónde estoy? Hay flores por todas partes."),
                L(Tsuki, "Hikari... la luz se la llevó hacia arriba. Tengo que encontrarla."),
                L(Tsuki, "Mi mano. Esta marca... es la misma que tenía ella."),
                L(Narrator, "Muévete con WASD o las flechas. Pulsa E cerca de algo para interactuar."),
            });
            Make("cabin_inside", new[]
            {
                L(Tsuki, "Está abandonada. Nadie ha vivido aquí en mucho tiempo."),
            });
            Make("sword_pickup", new[]
            {
                L(Narrator, "Una espada carmesí, sin filo. Pesa menos de lo que parece."),
                L(Tsuki, "Es como si me estuviera esperando... Me la llevo."),
                L(Narrator, "Obtuviste la Espada carmesí. Clic izquierdo: atacar. Espacio: esquivar."),
            });
            Make("island_vanish", new[]
            {
                L(Tsuki, "La isla... ¡se está desvaneciendo!"),
                L(Tsuki, "Ya no hay vuelta atrás. Solo hacia adelante."),
            });

            // ---------------- Llanura del Eco
            Make("researcher_fall", new[]
            {
                L(Unknown, "¡Aaah! ...Auch. Esa rama no era tan firme como pensaba."),
                L(Unknown, "Tú... no tienes alas de luz ni ojos de piedra. Eres humano. Un humano de verdad."),
                L(Tsuki, "¿Tan raro es?"),
                L(Unknown, "Aquí, sí. Y esa marca en tu mano es la de Rulebreaker. Si alguien más la ve, te ejecutan."),
                L(Unknown, "Tranquilo, no voy a delatarte. Me da demasiada curiosidad."),
                L(Kaede, "Soy Kaede. Investigo cosas que no debería. Tengo una base escondida entre los árboles, al oeste."),
                L(Kaede, "Hay una forja. Te dejo usarla si me dejas estudiarte. ¿Trato?"),
                L(Tsuki, "Busco a una chica que se llevaron con un rayo de luz. Si me ayudas, acepto."),
                L(Kaede, "Trato hecho. Te veo en el campamento: sigue el camino hacia el oeste."),
            });

            // ---------------- Campamento
            Make("camp_briefing", new[]
            {
                L(Kaede, "Bienvenido a mi escondite. Aquí no llegan los ojos del cielo."),
                L(Kaede, "Escucha bien. Esta isla tiene cuatro zonas. En el centro está el Gran Juzgado: el dios de la justicia."),
                L(Kaede, "Si se llevaron a tu amiga, seguro está ahí. Pero para entrar necesitas los permisos de los cuatro guardianes."),
                L(Kaede, "En cada zona hay un santuario. En cada uno duerme una de las cuatro doncellas de la leyenda."),
                L(Tsuki, "¿Las doncellas de la espada del rey?"),
                L(Kaede, "Las mismas. Y creo que tu espada carmesí puede despertarlas."),
                L(Kaede, "Otra cosa: las armas normales no hieren a un dios. Ni a sus guardianes."),
                L(Kaede, "El guardián de esta zona espera al este, en el Bosque Quebrado, detrás de un sello."),
                L(Kaede, "Primero, el santuario del norte. Su puerta solo se abre con un Alma pequeña."),
                L(Kaede, "Los Custodios de ruinas guardan almas dentro. Vence a uno y la tendrás."),
                L(Kaede, "Con las Almas antiguas y los Circuitos que sueltan los golems forjamos un Módulo de espada."),
                L(Kaede, "Y el guardián duerme. Para despertarlo necesitarás una Gota de Amrita: también sale de la forja."),
                L(Kaede, "Si te sobra cuero y restos de slime, fórjate una armadura. La vas a necesitar."),
                L(Kaede, "Antes de salir, toca el cristal: guarda tu avance."),
            });
            Make("camp_materials", new[]
            {
                L(Kaede, "El santuario está al norte de la llanura. Su puerta pide un Alma pequeña."),
                L(Kaede, "Los Custodios de ruinas la guardan. Los slimes sueltan flores y restos para pociones y armadura."),
            });
            Make("camp_after_maiden", new[]
            {
                L(Kaede, "¿La doncella te habló? ¡Entonces la espada sí responde!"),
                L(Kaede, "Ahora forja el Módulo de espada: 10 Almas antiguas y 5 Circuitos antiguos."),
                L(Kaede, "Con él despierta la espada, y ese sello no te detendrá."),
            });
            Make("camp_upgraded", new[]
            {
                L(Kaede, "Esa espada ya puede herir a un guardián. Ve al Bosque Quebrado, al este."),
                L(Kaede, "No olvides la Gota de Amrita: 5 Flores destello y 3 Almas antiguas. Sin ella, Varn no despertará."),
                L(Kaede, "Y no te mueras. Todavía no termino de estudiarte."),
            });
            Make("camp_after_permit", new[]
            {
                L(Kaede, "¿Un permiso de guardián? ¿En tu primer intento?"),
                L(Kaede, "Faltan tres zonas... pero ahora sé que se puede. Vamos por el siguiente."),
            });

            // ---------------- Santuario
            Make("sanctuary_door_locked", new[]
            {
                L(Narrator, "Una puerta de cristal azul. En el centro hay un hueco con forma de alma."),
                L(Tsuki, "Kaede dijo que los Custodios de ruinas guardan almas pequeñas..."),
            });
            Make("sanctuary_door_open", new[]
            {
                L(Narrator, "El Alma pequeña encaja en el hueco. La puerta se disuelve en luz azul."),
            });
            Make("sanctuary_enter", new[] { L(Tsuki, "Hace frío aquí... y la espada vibra.") });
            Make("trial_intro", new[]
            {
                L(Narrator, "La espada carmesí brilla al acercarse a la estatua. Una voz resuena en la sala."),
                L(Unknown, "Portador... muchos trajeron espadas. Ninguna era digna."),
                L(Unknown, "Demuestra que la tuya lo es. Sobrevive a la prueba."),
                L(Narrator, "La entrada se sella. Algo despierta en los círculos del suelo."),
            });
            Make("maiden_awaken", new[]
            {
                L(Narrator, "La espada carmesí brilla. La piedra de la estatua se agrieta... y algo despierta."),
                L(Aoi, "...Mil años. Ha pasado mucho tiempo, portador."),
                L(Tsuki, "¿Eres una de las doncellas?"),
                L(Aoi, "Soy Aoi, la primera. Mi poder sigue en esa espada, pero aún duerme."),
                L(Aoi, "Fórjale un Módulo con las almas de los golems. Solo así romperá el sello del guardián."),
                L(Aoi, "Ve. Cuando la espada despierte del todo, yo despertaré contigo."),
            });
            Make("maiden_hint", new[] { L(Aoi, "El Módulo de espada primero. Después, el sello del este. No lo olvides.") });

            // ---------------- Bosque Quebrado
            Make("zone2_enter", new[]
            {
                L(Tsuki, "Los árboles están secos... como si algo los hubiera quemado desde adentro."),
                L(Tsuki, "Hay algo vigilando entre las ruinas. Con cuidado."),
            });

            Make("seal_reject", new[]
            {
                L(Narrator, "Un sello carmesí cubre el paso. Al tocarlo, la espada rebota sin dejar marca."),
                L(Tsuki, "No es suficiente... Kaede dijo que necesita el Módulo de espada de la forja."),
            });
            Make("seal_open", new[]
            {
                L(Narrator, "La espada mejorada brilla. El sello se agrieta y se deshace en chispas rojas."),
                L(Tsuki, "El camino está libre. Allá voy."),
            });

            // ---------------- Umbral del Guardián
            Make("guardian_sleeping", new[]
            {
                L(Narrator, "Un gigante de piedra duerme en el centro del umbral, sobre su arma."),
                L(Tsuki, "Así que este es el guardián... Kaede dijo que solo la Amrita lo despierta."),
                L(Narrator, "Acércate y ofrécele la Gota de Amrita (E frente a él, o G)."),
            });
            Make("guardian_no_amrita", new[]
            {
                L(Tsuki, "No tengo ninguna Gota de Amrita. Kaede puede forjarla: 5 Flores destello y 3 Almas antiguas."),
            });
            Make("guardian_intro", new[]
            {
                L(Narrator, "La Amrita cae sobre la piedra. Los ojos del guardián se encienden."),
                L(Varn, "...¿Quién me despierta? Un humano... con la marca de Rulebreaker."),
                L(Varn, "Soy Varn, guardián del umbral. Ningún condenado pasa más allá de este punto."),
                L(Tsuki, "No vine a pedir permiso. Vine por alguien a quien le hice una promesa."),
                L(Varn, "Entonces demuestra que tu espada puede herir a un guardián."),
                L(Narrator, "Sal de las zonas rojas antes del golpe. Ataca cuando Varn quede vulnerable."),
            });
            Make("guardian_again", new[]
            {
                L(Varn, "Volviste. Tu terquedad es casi digna de respeto."),
                L(Narrator, "Sal de las zonas rojas antes del golpe. Ataca cuando Varn quede vulnerable."),
            });
            Make("guardian_defeat", new[]
            {
                L(Varn, "...Esa espada. No es una espada cualquiera."),
                L(Varn, "Has vencido, humano. La ley del juicio me obliga a reconocerlo."),
                L(Varn, "Toma mi permiso. Pero recuerda: el Gran Juzgado no perdona a quien rompe las reglas."),
            });
            Make("permit_get", new[]
            {
                L(Narrator, "Obtuviste el Permiso del Guardián (1 de 4)."),
                L(Tsuki, "Uno menos. Hikari, ya voy."),
            });

            // ---------------- Cierre
            Make("demo_end", new[]
            {
                L(Narrator, "Tsuki obtuvo el primer permiso de guardián."),
                L(Narrator, "Quedan tres zonas, tres guardianes y tres doncellas dormidas."),
                L(Narrator, "En el centro de la isla, el Gran Juzgado espera. Y en algún lugar, Hikari también."),
                L(Tsuki, "Espérame, Hikari. Aunque tenga que desafiar al cielo entero, voy a cumplir mi promesa."),
            });

            AssetDatabase.SaveAssets();
            Debug.Log("[Chapter1] Diálogos generados en " + Folder);
        }

        private static DialogueData.Line L(string speaker, string text) =>
            new DialogueData.Line { speaker = speaker, text = text, portrait = PortraitFor(speaker) };

        private static Sprite PortraitFor(string speaker)
        {
            switch (speaker)
            {
                case Tsuki: return PlaceholderArtGenerator.Load("placeholder_portrait_tsuki");
                case Kaede:
                case Unknown: return PlaceholderArtGenerator.Load("placeholder_portrait_researcher");
                case Aoi: return PlaceholderArtGenerator.Load("placeholder_portrait_maiden");
                case Hikari: return PlaceholderArtGenerator.Load("placeholder_portrait_hikari");
                case Varn: return PlaceholderArtGenerator.Load("placeholder_portrait_guardian");
                default: return null;
            }
        }

        public static void Make(string id, DialogueData.Line[] lines)
        {
            string path = $"{Folder}/{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(data, path);
            }
            else if (data.lockedByDesigner)
            {
                cache[id] = data;
                return;
            }
            data.lines = new List<DialogueData.Line>(lines);
            EditorUtility.SetDirty(data);
            cache[id] = data;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Chapter1")) AssetDatabase.CreateFolder("Assets/_Game/Data", "Chapter1");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Data/Chapter1", "Dialogues");
        }
    }
}
