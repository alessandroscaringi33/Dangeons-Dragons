# D&D Companion — Specifica Tecnica

## 1. Obiettivo

Creare un'applicazione desktop/mobile in C#/.NET MAUI pensata per assistere un Dungeon Master umano durante una campagna di Dungeons & Dragons.

**OpenCode non fa parte del software finale e non deve essere considerato il Dungeon Master.** OpenCode è esclusivamente lo strumento usato per sviluppare il progetto.

L'app deve consentire al DM di:
- importare e consultare una storia presente nella cartella `Campagne`;
- organizzare campagna, capitoli, scene, luoghi, NPC, personaggi, quest ed eventi;
- gestire le sessioni di gioco;
- gestire tiri di dado, anche quando il dado viene tirato fisicamente al tavolo;
- gestire combattimenti, iniziativa, HP, CA, attacchi e danni;
- registrare automaticamente uno storico della sessione;
- prendere note rapide;
- salvare tutto localmente;
- fare backup e ripristino della campagna;
- funzionare bene anche offline.

L'AI è **fuori dallo scope dell'MVP**. Potrà essere aggiunta in seguito come funzionalità opzionale per il DM, senza prendere il controllo della storia.

---

## 2. Principi fondamentali

1. **Local-first**: i dati della campagna devono essere disponibili localmente.
2. **Nessuna dipendenza obbligatoria dal cloud**.
3. **Il DM mantiene sempre il controllo**.
4. **Il PDF è una fonte di importazione/consultazione, non il database principale**.
5. **Ogni modifica importante deve essere persistita in modo affidabile**.
6. **L'app deve essere utilizzabile durante una partita con pochi click**.
7. **La schermata Sessione è il centro operativo dell'app**.
8. **Calcoli e regole devono essere deterministici e testabili**.
9. **Non introdurre funzionalità non richieste senza prima documentarle**.
10. **Non procedere allo step successivo se lo step corrente non è compilabile, testato e visivamente verificato**.

---

## 3. Stack tecnologico

### Obbligatorio

- C#
- .NET MAUI
- MVVM
- Dependency Injection
- SQLite
- Entity Framework Core per SQLite
- Async/await
- Nullable reference types
- XAML per la UI
- Git

### Architettura

Preferire una struttura separata:

```text
src/
  DndCompanion/
  DndCompanion.Core/
  DndCompanion.Infrastructure/
  DndCompanion.Tests/
```

### Responsabilità

`DndCompanion`
- Views
- ViewModels
- risorse UI
- navigazione
- servizi UI

`DndCompanion.Core`
- entità di dominio
- value objects
- enum
- regole
- interfacce
- servizi applicativi indipendenti dalla UI

`DndCompanion.Infrastructure`
- EF Core
- SQLite
- repository
- filesystem
- import PDF
- implementazioni concrete dei servizi

`DndCompanion.Tests`
- unit test
- integration test
- test dei calcoli
- test di persistenza

---

# 4. Struttura delle cartelle delle campagne

L'app deve cercare le campagne nella cartella:

```text
Campagne/
```

Ogni sottocartella rappresenta una campagna:

```text
Campagne/
├── Campagna1/
│   ├── storia.pdf
│   ├── campaign.db
│   ├── Backup/
│   └── Assets/
│
└── Campagna2/
    ├── avventura.pdf
    ├── campaign.db
    ├── Backup/
    └── Assets/
```

Il nome della cartella non deve essere considerato necessariamente il nome ufficiale della campagna.

L'app deve:
- rilevare le cartelle;
- rilevare i PDF;
- permettere di creare una nuova campagna;
- permettere di aprire una campagna esistente;
- non cancellare o modificare il PDF originale senza esplicita richiesta dell'utente.

La posizione fisica della cartella `Campagne` deve essere configurabile e visualizzata nelle impostazioni.

---

# 5. Modello di dominio

Entità principali:

```text
Campaign
Chapter
Scene
Character
NPC
Location
Quest
Session
SessionEvent
DiceRoll
Combat
Combatant
InventoryItem
Note
```

