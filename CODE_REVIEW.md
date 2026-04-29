# Code Review – větev `feature/csharp-backend`

Tento dokument je code review C# backendu, který nahrazuje původní Node.js
server (`server.js`) ekvivalentní ASP.NET Core / SignalR aplikací. Zaměřuje se
na architekturu, design, kvalitu kódu, bezpečnost a testy. Každá poznámka
obsahuje navrženou úpravu.

Použité značení priority:

- 🔴 **High** – chyba v chování, bezpečnosti nebo závažný designový problém,
  doporučuji opravit ještě před mergem.
- 🟡 **Medium** – designový dluh nebo nekonzistence; vhodné řešit v rámci této
  iterace nebo bezprostředně po ní.
- 🟢 **Low** – kosmetické / nice-to-have úpravy.

---

## 1. Celková architektura

Řešení sleduje "Clean Architecture" rozvržení obvyklé pro Aspire šablonu:

```
ScrumPoker.AppHost          – Aspire orchestrace
ScrumPoker.ServiceDefaults  – sdílená konfigurace (OTel, health, resilience)
ScrumPoker.Web              – ASP.NET Core host + SignalR hub + statický front-end
ScrumPoker.Application      – aplikační služby (RoomService, RoomStore) + DTO
ScrumPoker.Domain           – Room, Participant
ScrumPoker.UnitTests        – xUnit + AwesomeAssertions
ScrumPoker.IntegrationTests – WebApplicationFactory + SignalR client
```

Závislosti směřují správně dovnitř (Web → Application → Domain). `Domain` nemá
žádné odkazy mimo BCL. Centralizace přes `Directory.Build.props` /
`Directory.Packages.props` je čistá.

### 1.1 🟡 AppHost je momentálně téměř no-op

`src/ScrumPoker.AppHost/AppHost.cs` pouze přidává jeden projekt:

```csharp
builder.AddProject<Projects.ScrumPoker_Web>("scrum-poker-web");
```

Pro jednu službu Aspire orchestrace nepřináší velkou hodnotu (v podstatě
nahrazuje `dotnet run --project Web`). Doporučuji jednu z těchto možností:

1. Pokud se plánuje přidat další zdroj (Redis pro back-plane SignalR, OTel
   collector, Postgres pro persistenci atd.), nechat AppHost a explicitně do
   něj tyto zdroje přidat.
2. Pokud zůstane jediná služba, AppHost a ServiceDefaults zachovat (nízké
   náklady) a do AppHost minimálně přidat:

   ```csharp
   var web = builder.AddProject<Projects.ScrumPoker_Web>("scrum-poker-web")
                    .WithExternalHttpEndpoints();
   ```

   aby Aspire dashboard exponoval URL ven z kontejnerové sítě.

### 1.2 🟡 "Rozcestník" doménového modelu – Domain je anemický

`Room` a `Participant` jsou v podstatě DTO s veřejně mutovatelnými stavy a
veškerá business logika (validace, autorizace, projekce) žije v
`RoomService`. Ten je tak typický **transaction script** nad anemickým
modelem. Pro tento rozsah aplikace je to *přijatelné*, ale několik konkrétních
důsledků způsobuje skutečné chyby v kódu (viz §3 a §4) – encapsulace by je
preventivně eliminovala.

Návrh: minimálně přesunout invarianty na `Room` jako metody, např.
`Room.AddOrUpdate(participant)`, `Room.RecordVote(connId, vote)`,
`Room.EndVoting()`, `Room.RemoveByName(...)`. `RoomService` by pak řešil jen
orchestraci a projekci do DTO. Tím odpadne i veřejně publikovaný `SyncRoot`
(viz §2.3).

### 1.3 🟢 Hranice projektů – kontrakty hubu

Kontrakty hubu (názvy event metod jako `roomUpdate`, `joinConfirmation`,
`forcedDisconnect`, …) jsou na Web vrstvě roztroušené jako "magické" stringy a
anonymní objekty. Doporučuji zavést buď:

