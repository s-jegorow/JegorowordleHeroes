// UI-Grundaufbau
const board = document.getElementById('board');
const keyboardEl = document.getElementById('keyboard');
const msg = document.getElementById('message');
const p1 = document.getElementById('p1'), p2 = document.getElementById('p2');

const letters = "QWERTZUIOPASDFGHJKLYXCVBNM".split("");
const layout = ["QWERTZUIOP", "ASDFGHJKL", "YXCVBNM"];
const stateKeys = {}; // Buchstabe -> best state

let currentRow = 0, currentCol = 0;
let grid = Array.from({ length: 6 }, () => Array(5).fill(""));
let connection = null;
let roomCode = "", myName = "", otherName = "";
let gameOver = false;

// Board render
function renderBoard() {
    board.innerHTML = "";
    board.style.gridTemplateRows = `repeat(6,1fr)`;
    for (let r = 0; r < 6; r++) {
        const row = document.createElement('div'); row.className = "row";
        for (let c = 0; c < 5; c++) {
            const cell = document.createElement('div'); cell.className = "cell";
            cell.textContent = grid[r][c] || "";
            row.appendChild(cell);
        }
        board.appendChild(row);
    }
}
renderBoard();

// Keyboard render
function renderKeyboard() {
    keyboardEl.innerHTML = "";
    const rows = layout.map(r => r.split(""));
    rows.forEach((row, idx) => {
        row.forEach(ch => {
            const k = document.createElement('button');
            k.className = "key";
            k.textContent = ch;
            k.onclick = () => press(ch);
            const st = stateKeys[ch];
            if (st === "ok") k.classList.add("ok"); else if (st === "mid") k.classList.add("mid"); else if (st === "off") k.classList.add("off");
            keyboardEl.appendChild(k);
        });
        if (idx === 1) {
            const enter = document.createElement('button');
            enter.className = "key wide"; enter.textContent = "ENTER"; enter.onclick = submit;
            keyboardEl.appendChild(enter);
            const back = document.createElement('button');
            back.className = "key wide"; back.textContent = "⌫"; back.onclick = backspace;
            keyboardEl.appendChild(back);
        }
    });
}
renderKeyboard();

function press(ch) {
    if (gameOver) return;
    if (currentCol < 5) {
        grid[currentRow][currentCol++] = ch.toLowerCase();
        renderBoard();
    }
}
function backspace() {
    if (gameOver) return;
    if (currentCol > 0) {
        grid[currentRow][--currentCol] = "";
        renderBoard();
    }
}
function showMessage(text, cls) {
    msg.className = cls || "";
    msg.textContent = text;
}
async function submit() {
    if (gameOver) return;
    if (currentCol !== 5) { showMessage("5 Buchstaben nötig", "message-fail"); return; }
    const guess = grid[currentRow].join("");
    await connection.invoke("SubmitGuess", roomCode, guess);
}

function applyResult(rowIdx, result) {
    const row = board.children[rowIdx];
    for (let i = 0; i < 5; i++) {
        const cell = row.children[i];
        cell.textContent = result.guess[i].toUpperCase();
        const st = result.letters[i];
        if (st === 2) { cell.classList.add("ok"); updateKey(result.guess[i], 'ok'); }
        else if (st === 1) { cell.classList.add("mid"); updateKey(result.guess[i], 'mid'); }
        else { cell.classList.add("off"); updateKey(result.guess[i], 'off'); }
    }
    renderKeyboard();
}

function updateKey(ch, st) {
    const cur = stateKeys[ch.toUpperCase()];
    const rank = { off: 0, mid: 1, ok: 2 };
    if (!cur || rank[st] > rank[cur]) stateKeys[ch.toUpperCase()] = st;
}

document.getElementById('join').onclick = async () => {
    roomCode = document.getElementById('room').value.trim().toUpperCase();
    myName = document.getElementById('me').value.trim() || "Spieler";
    otherName = document.getElementById('other').value.trim() || "Gegner";

    if (!roomCode) { showMessage("Bitte Session-Code angeben", "message-fail"); return; }

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamehub")
        .withAutomaticReconnect()
        .build();

    // Hub Events
    connection.on("PlayersUpdated", (a, b) => {
        p1.textContent = a || "–";
        p2.textContent = b || "–";
    });

    connection.on("GameReady", (maxRows, wordLen) => {
        showMessage("Spiel bereit – viel Erfolg!", "message-success");
    });

    connection.on("InvalidGuess", () => showMessage("Ungültiges Wort", "message-fail"));
    connection.on("NoGuessesLeft", () => showMessage("Keine Versuche mehr", "message-fail"));

    connection.on("GuessAccepted", (res) => {
        applyResult(currentRow, res);
        if (!res.isWin) {
            currentRow++; currentCol = 0;
        } else {
            gameOver = true;
            board.classList.add("win");
        }
    });

    connection.on("OpponentGuessed", (name, res) => {
        // optional: Statusmeldung
    });

    connection.on("GameOver", (winnerName, byWin) => {
        gameOver = true;
        if (byWin) {
            if (winnerName === myName) {
                showMessage("Gewonnen!", "message-success");
                board.classList.add("win");
            } else {
                showMessage("Verloren!", "message-fail");
                board.classList.add("fail");
            }
        } else {
            showMessage("Runde vorbei!", "message-fail");
            board.classList.add("fail");
        }
    });

    await connection.start();
    await connection.invoke("CreateOrJoin", roomCode, myName);
};

// Tastatur-Events
document.addEventListener('keydown', (e) => {
    if (!connection) return;
    const k = e.key;
    if (/^[a-zA-Z]$/.test(k)) { press(k.toUpperCase()); }
    else if (k === "Backspace") { backspace(); }
    else if (k === "Enter") { submit(); }
});