## Campaign

Campi indicativi:

- Id
- Name
- Description
- SourcePdfPath
- CreatedAt
- UpdatedAt
- CurrentSceneId
- ActiveSessionId

## Chapter

- Id
- CampaignId
- Title
- Description
- Order
- Notes

## Scene

- Id
- ChapterId
- Title
- Description
- Order
- IsCompleted
- Notes

## Character

- Id
- CampaignId
- Name
- PlayerName
- Class
- Subclass
- Race
- Background
- Level
- Experience
- Strength
- Dexterity
- Constitution
- Intelligence
- Wisdom
- Charisma
- CurrentHp
- MaxHp
- TemporaryHp
- ArmorClass
- InitiativeModifier
- Speed
- ProficiencyBonus
- PassivePerception
- Notes

## NPC

- Id
- CampaignId
- Name
- Role
- Description
- CurrentHp
- MaxHp
- ArmorClass
- InitiativeModifier
- Notes
- IsAlive
- IsKnown

## Location

- Id
- CampaignId
- Name
- Description
- Notes

## Quest

- Id
- CampaignId
- Title
- Description
- Status
- Notes

Status minimo:

```text
NotStarted
Active
Completed
Failed
Abandoned
```

## Session

- Id
- CampaignId
- Number
- Title
- StartedAt
- EndedAt
- Summary
- Notes
- IsActive

## SessionEvent

- Id
- SessionId
- Timestamp
- Type
- Description
- RelatedCharacterId
- RelatedNpcId
- RelatedSceneId
- RelatedDiceRollId
- RelatedCombatId

## DiceRoll

- Id
- SessionId
- Timestamp
- DiceNotation
- DiceType
- DiceCount
- Modifier
- Results
- Total
- RollMode
- Purpose
- CharacterId
- IsPhysicalRoll

`IsPhysicalRoll = true` significa che il giocatore ha tirato realmente il dado e il valore è stato inserito nel programma.

## Combat

- Id
- SessionId
- StartedAt
- EndedAt
- CurrentRound
- CurrentTurnIndex
- IsActive

## Combatant

- Id
- CombatId
- CharacterId nullable
- NPCId nullable
- Name
- Initiative
- CurrentHp
- MaxHp
- ArmorClass
- IsActive
- TurnOrder

## InventoryItem

- Id
- CharacterId
- Name
- Description
- Quantity
- Weight
- Notes

## Note

- Id
- CampaignId
- SessionId nullable
- Title
- Content
- CreatedAt
- UpdatedAt
- IsPinned

---

# 6. Database

Usare SQLite tramite Entity Framework Core.

Requisiti:

- migrazioni versionate;
- database creato automaticamente quando necessario;
- schema versioning;
- transazioni per operazioni critiche;
- foreign key;
- indici sui campi usati frequentemente;
- gestione corretta degli errori;
- nessuna perdita silenziosa di dati.

Ogni modifica significativa deve essere salvata in modo esplicito o tramite autosave affidabile.

---

# 7. Importazione PDF

Il PDF presente nella cartella `Campagne/<NomeCampagna>/` deve poter essere importato.

Funzioni:

1. rilevamento PDF;
2. estrazione testo;
3. visualizzazione del testo estratto;
4. associazione del PDF alla campagna;
5. possibilità di consultare il documento;
6. possibilità futura di trasformare manualmente porzioni del documento in Chapter/Scene/NPC/etc.

L'MVP non deve dipendere da un LLM per l'importazione.

Se il PDF non contiene testo estraibile, mostrare un messaggio chiaro e prevedere l'estensione futura con OCR.

---

# 8. Gestione della storia

La storia deve poter essere rappresentata come:

```text
Campagna
  └── Capitolo
       └── Scena
            ├── Descrizione
            ├── NPC
            ├── Luoghi
            ├── Quest
            └── Note
```

Il DM deve poter:
- creare;
- modificare;
- eliminare;
- riordinare;
- completare;
- cercare.