- **strongly-typed hub** `ScrumPokerHub : Hub<IScrumPokerClient>`, kde
  `IScrumPokerClient` deklaruje `Task RoomUpdate(RoomStateDto state)`,
  `Task JoinConfirmation(JoinConfirmationDto dto)` apod., nebo
- aspoň zavést konstanty pro názvy událostí v jedné třídě
  (`HubEvents.RoomUpdate = "roomUpdate"`).

To výrazně zlepší refaktorovatelnost a integrační testy.

---

## 2. Doménová vrstva (`ScrumPoker.Domain`)

### 2.1 🟡 `Participant` má veřejně mutovatelné non-nullable referenční typy

```csharp
public class Participant
{
    public string ConnectionId { get; set; }
    public string Name { get; set; }
    public string? Vote { get; set; }
    public bool Active { get; set; }
    public bool IsObserver { get; set; }
    public DateTime LastHeartbeat { get; set; }

    public Participant(string connectionId, string name, bool isObserver) { ... }
}
```

Vzhledem k `<Nullable>enable</Nullable>` a `<WarningsAsErrors>Nullable</…>`
v `Directory.Build.props` toto kompiluje pravděpodobně jen proto, že hodnoty
nastavuje konstruktor; ale `set;` mimo konstruktor umožňuje `participant.Name
= null!`. Návrh:

```csharp
public sealed class Participant
{
    public string ConnectionId { get; }
    public string Name { get; set; }
    public string? Vote { get; set; }
    public bool IsObserver { get; set; }
    public DateTime LastHeartbeat { get; set; }

    public Participant(string connectionId, string name, bool isObserver)
    {
        ConnectionId = connectionId;
        Name = name;
        IsObserver = isObserver;
        LastHeartbeat = DateTime.UtcNow;
    }
}
```

– `ConnectionId` je identita, neměla by se měnit (`get;` only).
– `sealed` (žádná dědičnost není potřeba).
– `Active` viz §3.1 (navrhuji odstranit úplně).

### 2.2 🟢 `Room.SyncRoot` jako `object`

Na .NET 9+ existuje `System.Threading.Lock` s lepším výkonem a dedikovaným
`scope`. Pokud zůstane lock v doméně, lze:

```csharp
public Lock SyncRoot { get; } = new();
```

a pak `lock (room.SyncRoot)` funguje s monitorovou sémantikou na novějším
typu. Drobné zlepšení.

### 2.3 🟡 `Room.SyncRoot` je veřejný

Veřejně publikovaný lock objekt umožňuje libovolnému volajícímu zamknout celý
room z venku, což je antipattern (riziko deadlocků a porušení uzavřenosti
synchronizace). Lepší řešení:

- udělat `SyncRoot` `internal` a `RoomService` umístit do stejné assembly
  (Domain), nebo
- **lépe** – zapouzdřit zamykání dovnitř `Room` (např. `Room.Modify(action)`
  delegate / lambda) a vůbec ho nepublikovat.

### 2.4 🟡 `Room.Participants` – publikovaný mutovatelný `List<>`

`public List<Participant> Participants { get; } = new();` umožňuje libovolnému
volajícímu měnit vnitřní stav. V kombinaci s veřejným zámkem je to dvojí únik
implementace. Po případné refaktorizaci podle §1.2 by se zveřejňovala pouze
`IReadOnlyList<Participant>` (nebo lépe `IReadOnlyCollection<ParticipantDto>`).

---

## 3. Aplikační vrstva (`ScrumPoker.Application`)

### 3.1 🔴 Pole `Active` je dead-code – odstranit

V `RoomService.Disconnect`:

```csharp
participant.Active = false;
room.Participants.RemoveAll(p => !p.Active);
```

Na konci metody je tedy **každý** participant ve store buď `Active = true`,
nebo už neexistuje. Současně:

- `JoinRoom` při znovupřipojení nastaví `existing.Active = true`,
- `Vote` / `CancelVote` filtruje `p.Active`,
- `ProjectRoom` filtruje `p.Active`,
- `ResolveName` filtruje `p.Active`.

Nikdo ale nikdy nemůže pozorovat `Active = false`, protože stav je vždy
zveřejňován pod stejným zámkem, který tu příznak okamžitě vykoupil. Důsledek:

1. Pole `Active` je sémanticky mrtvé – zvyšuje cognitive load a vede
   k zbytečným filtrům (a tím i drobným bugům, např. `RemoveUser` dnes
   filtruje `p.Active` *vůbec ne* a opírá se právě o to, že nikdy žádný
   `Active = false` neexistuje – inkonzistence napříč metodami).
2. Pokud autor zamýšlel mít "soft delete" pro znovupřipojení po pádu
   připojení, potom `Disconnect` nesmí dělat `RemoveAll(!Active)`. Tj. logika
   je polovičatá.

Návrh: **rozhodnout se** pro jeden ze dvou modelů:

- **A.** Odstranit `Active` úplně. `Disconnect` jen `Participants.RemoveAll(p
  => p.ConnectionId == connectionId)`. Filtry `p.Active` smazat ze všech
  metod. Toto je nejmenší a nejčistší změna konzistentní s aktuálním
  pozorovatelným chováním a je doporučená.
- **B.** Zachovat `Active` jako přechodný "tombstone" stav (pro mřížku UI při
  reconnectu): odstranit `Participants.RemoveAll(!Active)` z `Disconnect`,
  doplnit cleanup neaktivních z `CleanupInactiveParticipants` a sjednotit
  `RemoveUser` aby `p.Active` filtroval také. Doporučuji **jen** pokud
  existuje produktový požadavek.

### 3.2 🔴 `EndVoting` a `ResetVoting` neautorizují volajícího

```csharp
public bool EndVoting(string roomId, string connectionId)
{
    var room = _store.Get(roomId);
    if (room == null) { return false; }
    lock (room.SyncRoot)
    {
        if (room.VotingEnded) { return false; }
        room.VotingEnded = true;
        return true;
    }
}
```

Parametr `connectionId` je deklarovaný, ale **nikdy nepoužitý**. Stejný vzor
v `ResetVoting`. Důsledek:

- Klient může poslat `EndVoting` / `ResetVoting`, i kdyby nebyl v místnosti
  (Hub sice používá `_connectionMap` a vyfiltruje neznámou conn-id, ale
  Application vrstva *sama* žádnou autorizaci neprovádí). Pokud by se
  RoomService volal odjinud než z hubu – například z chystaného HTTP API –
  byl by zranitelný.
- Z původního Node serveru a z `RemoveUser` plyne, že akce typu
  end/reset/remove jsou určeny pozorovatelům. Současné chování však dovoluje
  end/reset i běžnému hlasujícímu z místnosti.

Návrh: rozhodnout product behavior a buď

```csharp
var requester = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
if (requester == null) { return false; }
// volitelně: if (!requester.IsObserver) { return false; }
```

Pokud je úmyslem nechat end/reset i hlasujícím, alespoň ověřit, že volající
je v místnosti, a parametr nepoužitý smazat (a hub upravit).

### 3.3 🟡 `Disconnect` má race se `_store.GetOrCreate` v paralelním `JoinRoom`

Sekvence:

1. `Disconnect("R", "conn1")` zavolá `_store.Get("R")` → vrátí `room`.
2. Před vstupem do `lock` proběhne `CleanupInactiveParticipants` – pod zámkem
   odstraní posledního participanta a zavolá `_store.Remove("R")`.
3. Současně `JoinRoom("R", "conn2", ...)` zavolá `_store.GetOrCreate("R")`,
   který vrátí **nový** `Room` instanci.
4. `Disconnect` konečně vstoupí do `lock` *staré* (osiřelé) room instance,
   nenajde nikoho a vyskočí. Klient `conn2` mezitím v jiné room instanci žije.

