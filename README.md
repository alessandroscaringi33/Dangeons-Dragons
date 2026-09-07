# D&D Companion — README Tecnico

Assistente per il Dungeon Master di una campagna di Dungeons & Dragons. App
desktop/mobile in **C# / .NET MAUI**, local-first, offline-first.

Specifica tecnica di riferimento: [`DnD_Companion_Specifica_Tecnica.md`](./DnD_Companion_Specifica_Tecnica.md).

---

## Stato del progetto

| Fase (roadmap §24) | Stato |
|---------------------|-------|
| Phase 0 — Repository e architettura | **Completata** |
| Phase 1 — Database e Domain Model | **Completata** |
| Phase 2 — Gestione campagne | **Completata** |
| Phase 3 — Importazione PDF | **Completata** |
| Phase 4 — Storia: capitoli e scene | **Completata** |
| Phase 5 — Personaggi | **Completata** |
| Phase 6+ | Non avviate |

È stata costruita la **base architetturale**: solution, progetti, DI, logging,
base MVVM. Il **Domain Model** (`DndCompanion.Core/Domain`) contiene tutte le
entità, gli enum e le regole di calcolo deterministiche, senza dipendenze da
UI, database o filesystem. Il **layer di persistenza**
(`DndCompanion.Infrastructure/Data`) usa **SQLite + EF Core**: ogni campagna ha
il proprio database `campaign.db` nella propria cartella
(`Campagne/NomeCampagna/campaign.db`), con DbContext, configurazioni delle
entità, migrazioni versionate e inizializzazione automatica. La **gestione
campagne** (`DndCompanion.Infrastructure/Campaigns` + schermata `Campagne` in
MAUI) consente di elencare, creare e aprire le campagne, rilevare i PDF e
gestire la posizione configurabile della cartella `Campagne`. L'**importazione
PDF** (`IPdfReader` in Core, `PdfPigPdfReader` in Infrastructure, schermata
"Storia / Documento" in MAUI) estrae il testo e il numero di pagine senza
modificare il PDF originale e distingue chiaramente i documenti senza testo
estraibile (es. scansionati). La **gestione della storia** (Phase 4) consente al
DM di strutturare manualmente la campagna in **Capitolo → Scena**
(`IChapterService`/`ISceneService` in Core, `ChapterService`/`SceneService` in
Infrastructure, schermata **Storia** in MAUI, raggiungibile dalla schermata
"Storia / Documento"). Supporta CRUD di capitoli e scene, riordinamento
(1-based, riallineato automaticamente), spostamento di una scena anche tra
capitoli, completamento scena e **scena corrente** (evidenziata visivamente e
persistita su `Campaign.CurrentSceneId`). La **gestione personaggi** (Phase 5)
fornisce una Character Sheet digitale (`ICharacterService` in Core,
`CharacterService` in Infrastructure, schermate **Personaggi** e **Scheda
Personaggio** in MAUI, raggiungibili dalla schermata "Storia / Documento").
Gestisce nome, giocatore, razza, classe, sottoclasse, background, livello,
esperienza, caratteristiche, HP/HP massimi/HP temporanei, CA, iniziativa,
velocità, condizioni, note e inventario. I **valori derivati** (modificatori
delle caratteristiche, proficiency bonus per livello, iniziativa totale,
percezione passiva) sono **calcolati** in tempo reale e mai duplicati nel
database. La modifica degli HP è rapida (danno, cura, imposta) sia dall'elenco
sia dalla scheda.

---

## Stack

- C# / .NET 9
- .NET MAUI (Android, iOS, Mac Catalyst, Windows)
- MVVM con `CommunityToolkit.Mvvm`
- Dependency Injection (contenitore MAUI `MauiAppBuilder` / `IServiceCollection`)
- Nullable Reference Types attivi in tutti i progetti
- `async`/`await` come convenzione per tutte le operazioni I/O future
- Git

---

## Struttura della soluzione