La scena corrente deve essere facilmente identificabile.

---

# 9. Gestione personaggi

Character Sheet digitale.

Il sistema deve calcolare automaticamente quando applicabile:

- modificatori delle caratteristiche;
- proficiency bonus in base al livello;
- iniziativa;
- passive perception;
- altri valori derivati definiti dal modello.

Non duplicare dati derivati se possono essere calcolati in modo affidabile.

La UI deve permettere modifiche rapide a:

- HP;
- condizioni;
- inventario;
- note.

---

# 10. Dice Engine

Creare un motore indipendente dalla UI.

Deve supportare almeno:

```text
d4
d6
d8
d10
d12
d20
d100
```

e combinazioni:

```text
1d20
1d20 + 5
2d6 + 3
4d8
```

Supportare:

- tiro normale;
- vantaggio;
- svantaggio;
- modificatore;
- più dadi;
- inserimento manuale del risultato;
- storico.

Esempio:

```text
Tiro fisico:
D20
Valore inserito: 17
Modificatore: +4
Totale: 21
```

Il sistema non deve falsificare il tiro fisico.

---

# 11. Skill Check

Il sistema deve poter registrare:

```text
Abilità
CD
Tiro
Modificatore
Totale
Esito
```

Esempio:

```text
Percezione
CD 15
Tiro 13
Bonus +4
Totale 17
SUCCESSO
```

La CD deve essere modificabile.

---

# 12. Combattimento

Funzionalità MVP:

- nuovo combattimento;
- aggiunta personaggi;
- aggiunta NPC;
- inserimento iniziativa;
- ordinamento;
- round;
- turno corrente;
- HP;
- danni;
- guarigione;
- CA;
- eliminazione/morte;
- fine combattimento.

La schermata deve rendere immediatamente evidente chi sta agendo.

---

# 13. Sessione

La Sessione è la schermata principale durante il gioco.

Deve mostrare rapidamente:

- scena corrente;
- personaggi;
- HP;
- NPC importanti;
- quest attive;
- ultimo tiro;
- ultimo evento;
- combattimento attivo;
- note rapide.

Azioni principali sempre raggiungibili:

```text
Tiro
Combattimento
Personaggio
NPC
Nota
Storia
```

---

# 14. Timeline

Ogni evento importante deve poter essere registrato.

Esempi:

```text
20:14 — Il gruppo entra nella taverna.
20:19 — Arkon effettua un tiro di Percezione.
20:19 — Risultato 18: successo.
20:27 — Inizia combattimento.
20:44 — Il Goblin viene sconfitto.
```

La timeline deve essere consultabile e filtrabile.

---

# 15. Ricerca

Implementare ricerca locale per:

- personaggi;
- NPC;
- luoghi;
- quest;
- scene;
- note;
- eventi.

La ricerca deve essere veloce e tollerante rispetto a maiuscole/minuscole.

---

# 16. Backup

Ogni campagna deve poter essere esportata in un archivio.

Struttura indicativa:

```text
CampaignBackup/
├── campaign.db
├── manifest.json
├── storia.pdf (opzionale)
└── Assets/
```

Funzioni:

- crea backup;
- ripristina backup;
- verifica integrità;
- mostra data ultimo backup;
- evitare sovrascritture accidentali.

---

# 17. UX/UI

Principi:

- dark mode come esperienza principale da tavolo;
- contrasto elevato;
- pulsanti grandi;
- informazioni importanti sempre visibili;
- evitare schermate sovraccariche;
- navigazione coerente;
- feedback immediato;
- dialog di conferma solo quando realmente necessario;
- animazioni leggere;
- nessun elemento puramente decorativo che rallenti l'utilizzo.

La UI deve essere pensata prima di tutto per l'utilizzo durante una sessione.

---

# 18. Accessibilità

Supportare:

- dimensione testo ragionevole;
- contrasto;
- semantic labels;
- focus/navigation;
- elementi touch sufficientemente grandi;
- non affidarsi esclusivamente al colore;
- messaggi di errore comprensibili.

