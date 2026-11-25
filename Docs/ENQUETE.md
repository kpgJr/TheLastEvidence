# L'affaire Lemaire — bible narrative et solutions

> Document réservé à l'équipe : il contient toutes les solutions.

## Résumé

Le 14 octobre, **Victor Lemaire** (61 ans), fondateur de la société d'investissement *Lemaire Capital*, est retrouvé mort dans son bureau à 23 h. La police conclut à un arrêt cardiaque. Un message anonyme affirme qu'il a été empoisonné et que les preuves sont cachées dans le bâtiment.

## La vérité

**Coupable : Marc Dubois**, associé de Victor. **Mobile : dissimuler une fraude** (1,2 M$ détournés vers sa société écran *M.D. Holdings*) que Victor allait dénoncer le lundi suivant.

1. 19 h 02 – 19 h 25 : Claire, la nièce, vient réclamer de l'argent. Ils se disputent, puis elle repart.
2. 20 h 40 – 20 h 55 : Julien Roy, l'ancien comptable, entre avec son badge resté actif pour récupérer ses affaires aux archives. Il ment ensuite sur sa présence, mais il est innocent.
3. 21 h 10 : Marc quitte le gala et entre par la porte arrière avec le code de maintenance obtenu la veille.
4. 21 h 12 : il retire un fusible de la boîte électrique de la salle de surveillance, ce qui coupe les caméras.
5. 21 h 31 : Victor le reçoit (« Marc ? Tu devais être au gala… »). Un appel est déclenché par erreur et enregistré.
6. 21 h 36 : Marc verse de la digitaline dans le whisky de Victor, ce que filme la caméra cachée.
7. 21 h 48 : Marc ressort par la porte arrière et retourne au gala.
8. 23 h 00 : le gardien découvre le corps.

L'auteur du message anonyme est Sophie Tremblay, l'assistante de Victor. Elle a caché les preuves avant le classement de l'affaire.

## Suspects

| Suspect | Rôle | Mobile apparent | Alibi déclaré | Vérité |
|---|---|---|---|---|
| Marc Dubois | Associé | Rupture de l'association | Au gala de 20 h à 23 h 30 | **Faux**. Absent de 21 h à 21 h 50 : c'est le coupable. |
| Claire Lemaire | Nièce, héritière | Héritage et dettes | Rentrée chez elle après 19 h 25 | Vrai. Fausse piste. |
| Julien Roy | Ancien comptable | Vengeance (lettre de menace) | « Jamais venu ce soir-là » | Faux, mais il était seulement venu chercher ses affaires. Fausse piste. |

## Matrice des preuves (19)

| ID | Preuve | Zone et emplacement | Condition de découverte | Rôle narratif |
|---|---|---|---|---|
| ev_message | Message anonyme | Hall, petite table | Libre (tutoriel) | Déclencheur ; indice de l'horloge |
| ev_police | Rapport de police | Hall, tableau d'affichage | Libre | Heure de la découverte (23 h) |
| ev_agenda | Agenda de Victor | Bureau, sur le bureau | Clé du bureau | **Clé**. RDV de 21 h avec « M. » |
| ev_glass | Verre de whisky | Bureau, sur le bureau | Examiner le fond du verre | Prouve l'empoisonnement |
| ev_family_photo | Photo des 20 ans | Bureau, bibliothèque | Retourner la photo | **Clé**. Tension autour des comptes |
| ev_claire_voicemail | Message vocal de Claire | Bureau, téléphone | Clé du bureau | Alibi de Claire (fausse piste) |
| ev_audit | Rapport d'audit | Bureau, coffre-fort | Énigme 2 (coffre) | **Clé**. La fraude de Marc |
| ev_email_lawyer | Courriel à l'avocat | Ordinateur | Énigme 3 (mot de passe) | **Clé**. Victor allait porter plainte |
| ev_email_julien | Courriel de Julien | Ordinateur | Énigme 3 | Explique la venue de Julien |
| ev_transfers | Relevés de virements | Archives, classeur 1 (haut) | Porte des archives | **Clé**. Signatures de Marc |
| ev_newspaper | Article de journal | Archives, mur sud | Porte des archives | Mobile de Julien (fausse piste) |
| ev_threat | Lettre de menace | Archives, table | Porte des archives | Fausse piste |
| ev_will | Testament | Archives, classeur 2 (haut) | Porte des archives | Mobile de Claire (fausse piste) |
| ev_gala_photo | Photos du gala | Archives, table | Retourner la photo | **Clé**. Casse l'alibi de Marc |
| ev_guard | Rapport du gardien | Surveillance, petite table | Clé de surveillance | Fusible retiré à 21 h 12 |
| ev_access_log | Registre des entrées | Surveillance, écran central | Énigme 4 (courant) | **Clé**. Code de maintenance de Marc |
| ev_camera | Image de vidéosurveillance | Surveillance, écran gauche | Énigme 4 | **Clé**. Silhouette en tenue de gala |
| ev_voicemail | Enregistrement téléphonique | Surveillance, enregistreur | Énigme 4 | **Clé**. « Marc ? Tu devais être au gala… » |
| ev_hidden_video | Vidéo de la caméra cachée | Pièce secrète | Énigme 5 (chronologie) | **Clé**. Preuve finale |

**Bonne fin** : Marc Dubois, plus le mobile « Dissimuler une fraude… », plus au moins 4 preuves **clés** associées à Marc sur le tableau d'enquête. Ces valeurs se règlent dans `ScriptableObjects/Case_Lemaire.asset`.
**Fin incomplète** : le bon suspect, mais un mauvais mobile ou moins de 4 preuves clés.
**Mauvaise fin** : un autre suspect.

## Solutions des énigmes

| # | Énigme | Solution | Indices dans le jeu |
|---|---|---|---|
| 1 | Clé du bureau | Ouvrir le cadran de l'horloge du hall (arrêtée à 23 h) | Message anonyme, rapport de police |
| 2 | Coffre-fort | **1403** | Note du tiroir (« le jour où tout a commencé, JJMM ») et plaque (« fondée le 14 mars 1998 ») |
| 3 | Ordinateur | **REX2009** | Note du coffre (« nom de mon fidèle compagnon + année de la photo ») et verso de la photo du chien (« Rex — été 2009 ») |
| 4 | Boîte à fusibles | Éclairage 10 A, Écrans 15 A, Enregistreur 5 A, Serveur 20 A | Schéma au mur. Le fusible de 20 A est dans le classeur 2 (bas) des archives. |
| 5 | Chronologie | Claire → Julien → porte arrière → fusible → Victor reçoit Marc → corps | Registre, rapport du gardien, enregistrement, journal (onglet Chronologie) |
| 6 | Accusation | Marc Dubois, fraude, au moins 4 preuves clés | Tableau d'enquête |

## Progression

Hall → (clé dans l'horloge) → Bureau → coffre (1403) puis ordinateur (REX2009), qui déverrouille les archives → Archives (fusible et clé de surveillance) → Surveillance (fusibles, puis chronologie) → le panneau secret s'ouvre → Pièce secrète (vidéo, tableau, accusation) → la porte principale se déverrouille → sortie → écran de fin.