```text
src/
  DndCompanion.sln
  DndCompanion/
    Views/                 # pagine XAML
    ViewModels/            # view model
    Resources/             # icone, font, stili
    Platforms/             # bootstrap per piattaforma
    MauiProgram.cs         # composition root (DI + logging)
    App.xaml / AppShell.xaml
  DndCompanion.Core/
    Mvvm/                  # base MVVM indipendente dalla UI
    Domain/                # entità, enum e regole di dominio (Phase 1)
    Campaigns/             # contratti servizio campagne (Phase 2)
    Documents/             # contratto lettura PDF (Phase 3)
    Story/                 # contratti servizio capitoli/scene (Phase 4)
    Characters/            # contratti servizio personaggi (Phase 5)
    (Abstractions, Services nelle fasi successive)
  DndCompanion.Infrastructure/
    Logging/               # logging strutturato su file (spec §20)
    Data/                  # SQLite/EF Core: DbContext, factory, inizializzatore,
                           # configurazioni entità e migrazioni (Phase 1)
    Campaigns/             # CampaignService: elenco, creazione, apertura campagne
                           # e rilevamento PDF (Phase 2)
    Documents/             # PdfPigPdfReader: estrazione testo/n. pagine (Phase 3)
    Story/                 # ChapterService/SceneService: CRUD, ordinamento,
                           # completamento e scena corrente (Phase 4)
    Characters/            # CharacterService: CRUD personaggi, HP, condizioni,
                           # inventario e calcoli derivati (Phase 5)
    (Pdf, Repository nelle fasi successive)
  DndCompanion.Tests/
    ViewModelBaseTests.cs
    FileLoggerTests.cs
    ArchitectureTests.cs
    ChapterServiceTests.cs
    SceneServiceTests.cs
    CharacterServiceTests.cs
```

### Responsabilità dei layer

- **DndCompanion** — Views, ViewModels, risorse UI, navigazione, servizi UI,
  composition root.
- **DndCompanion.Core** — entità di dominio, value object, enum, regole,
  interfacce, servizi applicativi indipendenti dalla UI. Nessuna dipendenza da
  MAUI o da Infrastructure.
- **DndCompanion.Infrastructure** — EF Core, SQLite, repository, filesystem,
  import PDF, implementazioni concrete dei servizi. Dipende solo da Core.
- **DndCompanion.Tests** — unit/integration test, test dei calcoli, test di
  persistenza.

Le frecce di dipendenza sono **unidirezionali** verso Core. La direzione è
verificata automaticamente da `ArchitectureTests`.

---

## Dependency Injection

Composition root in `src/DndCompanion/MauiProgram.cs`:

- logging (Debug in DEBUG, file strutturato JSON in `AppDataDirectory/logs`);
- `AppShell`, `MainViewModel`, `MainPage` registrati nel container.

I view model ricevono `ILogger<T>` tramite costruttore (constructor injection).

## Logging

`DndCompanion.Infrastructure.Logging.FileLoggerProvider` scrive log strutturati
in formato JSON-lines con i livelli standard
`Debug/Information/Warning/Error/Critical` (spec §20). La soglia minima è
configurabile (default `Information`). I log risiedono in
`AppDataDirectory/logs/dnd-companion.log`.

## Base MVVM

`DndCompanion.Core.Mvvm.ViewModelBase` estende `ObservableObject` e fornisce
il supporto a `INotifyPropertyChanged` a tutti i view model. Le pagine usano
binding compilati (`x:DataType`) e view model iniettati via DI.

---

## Comandi

```bash
# Ripristino pacchetti
dotnet restore src/DndCompanion.sln

# Build completa (tutte le piattaforme + test)
dotnet build src/DndCompanion.sln

# Build della sola app Windows (più rapida)
dotnet build src/DndCompanion/DndCompanion.csproj -f net9.0-windows10.0.19041.0

# Esecuzione dei test
dotnet test src/DndCompanion.sln
```

Prerequisiti: .NET SDK 9 + workload MAUI
(`dotnet workload install maui`).

---

## Note sul repository

La cartella `Campagne/` è la posizione predefinita delle campagne gestite
dall'app (spec §4); la posizione sarà resa configurabile nelle fasi
successive.