Reálné dopady jsou drobné, ale: a) `Disconnect` sahá do `_store.Remove(roomId)`
i pro osiřelou room – mohl by smazat *novou* room ze store, kdyby v ní mezitím
nikdo nebyl.

Návrh: po vstupu do zámku ověřit, že `_store.Get(roomId)` vrací stále tutéž
referenci, nebo (lépe) celé rozhodování o `_store.Remove` provádět atomicky
přes `IRoomStore` (`bool RemoveIfEmpty(Room expected)`).

### 3.4 🟡 Lineární vyhledávání podle `ConnectionId`

`Participants.FirstOrDefault(p => p.ConnectionId == ...)` – při pár
participantech v místnosti zanedbatelné, ale vzor se opakuje 7×.
Navrhuji v `Room` interní `Dictionary<string, Participant>` indexovaný
ConnectionId; `Participants` zveřejnit jako `Values`.

### 3.5 🟡 `ResolveName` je O(n²)

Pro každou kandidátní příponu prochází znovu všechny účastníky. Pro reálnou
velikost (≤ 30 účastníků) bezvýznamné, ale snadná oprava: vybudovat `HashSet`
`activeNames` jednou a hledat první volné `(2)`/`(3)`/… v cyklu. Drobnost.

### 3.6 🟢 Nepoužitý parametr `requesterConnectionId`

`EndVoting(string roomId, string connectionId)` a
`ResetVoting(string roomId, string connectionId)` parametr nepoužívají.
Buď použít (viz §3.2) nebo odstranit. V `RoomService.cs` je to varování-tichý
red flag.

### 3.7 🟢 Pojmenování `RoomStateDto.Users`

V Domain je název typu `Participant`, v API se jim říká `Users`. Doporučuji
sjednotit – buď `Participants` všude, nebo `Users` všude. Změna v DTO si
vyžádá drobnou úpravu front-endu (`socket-handlers.js` čte `data.users`).

### 3.8 🟢 Chybí `CancellationToken`

Aplikační metody jsou synchronní, takže CT není nutný; ale `IRoomService`
metody, které mohou *do budoucna* dělat I/O (persistence, distribuovaný
back-plane), by měly přijímat `CancellationToken`. Je to designové
rozhodnutí, které stojí za to udělat teď, než po vzniku volajícího kódu.

### 3.9 🟢 `RoomService` injektuje `IRoomStore` jako interface

Správně. Konstruktor by mohl být `=>` body:

```csharp
public RoomService(IRoomStore store) => _store = store;
```

(už je tak.) Při zavedení DI `primary constructor` (.NET 8+) by stačilo:

```csharp
public class RoomService(IRoomStore store) : IRoomService
{
    private readonly IRoomStore _store = store;
    ...
}
```

– kosmetika.

---

## 4. Web vrstva (`ScrumPoker.Web`)

### 4.1 🔴 `_connectionMap` jako `static` v hubu = sdílený globální stav

```csharp
private static readonly ConcurrentDictionary<string, (string RoomId, string UserName)>
    _connectionMap = new();
```

Problémy:

1. **Duplikuje source-of-truth.** `RoomService` už ví, ve které místnosti je
   která conn-id (přes `Room.Participants`). Mapa se může rozejít s realitou
   (např. když `HeartbeatCleanupService` účastníka vykopne, mapa zůstane).
2. **Hub je instance-per-call**; tahat statický stav je netestovatelné a hůř
   se mockuje.
3. Při hostování ve více procesech (scale-out) je tento dict per-instance,
   takže by stejně musel vzniknout SignalR back-plane (Redis) a vlastní
   centralizovaný registr.

Doporučená oprava (v pořadí preferencí):

