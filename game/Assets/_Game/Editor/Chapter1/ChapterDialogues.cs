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
                L(Narrator, "Muévete con {move}. Pulsa {interact} cerca de algo para interactuar."),
            });
            Make("cabin_inside", new[]
            {
                L(Tsuki, "Está abandonada. Nadie ha vivido aquí en mucho tiempo."),
            });
            Make("sword_pickup", new[]
            {
                L(Narrator, "Una espada carmesí, sin filo. Pesa menos de lo que parece."),
                L(Tsuki, "Es como si me estuviera esperando... Me la llevo."),
                L(Narrator, "Obtuviste la Espada carmesí. {attack}: atacar. {dash}: esquivar."),
            });
            Make("island_vanish", new[]
            {
                L(Tsuki, "La isla... ¡se está desvaneciendo!"),
                L(Tsuki, "Ya no hay vuelta atrás. Solo hacia adelante."),
            });

            // ---------------- Llanura del Eco
            // Conocer a Kaede (rara, se emociona con todo lo que puede investigar): el trato
            MakeBranch("researcher_fall", new[]
            {
                L(Unknown, "¡Aaah! ...Auch. Esa rama no era tan firme como pensaba."),
                L(Unknown, "Espera... ¡espera, espera! No tienes alas de luz ni ojos de piedra. ¡Eres humano! ¡Un humano de verdad!"),
                L(Tsuki, "...¿Tan raro es?"),
                L(Unknown, "¿Raro? ¡Es fascinante! Y esa marca en tu mano es la de Rulebreaker. Si alguien más la ve, te ejecutan."),
                L(Unknown, "Tranquilo, no voy a delatarte. Me das demasiada curiosidad como para perderte."),
                L(Kaede, "Soy Kaede. Investigo cosas que no debería. Tengo una base escondida entre los árboles, al oeste."),
                L(Kaede, "Hay una forja. Te dejo usarla si me dejas estudiarte. ¿Trato? Di que sí. Por favor, di que sí."),
            }, new[]
            {
                L(Kaede, "¡Trato hecho! Te veo en el campamento: sigue el camino hacia el oeste."),
                L(Kaede, "Y no te mueras antes de que te estudie, ¿eh?"),
            },
            O("Acepto. Busco a alguien y necesito ayuda.", KaedeAccept,
                L(Tsuki, "Busco a una chica que se llevaron con un rayo de luz. Si me ayudas, acepto."),
                L(Kaede, "¿Un rayo de luz? ¿Del cielo? ¡Ay, esto se pone cada vez mejor!")),
            O("¿Estudiarme? ¿Qué me vas a hacer?", KaedeQuestion,
                L(Tsuki, "¿Estudiarme? ¿Qué me vas a hacer exactamente?"),
                L(Kaede, "Nada que duela. ...Casi nada. Preguntas, mediciones, tal vez una muestra de esa marca. Pequeña. Diminuta."),
                L(Tsuki, "...Está bien. Pero a cambio me ayudas a encontrar a una chica que se llevó el cielo.")),
            O("No confío en ti.", KaedeDistrust,
                L(Tsuki, "No confío en ti."),
                L(Kaede, "¡Perfecto! La desconfianza también es un dato. Lo anoto."),
                L(Kaede, "Pero sin mi forja no le harás ni un rasguño a un guardián. Piénsalo."),
                L(Tsuki, "...Busco a una chica que se llevó un rayo de luz. Ayúdame a encontrarla y hay trato.")));

            // ---------------- Campamento
            // Primera plática en el campamento. El saludo depende de cómo trataste a Kaede al conocerla.
            MakeBranch("camp_briefing", new[]
            {
                IfNot(KaedeQuestion + "," + KaedeDistrust, Kaede, "¡Llegaste! Bienvenido a mi escondite. Aquí no llegan los ojos del cielo."),
                If(KaedeQuestion, Kaede, "¡Llegaste! Tranquilo, hoy solo vamos a hablar. Las mediciones, después. ...Bueno, quizás una."),
                If(KaedeDistrust, Kaede, "Viniste, aunque no confías en mí. ¿Ves? Sigo sin delatarte. Punto para mí."),
                L(Kaede, "Escucha bien. Esta isla tiene cuatro zonas. En el centro está el Gran Juzgado: el dios de la justicia."),
                L(Kaede, "Si se llevaron a tu amiga, seguro está ahí. Pero para entrar necesitas los permisos de los cuatro guardianes."),
            }, new[]
            {
                L(Kaede, "En cada zona hay un santuario. En cada uno duerme una de las cuatro doncellas de la leyenda."),
                L(Tsuki, "¿Las doncellas de la espada del rey?"),
                L(Kaede, "Las mismas. Y creo que tu espada carmesí puede despertarlas. ¡Quiero verlo con mis propios ojos!"),
                L(Kaede, "Otra cosa: las armas normales no hieren a un dios. Ni a sus guardianes."),
                L(Kaede, "El guardián de esta zona espera al este, en el Bosque Quebrado, detrás de un sello."),
                L(Kaede, "Primero, el santuario del norte. Su puerta solo se abre con un Alma pequeña."),
                L(Kaede, "Los Custodios de ruinas guardan almas dentro. Vence a uno y la tendrás."),
                L(Kaede, "Con las Almas antiguas y los Circuitos que sueltan los golems forjamos un Módulo de espada."),
                L(Kaede, "Y el guardián duerme. Para despertarlo necesitarás una Gota de Amrita: también sale de la forja."),
                L(Kaede, "Si te sobra cuero y restos de slime, fórjate una armadura. La vas a necesitar."),
                L(Kaede, "Antes de salir, toca el cristal: guarda tu avance."),
            },
            O("Entonces iré por los cuatro.", "kaede_briefing_resolve",
                L(Tsuki, "Entonces iré por los cuatro."),
                L(Kaede, "¡Eso! Cuatro guardianes, cuatro permisos... ¡cuatro veces más datos!")),
            O("¿Por qué me ayudas de verdad?", "kaede_briefing_why",
                L(Tsuki, "¿Por qué me ayudas de verdad?"),
                L(Kaede, "Llevo años leyendo sobre Rulebreaker... y tú eres la primera prueba de que no era un cuento. ¿Sabes lo emocionante que es eso?"),
                L(Kaede, "Además, alguien tiene que mantenerte vivo. Los sujetos de estudio muertos no contestan preguntas.")),
            O("¿Y si mejor fuerzo la entrada?", "kaede_briefing_force",
                L(Tsuki, "¿Y si mejor fuerzo la entrada?"),
                L(Kaede, "Otros lo intentaron. Ya nadie recuerda sus nombres. ...Yo sí. Los tengo anotados."),
                L(Kaede, "Con permisos. Créeme.")));
            Make("camp_materials", new[]
            {
                IfNot(KaedeDistrust, Kaede, "¡Volviste! ¿Encontraste algo raro? Lo que sea. Me sirve todo."),
                If(KaedeDistrust, Kaede, "¿Otra vez por aquí? Empiezo a pensar que sí confías en mí."),
                L(Kaede, "El santuario está al norte de la llanura. Su puerta pide un Alma pequeña."),
                L(Kaede, "Los Custodios de ruinas la guardan. Los slimes sueltan flores y restos para pociones y armadura."),
            });
            Make("camp_after_maiden", new[]
            {
                L(Kaede, "¿¡La doncella te habló!? ¿Cómo era su voz? ¿Brillaba? ¡Cuéntamelo todo!"),
                L(Kaede, "¿Y dices que ahora vive dentro de tu espada? ...Necesito una libreta nueva. O tres."),
                If(KaedeDistrust, Kaede, "Y pensar que no confiabas en mí. Mira todo lo que estamos descubriendo juntos."),
                L(Kaede, "Ahora forja el Módulo de espada: 10 Almas antiguas y 5 Circuitos antiguos."),
                L(Kaede, "Con él despierta la espada, y ese sello no te detendrá."),
            });
            Make("camp_upgraded", new[]
            {
                L(Kaede, "Esa espada ya puede herir a un guardián. ¡Mira cómo vibra! ...Perdón. Sigo."),
                L(Kaede, "Ve al Bosque Quebrado, al este. No olvides la Gota de Amrita: 5 Flores destello y 3 Almas antiguas. Sin ella, Varn no despertará."),
                IfNot(KaedeQuestion, Kaede, "Y no te mueras. Todavía no termino de estudiarte."),
                If(KaedeQuestion, Kaede, "Y no te mueras. Me debes una medición, ¿recuerdas?"),
            });
            Make("camp_after_permit", new[]
            {
                L(Kaede, "¿Un permiso de guardián? ¿En tu primer intento? ¡Déjame verlo! No, no lo guardes, ¡déjame verlo!"),
                If(VarnRespect, Kaede, "¿Que Varn te llamó \"guerrero\"? Los guardianes no hablan así de los humanos. Eres todavía más raro de lo que pensaba. Me encanta."),
                If("kaede_briefing_resolve", Kaede, "Dijiste que irías por los cuatro. Uno menos."),
                If(KaedeDistrust, Kaede, "Para alguien que no confiaba en mí, hacemos buen equipo, ¿no?"),
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
            // Despertar a Aoi (le tiene un profundo respeto al portador). Al terminar vuelve a la espada
            // y le da el Paso de Rayo (MaidenStatue).
            MakeBranch("maiden_awaken", new[]
            {
                L(Narrator, "La espada carmesí brilla. La piedra de la estatua se agrieta... y algo despierta."),
                L(Aoi, "...Mil años esperando. Y por fin, un portador digno."),
                L(Tsuki, "¿Eres una de las doncellas?"),
                L(Aoi, "Soy Aoi, la primera. Es un honor conocerte, portador. Superaste mi prueba sin dudar ni un instante."),
            }, new[]
            {
                L(Aoi, "Mi lugar está en tu espada. Permíteme volver a ella... y prestarte mi rayo."),
                L(Aoi, "Las demás doncellas aún duermen. Fórjale un Módulo con las almas de los golems: así romperás el sello del guardián."),
                L(Aoi, "Ve, portador. Desde ahora, viajo contigo."),
            },
            O("¿Conoces a Hikari? Tiene la misma marca.", "aoi_ask_hikari",
                L(Tsuki, "Busco a Hikari. Tiene la misma marca que yo. ¿Sabes algo de ella?"),
                L(Aoi, "Esa marca la llevan quienes el cielo escoge para juzgar."),
                L(Aoi, "Si se la llevaron, aún vive. Y con alguien como tú buscándola, no está sola.")),
            O("¿Por qué la espada me eligió a mí?", "aoi_ask_sword",
                L(Tsuki, "¿Por qué esta espada me eligió a mí?"),
                L(Aoi, "No te eligió, portador. Te reconoció."),
                L(Aoi, "Tienes la misma terquedad que él... y un corazón más noble. Me inclino ante ti.")),
            O("¿Qué le pasó al rey?", "aoi_ask_king",
                L(Tsuki, "¿Qué le pasó al rey de la leyenda?"),
                L(Aoi, "Rompió las reglas del cielo para salvarnos a todos... y el cielo nunca lo perdonó."),
                L(Aoi, "Tú me recuerdas a él. Por eso, con todo respeto, te pido algo: no repitas su final.")));
            // La estatua ya está vacía: Aoi habla desde la espada
            Make("maiden_hint", new[]
            {
                L(Narrator, "La estatua está vacía. La espada carmesí vibra en tu mano."),
                L(Aoi, "El Módulo de espada primero, portador. Después, el sello del este. Estoy contigo."),
            });

            // Después de que Aoi entra a la espada y del Paso de Rayo de demostración (SwordCompanion)
            Make("maiden_join", new[]
            {
                L(Tsuki, "...¿Qué fue eso? Por un instante, yo era el rayo."),
                L(Aoi, "Ese es mi poder, portador. Mientras me lleves contigo, nada podrá alcanzarte."),
                L(Tsuki, "Gracias, Aoi. Te prometo que lo usaré bien."),
                L(Aoi, "Lo sé. Si alguna vez dudas del camino, llámame. Estaré en tu espada."),
                L(Narrator, "Aoi volvió a la espada carmesí. Desbloqueaste el Paso de Rayo."),
                L(Narrator, "Presiona {dash} para esquivar convertido en rayo: dañas a los enemigos que atraviesas."),
                L(Narrator, "Presiona {sword} en cualquier momento para hablar con Aoi y pedirle consejo."),
            });

            // Hablar con Aoi en la espada (Q / R3). Sus consejos cambian según cómo va la historia.
            MakeBranch("aoi_advice", new[]
            {
                L(Aoi, "¿Me llamabas, portador?"),
            }, null,
            O("¿Qué debo hacer ahora?", null,
                L(Tsuki, "¿Qué debo hacer ahora?"),
                IfNot(SwordUpgradedFlag, Aoi, "Forja el Módulo de espada en el campamento de Kaede: 10 Almas antiguas y 5 Circuitos antiguos."),
                IfNot(SwordUpgradedFlag, Aoi, "Los golems de la llanura guardan ambos. No temas: yo correré contigo."),
                Cond(SwordUpgradedFlag, SealOpenedFlag, Aoi, "La espada ya despertó. Ve al Bosque Quebrado, al este, y rompe el sello carmesí."),
                Cond(SealOpenedFlag, GuardianAwakeFlag + "," + PermitFlag, Aoi, "El guardián duerme más allá del sello. Para despertarlo necesitas una Gota de Amrita."),
                Cond(SealOpenedFlag, GuardianAwakeFlag + "," + PermitFlag, Aoi, "Kaede puede forjarla: 5 Flores destello y 3 Almas antiguas."),
                Cond(GuardianAwakeFlag, PermitFlag, Aoi, "Varn te espera. Su permiso es el primer paso hacia el Gran Juzgado... y hacia Hikari."),
                If(PermitFlag, Aoi, "Tienes el primer permiso. Vuelve con Kaede: ella sabrá hacia dónde seguir."),
                If(PermitFlag, Aoi, "Estoy orgullosa de ti, portador.")),
            O("Dame un consejo de combate.", null,
                L(Tsuki, "Dame un consejo de combate."),
                IfNot(GuardianAwakeFlag, Aoi, "Cuando un enemigo ataque, cruza a través de él con mi rayo: lo dañarás sin recibir el golpe."),
                IfNot(GuardianAwakeFlag, Aoi, "Y si esquivas justo a tiempo, el mundo se volverá lento para ti."),
                Cond(GuardianAwakeFlag, PermitFlag, Aoi, "Contra Varn, sal de las zonas rojas antes del golpe y ataca cuando quede vulnerable."),
                Cond(GuardianAwakeFlag, PermitFlag, Aoi, "Cuando desenvaine, su golpe al suelo lanza una onda. Aléjate del círculo grande... o crúzalo como rayo."),
                If(PermitFlag, Aoi, "Cada guardián será más fuerte que el anterior. Pero tú también lo serás, portador.")),
            O("Háblame de ti, Aoi.", null,
                L(Tsuki, "Háblame de ti, Aoi."),
                L(Aoi, "Fui la primera doncella. Juré proteger al rey con la velocidad del rayo."),
                L(Aoi, "Mil años dormí en esa estatua, esperando a alguien digno. Me alegra que hayas sido tú.")));

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
                L(Narrator, "Acércate y ofrécele la Gota de Amrita ({interact} frente a él, o {amrita})."),
            });
            Make("guardian_no_amrita", new[]
            {
                L(Tsuki, "No tengo ninguna Gota de Amrita. Kaede puede forjarla: 5 Flores destello y 3 Almas antiguas."),
                L(Narrator, "Puedes volver al Bosque Quebrado por el camino de entrada (abajo a la izquierda)."),
            });
            // Despertar a Varn. Respeta a quien tiene agallas: retarlo de frente cambia cómo te habla después.
            MakeBranch("guardian_intro", new[]
            {
                L(Narrator, "La Amrita cae sobre la piedra. Los ojos del guardián se encienden."),
                L(Varn, "...¿Quién me despierta? Un humano... con la marca de Rulebreaker."),
                L(Varn, "Soy Varn, guardián del umbral. Ningún condenado pasa más allá de este punto."),
            }, new[]
            {
                L(Narrator, "Sal de las zonas rojas antes del golpe. Ataca cuando Varn quede vulnerable."),
            },
            O("Entonces pelea conmigo, Varn.", VarnRespect,
                L(Tsuki, "Si para pasar tengo que vencerte... entonces pelea conmigo, Varn."),
                L(Varn, "...Ja. Un humano que me reta de frente."),
                L(Varn, "Hace siglos que nadie tenía tantas agallas. Te concedo un combate digno. ¡En guardia!")),
            O("No soy un condenado.", "varn_intro_innocent",
                L(Tsuki, "Esta marca no la pedí. No soy un condenado."),
                L(Varn, "Todos dicen lo mismo antes del juicio."),
                L(Varn, "Levanta tu espada, humano.")),
            O("Déjame pasar y nadie sale herido.", "varn_intro_threat",
                L(Tsuki, "Déjame pasar y nadie sale herido."),
                L(Varn, "...Palabras grandes para alguien tan pequeño."),
                L(Varn, "Veamos si tu espada las respalda.")));
            Make("guardian_again", new[]
            {
                If(VarnRespect, Varn, "Volviste. Lo sabía: los que tienen agallas no se quedan en el suelo."),
                If(VarnRespect, Varn, "Levántate y pelea otra vez, guerrero."),
                IfNot(VarnRespect, Varn, "Volviste. Tu terquedad es casi digna de respeto."),
                L(Narrator, "Sal de las zonas rojas antes del golpe. Ataca cuando Varn quede vulnerable."),
            });
            Make("guardian_sword", new[]
            {
                L(Narrator, "Varn hunde la mano en su pecho y arranca una espada de luz roja. El cielo se cierra sobre el umbral."),
                If(VarnRespect, Varn, "Me obligas a desenvainar. Pocos han ganado ese honor."),
                If(VarnRespect, Varn, "Ahora pelearé con todo, humano. Te lo debo."),
                IfNot(VarnRespect, Varn, "Hace siglos que nadie me obligaba a desenvainar."),
                IfNot(VarnRespect, Varn, "Se acabó el juego, humano. Ahora sí voy a tomarte en serio."),
                L(Tsuki, "...Bien. Yo tampoco pienso contenerme."),
                L(Narrator, "Varn encadena sus ataques y su golpe al suelo lanza una onda. Sal también del círculo grande."),
            });
            // Varn vencido: si lo retaste de frente, te habla con respeto
            MakeBranch("guardian_defeat", new[]
            {
                If(VarnRespect, Varn, "...Esa espada. Y ese valor. No eres un humano cualquiera."),
                If(VarnRespect, Varn, "Me venciste de frente, como prometiste. Es un honor reconocerlo."),
                IfNot(VarnRespect, Varn, "...Esa espada. No es una espada cualquiera."),
                IfNot(VarnRespect, Varn, "Has vencido, humano. La ley del juicio me obliga a reconocerlo."),
            }, new[]
            {
                If(VarnRespect, Varn, "Toma mi permiso, guerrero. Si el Gran Juzgado te detiene, que sepan que Varn te reconoció."),
                IfNot(VarnRespect, Varn, "Toma mi permiso. Pero recuerda: el Gran Juzgado no perdona a quien rompe las reglas."),
            },
            O("¿Sabes adónde se llevaron a Hikari?", "varn_defeat_hikari",
                L(Tsuki, "Una chica con mi misma marca. Se la llevó un rayo de luz. ¿Adónde?"),
                L(Varn, "Al Gran Juzgado. Todos los marcados terminan ahí."),
                If(VarnRespect, Varn, "Si alguien puede llegar antes del juicio, eres tú."),
                IfNot(VarnRespect, Varn, "Si quieres llegar antes del juicio, no te detengas.")),
            O("¿Por qué sirves a ese dios?", "varn_defeat_why",
                L(Tsuki, "¿Por qué sirves a un dios que encierra a los humanos?"),
                L(Varn, "Sirvo a la ley. Sin ella, este mundo ya habría caído."),
                L(Varn, "...O eso me repito desde hace mil años.")),
            O("Peleaste bien.", "varn_defeat_respect",
                L(Tsuki, "Peleaste bien, Varn."),
                If(VarnRespect, Varn, "Viniendo de ti, lo acepto con orgullo."),
                IfNot(VarnRespect, Varn, "No necesito la lástima de un humano."),
                IfNot(VarnRespect, Varn, "...Pero acepto el respeto.")));
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

        // Línea que solo sale si ya se tienen estos flags (varios separados por coma)
        private static DialogueData.Line If(string flags, string speaker, string text)
        {
            var line = L(speaker, text);
            line.condition = new FlagCondition(flags, null);
            return line;
        }

        // Línea que solo sale si NO se tiene ninguno de estos flags
        private static DialogueData.Line IfNot(string flags, string speaker, string text)
        {
            var line = L(speaker, text);
            line.condition = new FlagCondition(null, flags);
            return line;
        }

        // Línea que pide unos flags y prohíbe otros
        private static DialogueData.Line Cond(string requires, string forbids, string speaker, string text)
        {
            var line = L(speaker, text);
            line.condition = new FlagCondition(requires, forbids);
            return line;
        }

        // Avance de la historia (para los consejos de Aoi)
        private const string SwordUpgradedFlag = "sword_upgraded";
        private const string SealOpenedFlag = "seal_opened";
        private const string GuardianAwakeFlag = "guardian_awakened";
        private const string PermitFlag = "permit_obtained";

        // Flags de las decisiones (los usan los diálogos de después)
        private const string KaedeAccept = "kaede_meet_accept";
        private const string KaedeQuestion = "kaede_meet_question";
        private const string KaedeDistrust = "kaede_meet_distrust";
        private const string VarnRespect = "varn_respect";

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

        public static DialogueData Make(string id, DialogueData.Line[] lines)
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
                return data;
            }
            data.lines = new List<DialogueData.Line>(lines);
            EditorUtility.SetDirty(data);
            cache[id] = data;
            return data;
        }

        // ---------------- Decisiones

        private struct Option
        {
            public string text;
            public string flag;
            public DialogueData.Line[] response;
        }

        // Una opción: el texto que se elige, un flag para recordar la decisión y la respuesta que provoca
        private static Option O(string text, string flag, params DialogueData.Line[] response) =>
            new Option { text = text, flag = flag, response = response };

        // Conversación con decisión. Se leen las líneas de 'before', se elige una opción, se lee su respuesta
        // y todas terminan con 'after'. Crea: id (antes + opciones), id_op1..3 (respuestas) e id_fin (el final común).
        private static void MakeBranch(string id, DialogueData.Line[] before, DialogueData.Line[] after, params Option[] options)
        {
            DialogueData end = after != null && after.Length > 0 ? Make(id + "_fin", after) : null;
            if (end != null) SetFlow(end, null, null);

            var choices = new List<DialogueData.Choice>();
            for (int i = 0; i < options.Length; i++)
            {
                var response = Make($"{id}_op{i + 1}", options[i].response);
                SetFlow(response, end, null);
                choices.Add(new DialogueData.Choice { text = options[i].text, next = response, setFlag = options[i].flag });
            }
            SetFlow(Make(id, before), null, choices);
        }

        private static void SetFlow(DialogueData data, DialogueData continueWith, List<DialogueData.Choice> choices)
        {
            if (data == null || data.lockedByDesigner) return;
            data.continueWith = continueWith;
            data.choices = choices ?? new List<DialogueData.Choice>();
            EditorUtility.SetDirty(data);
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data")) AssetDatabase.CreateFolder("Assets/_Game", "Data");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data/Chapter1")) AssetDatabase.CreateFolder("Assets/_Game/Data", "Chapter1");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game/Data/Chapter1", "Dialogues");
        }
    }
}
