using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LastEvidence.EditorTools
{
    /// <summary>Ensemble des données générées, transmis au constructeur de scènes.</summary>
    public class GeneratedContent
    {
        public GameDatabase database;
        public CaseData caseData;
        public readonly Dictionary<string, ItemData> items = new Dictionary<string, ItemData>();
        public readonly Dictionary<string, EvidenceData> evidence = new Dictionary<string, EvidenceData>();
        public readonly Dictionary<string, SuspectData> suspects = new Dictionary<string, SuspectData>();

        public ItemData Item(string id) => items[id];
        public EvidenceData Ev(string id) => evidence[id];
    }

    /// <summary>
    /// Crée les ScriptableObjects de l'affaire : suspects, preuves, objets, affaire et base de données.
    /// Toute l'histoire est définie ici (voir aussi Docs/Enquete.md).
    /// </summary>
    public static class ContentFactory
    {
        private const string SoFolder = ProjectGenerator.Root + "/ScriptableObjects";

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static GeneratedContent CreateAll()
        {
            var c = new GeneratedContent();
            CreateSuspects(c);
            CreateEvidence(c);
            CreateItems(c);
            CreateCase(c);

            var db = CreateOrLoad<GameDatabase>($"{ProjectGenerator.Root}/Resources/GameDatabase.asset");
            db.caseData = c.caseData;
            db.items = new List<ItemData>(c.items.Values);
            db.evidence = new List<EvidenceData>(c.evidence.Values);
            db.suspects = new List<SuspectData>(c.suspects.Values);
            db.pointerMaterial = MaterialLibrary.Pointer;
            db.defaultMaterial = MaterialLibrary.Get("Controller", new Color(0.3f, 0.3f, 0.32f));
            db.handMaterial = MaterialLibrary.Get("Skin", new Color(0.78f, 0.6f, 0.5f), 0.3f);
            EditorUtility.SetDirty(db);
            c.database = db;
            AssetDatabase.SaveAssets();
            return c;
        }

        // ------------------------------------------------------------------ Suspects

        private static void Suspect(GeneratedContent c, string id, string name, string role, string profile,
            string motive, string alibi, Color color)
        {
            var s = CreateOrLoad<SuspectData>($"{SoFolder}/Suspects/Suspect_{id}.asset");
            s.id = id; s.fullName = name; s.role = role; s.profile = profile;
            s.potentialMotive = motive; s.alibi = alibi; s.portraitColor = color;
            EditorUtility.SetDirty(s);
            c.suspects[id] = s;
        }

        private static void CreateSuspects(GeneratedContent c)
        {
            Suspect(c, "marc", "Marc Dubois", "Associé de la victime",
                "54 ans, cofondateur de Lemaire Capital. Élégant, influent, toujours au cœur des réseaux d'affaires.",
                "Victor s'apprêtait peut-être à rompre leur association.",
                "Affirme avoir passé toute la soirée au gala de la Chambre de commerce, de 20 h à 23 h 30.",
                new Color(0.45f, 0.55f, 0.68f));
            Suspect(c, "claire", "Claire Lemaire", "Nièce de la victime",
                "28 ans, étudiante en architecture, criblée de dettes. Seule famille de Victor.",
                "Principale héritière ; s'est disputée avec son oncle au sujet d'argent le soir du drame.",
                "Dit être rentrée directement chez elle après sa visite de 19 h.",
                new Color(0.75f, 0.55f, 0.45f));
            Suspect(c, "julien", "Julien Roy", "Ancien comptable",
                "41 ans, licencié en juillet pour des « erreurs comptables » qu'il conteste toujours.",
                "Rancune après son licenciement ; a envoyé une lettre de menace.",
                "Affirme ne pas avoir mis les pieds dans le bâtiment ce soir-là.",
                new Color(0.55f, 0.62f, 0.45f));
        }

        // ------------------------------------------------------------------ Preuves

        private static void Ev(GeneratedContent c, string id, string title, EvidenceCategory cat, string location,
            string description, string timeline = null, int minutes = -1, params string[] suspects)
        {
            var e = CreateOrLoad<EvidenceData>($"{SoFolder}/Evidence/Evidence_{id}.asset");
            e.id = id; e.title = title; e.category = cat; e.location = location; e.description = description;
            e.timelineEntry = timeline; e.timelineMinutes = minutes;
            e.relatedSuspects = System.Array.ConvertAll(suspects, s => c.suspects[s]);
            EditorUtility.SetDirty(e);
            c.evidence[id] = e;
        }

        private static void CreateEvidence(GeneratedContent c)
        {
            Ev(c, "ev_message", "Message anonyme", EvidenceCategory.Document, "Hall d'entrée",
                "« Victor Lemaire n'est pas mort d'un accident : il a été empoisonné. Les preuves sont cachées dans ce bâtiment. " +
                "Le temps garde la clé de son bureau : l'horloge s'est arrêtée à l'heure où l'on a découvert son corps. » - Un ami");
            Ev(c, "ev_police", "Rapport de police préliminaire", EvidenceCategory.Document, "Hall d'entrée (tableau d'affichage)",
                "Victor Lemaire, 61 ans, retrouvé sans vie dans son bureau le 14 octobre à 23 h par le gardien. " +
                "Conclusion : arrêt cardiaque (antécédents connus). Aucune autopsie. Dossier classé.",
                "23 h 00 - Le gardien découvre le corps de Victor", 1380);
            Ev(c, "ev_agenda", "Agenda de Victor", EvidenceCategory.Document, "Bureau de la victime",
                "Page du 14 octobre : « 19 h - Claire (encore l'argent…) » ; « 21 h - M. - dossier des virements. Garder les preuves sous la main. »",
                "21 h 00 - Rendez-vous prévu avec « M. » au sujet des virements", 1260, "marc", "claire");
            Ev(c, "ev_glass", "Verre de whisky", EvidenceCategory.Physical, "Bureau de la victime",
                "Au fond du verre de Victor, un dépôt de poudre blanche. Le whisky contenait autre chose : ce n'était pas un accident.");
            Ev(c, "ev_family_photo", "Photo des 20 ans de l'entreprise", EvidenceCategory.Photo, "Bureau de la victime",
                "Victor, Marc et Claire aux 20 ans de Lemaire Capital. Au verso, de la main de Victor : « Marc ne supporte plus que je vérifie ses comptes. »",
                null, -1, "marc");
            Ev(c, "ev_audit", "Rapport d'audit interne", EvidenceCategory.Document, "Coffre-fort du bureau",
                "Rapport confidentiel commandé par Victor : 1,2 million de dollars détournés en dix-huit mois vers la société « M.D. Holdings », " +
                "dont l'unique administrateur est Marc Dubois.", null, -1, "marc");
            Ev(c, "ev_claire_voicemail", "Message vocal de Claire", EvidenceCategory.Testimony, "Bureau (téléphone)",
                "Reçu à 20 h 05 : « Oncle Victor, c'est Claire. Pardon pour la dispute… Je suis partie vers 19 h 25 et je suis rentrée directement. " +
                "Je ne voulais pas te parler comme ça. Rappelle-moi. »",
                "19 h 00 - Claire rend visite à Victor ; ils se disputent au sujet d'argent", 1140, "claire");
            Ev(c, "ev_email_lawyer", "Courriel à l'avocat", EvidenceCategory.Document, "Ordinateur de Victor",
                "Victor à Me Gagnon, 13 octobre : « J'ai la preuve que Marc détourne des fonds depuis un an et demi. Je le confronte demain soir " +
                "et je porte plainte lundi. Préparez la dissolution de l'association. »", null, -1, "marc");
            Ev(c, "ev_email_julien", "Courriel de Julien Roy", EvidenceCategory.Document, "Ordinateur de Victor",
                "Julien Roy à Victor, 10 octobre : « Je passerai récupérer mes affaires personnelles aux archives. Mon badge fonctionne encore. " +
                "Je regrette ma lettre de septembre, écrite sous le coup de la colère. »", null, -1, "julien");
            Ev(c, "ev_transfers", "Relevés de virements", EvidenceCategory.Document, "Salle des archives",
                "Dix-neuf virements vers M.D. Holdings, tous autorisés avec la signature électronique de Marc Dubois.", null, -1, "marc");
            Ev(c, "ev_newspaper", "Article : licenciement contesté", EvidenceCategory.Document, "Salle des archives",
                "Le Courrier, 3 juillet : « Julien Roy, comptable licencié par Lemaire Capital, affirme servir de bouc émissaire " +
                "pour des irrégularités dont il ne serait pas responsable. »", null, -1, "julien");
            Ev(c, "ev_threat", "Lettre de menace", EvidenceCategory.Document, "Salle des archives",
                "Lettre signée J. Roy, 2 septembre : « Vous me le paierez. Un jour, tout le monde saura ce qui se passe dans votre entreprise. »",
                null, -1, "julien");
            Ev(c, "ev_will", "Testament de Victor", EvidenceCategory.Document, "Salle des archives",
                "Claire Lemaire est désignée légataire principale. Note manuscrite : « À modifier si Claire ne règle pas ses dettes. »",
                null, -1, "claire");
            Ev(c, "ev_gala_photo", "Photos du gala", EvidenceCategory.Photo, "Salle des archives",
                "Deux photos du gala de la Chambre de commerce prises par Sophie, l'assistante de Victor. 20 h 15 : Marc Dubois en smoking, écharpe blanche. " +
                "21 h 30 : sa chaise, à la table 4, est vide.",
                "21 h 30 - Marc Dubois n'est plus à sa table au gala", 1290, "marc");
            Ev(c, "ev_access_log", "Registre des entrées", EvidenceCategory.Document, "Salle de surveillance",
                "19 h 02 - Visiteuse : Claire Lemaire (entrée principale)\n19 h 25 - Sortie : Claire Lemaire\n" +
                "20 h 40 - Badge n° 117 (J. Roy, non désactivé) : porte des archives\n20 h 55 - Sortie : badge n° 117\n" +
                "21 h 10 - Code de maintenance : porte arrière\n21 h 48 - Code de maintenance : porte arrière (sortie)\n" +
                "Note : code de maintenance communiqué à M. Dubois le 13 octobre (travaux dans son bureau).",
                "21 h 10 - Quelqu'un entre par la porte arrière avec le code de maintenance", 1270, "marc", "claire", "julien");
            Ev(c, "ev_camera", "Image de vidéosurveillance", EvidenceCategory.Recording, "Salle de surveillance",
                "Caméra de la porte arrière, 21 h 09 : une silhouette en smoking, écharpe blanche, le visage caché par un parapluie. " +
                "La caméra s'éteint trois minutes plus tard.", null, -1, "marc");
            Ev(c, "ev_voicemail", "Enregistrement téléphonique", EvidenceCategory.Recording, "Salle de surveillance (enregistreur)",
                "Ligne du bureau, 21 h 31, appel déclenché par erreur : « … Marc ? Qu'est-ce que tu fais ici ? Tu devais être au gala… " +
                "Bon, assieds-toi. Je te sers un verre, il faut qu'on parle des virements. »",
                "21 h 31 - Victor reçoit Marc dans son bureau et lui sert un verre", 1291, "marc");
            Ev(c, "ev_guard", "Rapport du gardien", EvidenceCategory.Testimony, "Salle de surveillance",
                "« Ronde de 23 h : M. Lemaire affalé sur son bureau, un verre renversé. La porte arrière n'était pas verrouillée. " +
                "Les caméras étaient coupées depuis 21 h 12 : quelqu'un avait retiré un fusible de la boîte électrique. »",
                "21 h 12 - Un fusible est retiré : les caméras s'éteignent", 1272);
            Ev(c, "ev_hidden_video", "Vidéo de la caméra cachée", EvidenceCategory.Recording, "Pièce secrète",
                "Victor avait installé une caméra cachée, alimentée par batterie. 21 h 36 : pendant que Victor cherche un dossier, " +
                "Marc Dubois verse une poudre dans son verre de whisky. 21 h 44 : Victor s'effondre. Marc emporte des documents et s'en va.",
                "21 h 36 - Marc verse une poudre dans le verre de Victor", 1296, "marc");
        }

        // ------------------------------------------------------------------ Objets

        private static void Item(GeneratedContent c, string id, string name, ItemCategory cat, string description,
            string readable = null)
        {
            var i = CreateOrLoad<ItemData>($"{SoFolder}/Items/Item_{id}.asset");
            i.id = id; i.displayName = name; i.category = cat; i.description = description;
            i.readableText = readable; i.isQuestItem = true;
            EditorUtility.SetDirty(i);
            c.items[id] = i;
        }

        private static void CreateItems(GeneratedContent c)
        {
            Item(c, "key_office", "Clé du bureau", ItemCategory.Key,
                "Petite clé en laiton trouvée derrière le cadran de l'horloge. Étiquette : « Bureau V.L. »");
            Item(c, "password_note", "Note manuscrite de Victor", ItemCategory.Document,
                "Note trouvée dans le coffre-fort.",
                "« Mon mot de passe : le nom de mon fidèle compagnon, suivi de l'année où cette photo a été prise. - V. »");
            Item(c, "fuse", "Fusible de rechange 20 A", ItemCategory.Tool,
                "Un fusible neuf de 20 ampères, rangé avec les fournitures des archives.");
            Item(c, "key_surveillance", "Clé de la salle de surveillance", ItemCategory.Key,
                "Clé étiquetée « Surveillance - gardien ».");
        }

        // ------------------------------------------------------------------ Affaire

        private static void CreateCase(GeneratedContent c)
        {
            var data = CreateOrLoad<CaseData>($"{SoFolder}/Case_Lemaire.asset");
            data.caseTitle = "L'affaire Lemaire";
            data.caseSummary =
                "Le 14 octobre au soir, Victor Lemaire, fondateur de la société d'investissement Lemaire Capital, a été retrouvé mort dans son bureau. " +
                "La police a conclu à un arrêt cardiaque et classé l'affaire.\n\n" +
                "Un message anonyme affirme le contraire : Victor aurait été assassiné et les preuves seraient cachées dans le bâtiment. " +
                "La porte principale s'est verrouillée derrière vous. Pour sortir, il faudra découvrir la vérité.\n\n" +
                "Trois personnes gravitaient autour de Victor : son associé, sa nièce et un ancien employé.";
            data.suspects = new[] { c.suspects["marc"], c.suspects["claire"], c.suspects["julien"] };
            data.motives = new[]
            {
                "Dissimuler une fraude sur le point d'être dénoncée",
                "Hériter de la fortune de Victor",
                "Se venger d'un licenciement",
                "Régler une dispute d'argent familiale"
            };
            data.culprit = c.suspects["marc"];
            data.correctMotiveIndex = 0;
            data.keyEvidence = new[]
            {
                c.Ev("ev_agenda"), c.Ev("ev_family_photo"), c.Ev("ev_audit"), c.Ev("ev_email_lawyer"), c.Ev("ev_transfers"),
                c.Ev("ev_gala_photo"), c.Ev("ev_access_log"), c.Ev("ev_camera"), c.Ev("ev_voicemail"), c.Ev("ev_hidden_video")
            };
            data.minimumKeyEvidence = 4;
            data.truthSummary =
                "Marc Dubois détournait l'argent de Lemaire Capital vers sa société écran, M.D. Holdings. Victor l'avait découvert et comptait porter plainte.\n\n" +
                "Le 14 octobre, Marc s'éclipse du gala vers 21 h. Il entre par la porte arrière grâce au code de maintenance obtenu la veille, " +
                "puis retire un fusible de la boîte électrique à 21 h 12 pour aveugler les caméras. Victor le reçoit et lui sert un whisky. " +
                "Profitant d'un instant d'inattention, Marc verse de la digitaline dans le verre de Victor, dont le cœur fragile lâche quelques minutes plus tard. " +
                "Il emporte une partie des dossiers et retourne au gala.\n\n" +
                "Claire était seulement venue réclamer de l'argent plus tôt dans la soirée, et Julien était passé récupérer ses affaires : deux fausses pistes.\n\n" +
                "Le message anonyme venait de Sophie Tremblay, l'assistante de Victor, qui avait caché les preuves avant le classement de l'affaire. " +
                "Méfiant, Victor avait installé une caméra cachée reliée à sa pièce secrète.";
            data.goodEndingText =
                "Face au rapport d'audit, aux virements, au registre des entrées et à la vidéo de la caméra cachée, Marc Dubois passe aux aveux. " +
                "L'affaire Lemaire est rouverte : le meurtrier répondra de ses actes.";
            data.incompleteEndingText =
                "Vous avez désigné le bon suspect, mais votre dossier ne tient pas : mobile mal établi ou preuves insuffisantes. " +
                "L'avocat de Marc Dubois obtient l'abandon des poursuites. Le doute demeure…";
            data.badEndingText =
                "Votre accusation s'effondre : la personne désignée est innocente. Pendant que l'enquête s'égare, le véritable coupable " +
                "efface ses traces. L'affaire Lemaire est de nouveau classée.";
            EditorUtility.SetDirty(data);
            c.caseData = data;
        }
    }
}