1. **Použít `Context.Items`** – `HubCallerContext.Items` je per-connection
   slovník, který SignalR zachovává po celou dobu connection.
   Žádný globální stav, žádný leak:

   ```csharp
   public async Task JoinRoom(string room, string name, bool isObserver)
   {
       var result = _roomService.JoinRoom(room, Context.ConnectionId, name, isObserver);
       Context.Items["roomId"] = room;
       Context.Items["userName"] = result.ResolvedName;
       await Groups.AddToGroupAsync(Context.ConnectionId, room);
       ...
   }

   private bool TryGetRoom(out string roomId)
   {
       if (Context.Items.TryGetValue("roomId", out var r) && r is string s)
       {
           roomId = s;
           return true;
       }
       roomId = "";
       return false;
   }
   ```

   `OnDisconnectedAsync` o čištění mapy nemusí starat – SignalR ji uvolní s
   connection.

2. Pokud je nutný **globální** registr (např. cleanup service potřebuje
   broadcastovat na konkrétní conn-id), zavést samostatný DI singleton
   `IConnectionRegistry` injektovaný i do `ScrumPokerHub`, i do
   `HeartbeatCleanupService`. Static field nepoužívat.

### 4.2 🟡 Při force-disconnectu klient zůstane SignalR-připojen

```csharp
await Clients.Client(result.RemovedConnectionId).SendAsync("forcedDisconnect", ...);
await Groups.RemoveFromGroupAsync(result.RemovedConnectionId, info.RoomId);
```

Klient dostane `forcedDisconnect` zprávu, frontend přepne na login obrazovku –
ale connection samotné běží dál a klient se může okamžitě znovu přihlásit do
téže místnosti (frontend tomu nebrání). Pokud je to záměr, OK; jinak zvážit
`Context.Abort()` aplikované na cílový `HubCallerContext` (interně přes
`IHubContext` to nelze – musí se to delegovat přes `Context.ConnectionId` a
`HubLifetimeManager`, případně držet seznam `HubCallerContext` v registru, viz
§4.1).

### 4.3 🟡 Validace vstupů hubu

`JoinRoom(string room, string name, bool isObserver)` neověřuje:

- `string.IsNullOrWhiteSpace(room)`,
- `string.IsNullOrWhiteSpace(name)`,
- maximální délku jména (anti-DoS na vykreslení),
- whitelist znaků pro `room` (slouží jako ID v URL).

`Vote(string vote)` neověřuje, že `vote` je z povolené sady ("0",1,2,3,5,8,13,
"?", "☕"…). Doporučuji buď DTO + DataAnnotations + `services.AddSignalR()
.AddJsonProtocol()` s validací, nebo jednoduché throw `HubException` v hubu.

### 4.4 🟡 Anonymní payloady místo records

```csharp
await Clients.Caller.SendAsync("joinConfirmation",
    new { name = result.ResolvedName, isObserver = result.IsObserver });
await Clients.Caller.SendAsync("userRemoved",
    new { status = "success", userName = userToRemove });
await Clients.Caller.SendAsync("userRemoved",
    new { status = "error", userName = userToRemove, message = result.ErrorMessage });
```

- Anonymní typ `userRemoved` má **různé tvary** v success/error větvi
  (`message` chybí v success). To znesnadňuje strongly-typed klient v C#.
- Záleží na default casing politice JSON Hub Protocolu (camelCase by default).

Doporučuji zavést records v Application/Web vrstvě:

```csharp
public record JoinConfirmation(string Name, bool IsObserver);
public record UserRemovedResult(string Status, string UserName, string? Message = null);
public record ForcedDisconnect(string Message);
```

…a posílat je přes strongly-typed hub (§1.3).

### 4.5 🟡 V odpovědích chybí strukturované chybové stavy

`Vote`, `CancelVote`, `EndVoting`, `ResetVoting` při neplatném stavu mlčky
ukončí. Klient nedostane žádnou zpětnou vazbu. Pro debugovatelnost je vhodné
posílat `voteError` / log na úrovni warning. Z perspektivy bezpečnosti je
mlčení OK (neprozradíme nic), ale UX trpí.

### 4.6 🟡 Bez rate-limitu / payload limitů

