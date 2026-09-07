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
| Phase 4+ | Non avviate |

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
estraibile (es. scansionati). Non sono ancora implementati capitoli/scene
(Phase 4).

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
    (Abstractions, Services nelle fasi successive)
  DndCompanion.Infrastructure/
    Logging/               # logging strutturato su file (spec §20)
    Data/                  # SQLite/EF Core: DbContext, factory, inizializzatore,
                           # configurazioni entità e migrazioni (Phase 1)
    Campaigns/             # CampaignService: elenco, creazione, apertura campagne
                           # e rilevamento PDF (Phase 2)
    Documents/             # PdfPigPdfReader: estrazione testo/n. pagine (Phase 3)
    (Pdf, Repository nelle fasi successive)
  DndCompanion.Tests/
    ViewModelBaseTests.cs
    FileLoggerTests.cs
    ArchitectureTests.cs
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