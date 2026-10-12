# JegorowordleHeroes

Wordle für zwei Spieler in Echtzeit. Beide raten dasselbe Wort, wer es zuerst findet, gewinnt. Entstanden als Übung für SignalR.

## Stack

ASP.NET Core 9 (MVC), SignalR, ASP.NET EF Core mit SQLite, ASP.NET Core  Identity, JS

## Funktionen

- Räume über Session-Code, Zufallsgegner, Einladungslink per `?room=`
- Live-Fortschritt des Gegners
- Punktevergabe
- Chat mit Systemmeldungen
- Reconnect und Reload ohne Spielverlust
- Responsives Layout
- Login via ASP.NET Core Identity 

## Starten

dotnet tool restore
dotnet ef database update
dotnet run

dotnet run

## ToDo

- Optisches Tuning
- Avatare
- UX-Verbesserungen
- Spectator-Modus