Defaultní `HubOptions.MaximumReceiveMessageSize` = 32 KB; pro jednoduchý poker
je to víc než dost a může se snížit:

```csharp
services.AddSignalR(o =>
{
    o.MaximumReceiveMessageSize = 4 * 1024;
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
```

Rate-limit na hub call (např. `Vote` / `Heartbeat`) lze řešit jednoduchým
sliding-window per-connection middleware nebo `IHubFilter`.

### 4.7 🟡 Logování v `JoinRoom`

```csharp
_logger.LogInformation("User {Name} joined room {Room}{Observer}",
    result.ResolvedName, room, result.IsObserver ? " as observer" : "");
```

Vkládat formátovaný text do template stringu rozbije strukturované logování –
log indexy budou vidět záhadné `{Observer}` = "" / " as observer". Lépe:

```csharp
_logger.LogInformation("User {Name} joined room {Room} (observer={IsObserver})",
    result.ResolvedName, room, result.IsObserver);
```

### 4.8 🟢 `Heartbeat()` jako synchronní

```csharp
public Task Heartbeat()
{
    if (_connectionMap.TryGetValue(Context.ConnectionId, out var info))
    {
        _roomService.UpdateHeartbeat(info.RoomId, Context.ConnectionId);
    }
    return Task.CompletedTask;
}
```

Drobnost: deklarovat `public void Heartbeat()` – SignalR umožňuje `void` hub
metody. Klient nemá důvod čekat na ack heartbeatu.

### 4.9 🟢 `JoinRoom` po reconnectu volá `AddToGroupAsync` znovu

`AddToGroupAsync` je idempotentní, takže se nic zlého neděje, ale kdyby se
metoda zavolala 1000× (špatně chovající se klient), vznikne 1000 záznamů ve
vnitřních strukturách SignalR. Nepatrné, jen pro úplnost.

---

## 5. Hosting & konfigurace (`Program.cs`, `HeartbeatCleanupService`)

### 5.1 🟡 `HeartbeatCleanupService` má hard-coded threshold

```csharp
private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);
private static readonly TimeSpan InactivityThreshold = TimeSpan.FromMinutes(10);
```

Doporučuji vytáhnout do `IOptions<HeartbeatOptions>` s defaulty a `appsettings`
sekcí `Heartbeat:CheckInterval` / `Heartbeat:InactivityThreshold`.

### 5.2 🟡 Smyčka přes `Task.Delay` místo `PeriodicTimer`

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    try { ... } catch (...) { ... }
    await Task.Delay(CheckInterval, stoppingToken);
}
```

Modernější (a méně náchylná na drift) varianta:

```csharp
using var timer = new PeriodicTimer(_options.CheckInterval);
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    try { ... } catch (Exception ex) when (ex is not OperationCanceledException) { ... }
}
```

### 5.3 🟡 Cleanup neoznámí klienta o jeho odpojení

Když cleanup vyhodí inaktivního účastníka, ostatním v místnosti pošle
`roomUpdate`, ale samotnému dotčenému klientovi (pokud má SignalR connection
ještě živé) nepošle nic. Ten v UI nadále vidí starou místnost. Vhodné opravit
po sjednocení s §4.1 (mít registr `connectionId → roomId` na jednom místě
a poslat `forcedDisconnect`).

### 5.4 🟡 Health-checks pouze v Development

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks(HealthEndpointPath);
    app.MapHealthChecks(AlivenessEndpointPath, ...);
}
```

Toto je default Aspire šablony, ale v praxi se tyto endpointy v produkci
hodí (Kubernetes liveness/readiness probes, Aspire dashboard). Pokud cílíme
nasazení na K8s, prefix podmínkou `Development` zablokuje monitoring.
Doporučuji zveřejnit health-checks vždy a chránit je (např. host header check
nebo bind na loopback).

### 5.5 🟢 `AddSignalR` bez konfigurace

V `Program.cs`:

```csharp
builder.Services.AddSignalR();
```

Doplnit limity (§4.6) a v dev `EnableDetailedErrors = true` pro lepší DX.