---

# 19. Error handling

Mai mostrare stack trace all'utente finale.

Ogni errore deve:

1. essere registrato nei log;
2. mostrare un messaggio comprensibile;
3. lasciare l'app in uno stato consistente;
4. evitare perdita dati.

---

# 20. Logging

Implementare logging strutturato.

Livelli:

- Debug
- Information
- Warning
- Error
- Critical

Non registrare dati inutilmente sensibili.

---

# 21. Test

Ogni componente importante deve avere test.

Minimo:

### Domain

- modificatori;
- proficiency;
- dadi;
- vantaggio/svantaggio;
- skill check;
- HP;
- iniziativa;
- ordine combattimento.

### Persistence

- creazione DB;
- CRUD;
- relazioni;
- salvataggio;
- caricamento.

### Application

- creazione campagna;
- apertura campagna;
- creazione sessione;
- registrazione evento;
- registrazione tiro;
- combattimento.

---

# 22. Definition of Done

Uno step è considerato completato solo quando:

- il progetto compila senza errori;
- non ci sono warning nuovi non giustificati;
- i test automatici passano;
- la funzionalità è realmente utilizzabile;
- non sono presenti placeholder;
- non ci sono eccezioni non gestite;
- la UI è coerente con il design;
- la persistenza è verificata;
- regressioni precedenti sono escluse;
- il codice è pulito e coerente con l'architettura;
- il risultato visivo è stato verificato.

**OpenCode non deve procedere allo step successivo se uno di questi punti fallisce.**

---

# 23. Regola fondamentale per OpenCode

Prima di modificare il codice:

1. analizzare il repository;
2. leggere l'architettura esistente;
3. verificare gli step precedenti;
4. identificare eventuali problemi;
5. proporre il piano di implementazione;
6. implementare;
7. compilare;
8. eseguire i test;
9. verificare la UI;
10. correggere tutti i problemi;
11. ripetere build/test/verifica;
12. solo quando tutto è OK considerare completato lo step.

Non riscrivere parti funzionanti senza motivo.

Non introdurre dipendenze non necessarie.

Non inventare API o classi già esistenti.

Non dichiarare "completato" un task solo perché il codice è stato scritto.

---

# 24. Roadmap

## Phase 0
Repository e architettura.

## Phase 1
Database e Domain Model.

## Phase 2
Gestione campagne.

## Phase 3
Importazione PDF.

## Phase 4
Storia: capitoli e scene.

## Phase 5
Personaggi.

## Phase 6
NPC, luoghi e quest.

## Phase 7
Dice Engine.

## Phase 8
Skill Check e calcoli.

## Phase 9
Sessione.

## Phase 10
Combattimento.

## Phase 11
Timeline e note.

## Phase 12
Ricerca.

## Phase 13
Backup/Restore.

## Phase 14
UX/UI polish.

## Phase 15
Accessibilità.

## Phase 16
Performance.

## Phase 17
Testing completo e hardening.

## Phase 18
Release candidate.

---

# 25. AI futura — fuori MVP

L'architettura deve lasciare spazio a un futuro:

```text
IAssistantService
```

ma nessuna implementazione AI è necessaria per completare l'MVP.

Eventuali future funzioni:

- riassunto sessione;
- ricerca nella storia;
- generazione NPC;
- generazione incontri;
- generazione loot;
- aiuto al DM.

L'AI non deve modificare autonomamente la campagna.

---

# 26. Obiettivo finale

Il prodotto deve risultare come una vera **DM Companion App**, non come un semplice CRUD.

Durante una partita il DM deve poter:

1. aprire la campagna;
2. vedere la scena corrente;
3. gestire personaggi e NPC;
4. effettuare/inserire tiri;
5. gestire combattimenti;
6. annotare ciò che succede;
7. avanzare la storia;
8. ritrovare facilmente qualsiasi informazione.

Il software deve ridurre il lavoro amministrativo del DM senza sostituirlo.
