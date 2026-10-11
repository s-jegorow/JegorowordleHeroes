# JegorowordleHeroes

Wordle für zwei Spieler in Echtzeit. Beide raten dasselbe Wort, wer es zuerst findet, gewinnt. Entstanden als Übung für SignalR.

## Stack

ASP.NET Core 9 (MVC), SignalR, JS

## Funktionen

- Räume über Session-Code, Einladungslink per `?room=`
- Live-Fortschritt des Gegners
- Chat mit Systemmeldungen
- Reconnect und Reload ohne Spielverlust
- Responsives Layout

## Starten

dotnet run

## ToDo

- Optisches Tuning
- UX-Verbesserungen
- User-Verwaltung
- Spectator-Modus