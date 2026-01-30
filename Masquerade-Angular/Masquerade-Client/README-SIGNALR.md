# SignalR - instrukcje instalacji i testowania

Ten plik opisuje jak zainstalować klienta SignalR i uruchomić testowy, standalone komponent `SignalrTestComponent`.

1. Zainstaluj zależności (w katalogu projektu klienta Angular):

```bash
cd Masquerade-Angular/Masquerade-Client
npm install
```

2. (Alternatywnie wymuś instalację tylko klienta SignalR):

```bash
npm install @microsoft/signalr
```

3. Uruchom deweloperski serwer Angular:

```bash
npm start
# lub: ng serve
```

4. Otwórz przeglądarkę pod adresem `http://localhost:4200`.

5. Aplikacja powinna przekierować na trasę `/signalr-test` i wyświetlić prosty UI do łączenia z hubem i wysyłania wiadomości.

6. Upewnij się, że backend (.NET) jest uruchomiony i nasłuchuje (np. `dotnet run --project ../Masquerade-GGJ-2026`), a w `Program.cs` masz zmapowany hub pod `/hubs/echo`.

Uwagi:
- Jeśli backend działa na innym porcie lub hostcie, podaj ten adres w polu `Server URL` w komponencie testowym.
- CORS: w `Program.cs` została dodana polityka CORS dla `http://localhost:4200` — jeśli używasz innego origin, zaktualizuj ją.