### 5.6 🟢 `app.UseDefaultFiles()` před `app.UseStaticFiles()` ✅ – správné pořadí

Žádná změna.

---

## 6. Testy

### 6.1 ✅ Unit testy (`RoomServiceTests`) – kvalitní pokrytí

- Striktní AAA (`// Arrange / Act / Assert`).
- `AwesomeAssertions` v souladu s `AGENTS.md`.
- Pokrývá join (vč. duplicit a reconnectu), vote, cancel, end-voting, reset,
  remove (vč. neg. cest), disconnect, cleanup.

### 6.2 🟡 Chybějící unit testy

- `EndVoting` / `ResetVoting` z conn-id, který v místnosti není – aktuálně
  uspěje (souvisí s §3.2). Po opravě by se mělo testovat.
- `RemoveUser`, kde požadovaný uživatel je sám pozorovatel.
- `Disconnect` neexistujícího `roomId` (nesmí padnout).
- `UpdateHeartbeat` před `CleanupInactiveParticipants` skutečně zachová
  účastníka (currently testujete jen "act.Should().NotThrow()"; neověřujete
  efekt). Otestujte timestamp manipulací času (nejlépe injektovat
  `TimeProvider`, viz §6.4).

### 6.3 🟡 Integrační testy `ScrumPokerHubTests`

- Pokrývají join + room update + vote + end + reset.
- Chybí: `RemoveUser` (force-disconnect), `Heartbeat`, reconnect (rejoin) a
  násobné klienty v jedné místnosti. Násobné klienty jsou důležité, protože
  některé bugy se zviditelní jen mezi účastníky.

### 6.4 🟢 Nedeterminismus s časem

`CleanupInactiveParticipants` je závislé na `DateTime.UtcNow`. Pro robustnější
testování injektovat `TimeProvider` (k dispozici od .NET 8 s
`FakeTimeProvider` v `Microsoft.Extensions.TimeProvider.Testing`). Drobné, ale
kvalitní zlepšení.

### 6.5 🟢 `HostStartupTests` jsou OK ✅

---

## 7. Bezpečnost

### 7.1 🔴 Žádná autentizace ani autorizace na úrovni hubu

Aplikace umožňuje komukoliv zavolat libovolnou hub metodu na libovolné
místnosti. To je *pravděpodobně* záměr (veřejný free tool), ale je vhodné to
explicitně dokumentovat (a zaškrtnout že to vědí majitelé). Pokud je
interpretace "místnost zná jen ten, kdo má URL" záměrem zabezpečení, pak
varianta s neuhodnutelným `roomId` by měla být povinná – aktuálně si klient
volí `roomId` libovolně, takže lze chodit do cizích místností uhádnutím
jména. Návrh: server-side generovat `roomId` jako 22-znakové URL-safe ID
a klientovi ho přidělovat až po `CreateRoom`-like volání. Toto je produktové
rozhodnutí.

### 7.2 🟡 Žádná validace `roomId` jako součásti URL/komunikace

V `RoomStore` `_rooms.GetOrAdd(roomId, ...)`. Kdokoliv pošle `JoinRoom("",
"a", false)` vytvoří místnost s prázdným ID. `JoinRoom("../foo", ...)`
vytvoří roomId zahrnující URL nebezpečné znaky (frontend je dává do URL přes
copyLinkBtn). Doporučuji whitelist `^[A-Za-z0-9_-]{3,64}$`.

### 7.3 🟡 XSS přes `Name`

Server `Name` jen předává; renderování probíhá na frontendu. Pokud frontend
používá `innerHTML` se jménem, vznikne XSS. (Z `ui-elements.js` je vidět
`updateUsersList()` – nutno potvrdit, že používá `textContent`. Mimo rozsah
této review, ale je to relevantní bezpečnostní bod plynoucí z chybějící
sanitace na serveru.)

### 7.4 🟢 OpenTelemetry traces filtrují health-endpointy

