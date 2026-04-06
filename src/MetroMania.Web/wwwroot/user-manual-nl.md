# Metro Mania — Spelershandleiding

## Welkom bij de Uitdaging

**Metro Mania** is een programmeeruitdaging waarbij je een C# bot schrijft om een groeiend metronetwerk te beheren. Je bot luistert naar spelgebeurtenissen, plant routes en geeft elk uur één opdracht om reizigers door de stad te laten stromen. Het spel eindigt wanneer te veel reizigers zich op één station ophopen — houd je netwerk dus in beweging!

## De Spelwereld

De simulatie speelt zich af op een tegelraster. Naarmate het spel vordert, verschijnen nieuwe stations op verschillende locaties op de kaart. Watertegels kunnen delen van het raster vullen, maar blokkeren de metrolijnen niet — lijnen lopen conceptueel boven het terrein.

### Stationstypes

Elk station heeft een **vorm**. Reizigers spawnen bij stations en willen *elk* station bereiken dat overeenkomt met hun gewenste vorm. Er zijn zes types:

| Vorm | Naam |
|------|------|
| ● | Cirkel |
| ■ | Rechthoek |
| ▲ | Driehoek |
| ◆ | Ruit |
| ⬠ | Vijfhoek |
| ✦ | Ster |

## Reizigers

Reizigers verschijnen na verloop van tijd bij stations volgens het spawnschema van elk station. Het doel van een reiziger is om **elk** station van het juiste type te bereiken — niet één specifiek station.

Reizigers zijn slim: ze stappen alleen in een trein als de lijn van die trein daadwerkelijk naar een station van hun bestemmingstype leidt. Een doodlopende lijn die niet-verwante stations verbindt, zal geen reizigers aantrekken.

> **Waarschuwing:** Wanneer een station **10 of meer** wachtende reizigers heeft, wordt je `OnStationOverrun`-callback aangeroepen. Handel snel!

> **Game Over:** Wanneer een station **20 of meer** wachtende reizigers bereikt, eindigt het spel onmiddellijk.

## Middelen

Je begint elke run met een beperkte voorraad middelen:

- **1 Lijn** — definieert een metroroute tussen stations
- **1 Trein** — een voertuig om op een lijn te plaatsen

Elke **maandag om middernacht** (begin van elke spelweek) ontvangt je bot een **wekelijks cadeau**: één extra middel willekeurig gekozen — ofwel een **Lijn** of een **Trein**.

| Middel | Doel |
|--------|------|
| Lijn | Definieer een route die twee of meer stations verbindt |
| Trein | Een voertuig dat heen en weer rijdt over een lijn |

Elke trein vervoert standaard **6 reizigers** (instelbaar per level).

## Tijd

De simulatie loopt **uur voor uur** (24 uur = 1 dag). Elke aanroep van `OnHourTick` staat voor één speluur. Een level duurt doorgaans maximaal 200 dagen of totdat een game-over-gebeurtenis optreedt.

## Jouw Bot — De IMetroManiaRunner Interface

Je bot is een C#-klasse die `IMetroManiaRunner` implementeert. De spelmotor roept deze methoden aan naarmate er gebeurtenissen plaatsvinden:

### Gebeurteniscallbacks

| Methode | Wanneer aangeroepen | Geeft terug |
|---------|---------------------|-------------|
| `OnHourTick(snapshot)` | Elk uur | `PlayerAction` |
| `OnDayStart(snapshot)` | Om middernacht elke dag | — |
| `OnWeeklyGift(snapshot, gift)` | Elke maandag om middernacht | — |
| `OnStationSpawned(snapshot, id, location, type)` | Nieuw station verschijnt | — |
| `OnPassengerWaiting(snapshot, location, passengers)` | Reiziger begint te wachten | — |
| `OnStationOverrun(snapshot, location, passengers)` | 10+ reizigers op een station | — |
| `OnGameOver(snapshot, location, passengers)` | 20+ reizigers — spel eindigt | — |

> **`OnHourTick` is jouw hoofdcontrolecyclus.** Het is de enige callback die een actie teruggeeft. Alle andere callbacks zijn informatief — gebruik ze om je toestand bij te houden en vooruit te plannen.

### De GameSnapshot

Elke callback ontvangt een `GameSnapshot` met de volledige huidige toestand:

- `snapshot.Stations` — alle gespawnde stations (id, locatie, type, aantal reizigers)
- `snapshot.Lines` — alle actieve lijnen (id, geordende lijst van station-ID's)
- `snapshot.Vehicles` — alle voertuigen (id, lijn, reizigers aan boord, positie)
- `snapshot.Resources.AvailableLines` — lijnmiddelen die nog niet zijn geplaatst
- `snapshot.Resources.AvailableVehicles` — treinen die nog niet zijn geplaatst
- `snapshot.GameTime` — huidige `Dag` en `Uur`

---

## Spelersacties

Geef vanuit `OnHourTick` precies **één actie** terug per tick. Je kunt slechts één bewerking per uur uitvoeren — kies verstandig.

### `CreateLine` — Maak een Nieuwe Route

```csharp
return new CreateLine(
    LineId: Guid.NewGuid(),
    StationIds: [stationA, stationB]);
```

Maakt een nieuwe metrolijn die twee of meer stations verbindt. Verbruikt één beschikbaar **Lijn**-middel.

### `ExtendLine` — Voeg een Station Toe

```csharp
return new ExtendLine(
    LineId: bestaandeLijnId,
    FromStationId: huidigEindstation,
    ToStationId: nieuwStation);
```

Voegt een station toe aan de voor- of achterkant van een bestaande lijn. `FromStationId` moet het eerste of laatste station zijn.

### `InsertStationInLine` — Voeg een Station In het Midden In

```csharp
return new InsertStationInLine(
    LineId: bestaandeLijnId,
    NewStationId: inTeVoegenStation,
    FromStationId: aangrenzendStationA,
    ToStationId: aangrenzendStationB);
```

Voegt een nieuw station in tussen twee reeds aangrenzende stations op de lijn.

### `RemoveLine` — Verwijder een Route

```csharp
return new RemoveLine(LineId: lijnId);
```

Verwijdert een volledige lijn en geeft alle middelen (lijntoken + alle voertuigen) terug aan je pool.

### `AddVehicleToLine` — Zet een Trein In

```csharp
return new AddVehicleToLine(
    VehicleId: beschikbaarVoertuigId,
    LineId: doellijnId,
    StationId: beginstation);
```

Plaatst een beschikbare trein op een lijn bij het opgegeven beginstation. De trein begint direct te pendelen.

### `RemoveVehicle` — Haal een Trein Terug

```csharp
return new RemoveVehicle(VehicleId: voertuigId);
```

Verwijdert een voertuig van zijn lijn en geeft het terug aan je beschikbare pool.

### `NoAction`— Sla Deze Tick Over

```csharp
return PlayerAction.None;  // of: return new NoAction();
```

---

## Starterscode

Je inzending begint vanuit dit sjabloon:

```csharp
public class MyMetroManiaRunner : IMetroManiaRunner
{
    public PlayerAction OnHourTick(GameSnapshot snapshot) => PlayerAction.None;

    public void OnDayStart(GameSnapshot snapshot) { }

    public void OnWeeklyGift(GameSnapshot snapshot, ResourceType gift) { }

    public void OnStationSpawned(GameSnapshot snapshot, Guid stationId,
        Location location, StationType stationType) { }

    public void OnPassengerWaiting(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnStationOverrun(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }

    public void OnGameOver(GameSnapshot snapshot, Location location,
        IReadOnlyList<Passenger> passengers) { }
}
```

---

## Score

Je score stijgt elke keer dat een reiziger succesvol wordt afgeleverd bij een station van zijn bestemmingstype. Elk level duurt een vast aantal dagen. Hoe **langer je overleeft** en hoe **meer reizigers je aflevert**, hoe hoger je score. Je **beste score per level** telt mee voor de ranglijst.

---

## Strategietips

- **Sla station-ID's op in `OnStationSpawned`** — bewaar ze in klassevelden zodat je in de volgende `OnHourTick` actie kunt ondernemen.
- **Reageer meteen op `OnStationOverrun`** — bij 10 reizigers heb je nog ongeveer 10 uur voor game over.
- **Verbind elk station** — een niet-verbonden station stapelt reizigers op zonder ontsnappingsroute. Prioriteer altijd nieuwe stations.
- **Denk na over lijntopologie** — hub-and-spoke- of lus-netwerken presteren meestal beter dan één lange keten.
- **Gebruik wekelijkse cadeaus meteen** — zet nieuwe middelen zo snel mogelijk in.
- **Reizigersroutering is slim** — reizigers stappen alleen in als de lijn naar hun bestemmingstype leidt. Willekeurig stations verbinden helpt niet.
- **Verwijder en herbouw** — een slecht geplande lijn slopen en opnieuw bouwen is soms de beste zet, ondanks de tijdelijke verstoring.