Správně, neznečišťuje to traces.

---

## 8. Drobnosti / styl

### 8.1 🟢 `csharp_prefer_braces = true` v `.editorconfig` ✅ – konvence dodržena

### 8.2 🟢 Chybějící XML dokumentace

`IRoomService` je veřejný kontrakt mezi vrstvami; chybí XML komentáře –
specielně u sémanticky nejasných hodnot vracení (`bool` z `Vote` znamená co?
"bylo to platné", nebo "došlo ke změně"?).

### 8.3 🟢 `RoomStateDto.Users` je `IReadOnlyList<ParticipantDto>` ✅

`ProjectRoom` provádí `.ToList()` – takže klient nedrží referenci na živý
seznam. OK.

### 8.4 🟢 Sjednotit prázdné konstruktory

`RoomService` používá expression-bodied `=> _store = store;`, jiné soubory
mají blok. Drobné. Přejít na primary constructors pro celou Application
vrstvu by sjednotilo.

### 8.5 🟢 `ScrumPoker.Web/wwwroot/README.md` – ověřit, že popisuje aktuální
strukturu po migraci ze Socket.IO na SignalR (zejména že zmiňuje
`signalr.min.js`, ne `socket.io.min.js`).

---

## 9. Shrnutí navrhovaných úprav

V pořadí podle priority – pokud má být review akceschopné, doporučuji rozdělit
do dvou (či tří) menších PR proti `feature/csharp-backend`:

### PR A – chování & bezpečnost (musí jít před cut-over do produkce)

1. §3.1 – odstranit `Active` flag (alternativně dotáhnout B-variantu).
2. §3.2 – v `EndVoting` / `ResetVoting` ověřit `connectionId` (a vyhodnotit,
   zda omezit na pozorovatele).
3. §4.1 – odstranit `static _connectionMap`, využít `Context.Items` nebo DI
   `IConnectionRegistry`.
4. §4.3 / §7.2 – serverová validace vstupů (`roomId`, `name`, `vote`).

### PR B – design & encapsulace

5. §1.2 / §2.3 / §2.4 – posunout invarianty do `Room`, schovat `SyncRoot`,
   `Participants` jen pro čtení.
6. §1.3 / §4.4 – zavést strongly-typed hub a records pro klientské zprávy.
7. §3.3 – atomický `RemoveIfEmpty` v `IRoomStore`.
8. §3.6 – odstranit nepoužitý `requesterConnectionId` (po aplikaci §3.2).

### PR C – polish & ops

9. §5.1 / §5.2 – `IOptions<HeartbeatOptions>` + `PeriodicTimer`.
10. §5.4 – health-checks v produkci.
11. §6.2 / §6.3 / §6.4 – doplnit testy, injektovat `TimeProvider`.
12. §4.7 / §8.2 – logování bez ohýbání template stringu, XML docs.
13. §1.1 – rozhodnout o budoucnosti AppHostu (zachovat / přidat zdroje /
    odstranit).

---

## 10. Co je naopak fajn (pochvaly)

- **Centralizovaná balíčková správa** přes `Directory.Packages.props` +
  `WarningsAsErrors=Nullable` + `AnalysisLevel=10.0` – moderní setup.
- **Vrstvení dependency-arrows** je zdravé; Domain neimportuje nic mimo BCL.
- **Unit testy** mají striktní AAA, čisté pojmenování, AwesomeAssertions.
- **Heartbeat / cleanup** logika je v BackgroundService (ne ve hubu samotném)
  – správné rozdělení.
- **Aspire ServiceDefaults** – health-checks, OTel, resilience – připraveno
  pro budoucí rozšíření.
- **Migrace Socket.IO → SignalR** ve frontendu si zachovala stejnou událostní
  smlouvu (`roomUpdate`, `joinConfirmation`, `forcedDisconnect`) – minimální
  riziko regrese.

---

*Review zpracováno na commitu `28e26f9` ("Centralize .NET build properties and
package versions").*
