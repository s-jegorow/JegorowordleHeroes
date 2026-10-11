const board = document.getElementById('board');
const keyboardEl = document.getElementById('keyboard');
const msg = document.getElementById('message');
const p1 = document.getElementById('p1'), p2 = document.getElementById('p2');

const letters = "QWERTZUIOPASDFGHJKLYXCVBNM".split("");
const layout = ["QWERTZUIOP", "ASDFGHJKL", "YXCVBNM"];
let stateKeys = {};
const STATE_CLASSES = ["off", "mid", "ok"];
const FLIP_DURATION = 500;
const FLIP_STAGGER = 250;

let currentRow = 0, currentCol = 0;
let grid = Array.from({ length: 6 }, () => Array(5).fill(""));
let connection = null;
let roomCode = "", myName = "";
const playerId = loadPlayerId();
let gameOver = false;
let completedRows = [];
let inputLocked = false;
let revealDone = Promise.resolve();

function loadPlayerId() {
    let id = sessionStorage.getItem("playerId");
    if (!id) {
        id = crypto.randomUUID();
        sessionStorage.setItem("playerId", id);
    }
    return id;
}

function renderBoard() {
    board.innerHTML = "";
    board.style.gridTemplateRows = `repeat(6,1fr)`;
    for (let r = 0; r < 6; r++) {
        const row = document.createElement('div'); row.className = "row";
        for (let c = 0; c < 5; c++) {
            const cell = document.createElement('div'); cell.className = "cell";
            cell.textContent = grid[r][c] || "";
            
            if (completedRows[r] && completedRows[r][c]) {
                cell.className = completedRows[r][c];
                cell.textContent = cell.textContent.toUpperCase();
            }
            
            row.appendChild(cell);
        }
        board.appendChild(row);
    }
}
renderBoard();

function renderKeyboard() {
    keyboardEl.innerHTML = "";
    layout.forEach((rowLetters, idx) => {
        const row = document.createElement('div');
        row.className = "key-row";

        if (idx === 1) row.appendChild(createSpacer());
        if (idx === 2) row.appendChild(createKey("ENTER", submit, "wide"));

        rowLetters.split("").forEach(ch => {
            const k = createKey(ch, () => press(ch));
            const st = stateKeys[ch];
            if (st) k.classList.add(st);
            row.appendChild(k);
        });

        if (idx === 1) row.appendChild(createSpacer());
        if (idx === 2) row.appendChild(createKey("⌫", backspace, "wide"));

        keyboardEl.appendChild(row);
    });
}

function createKey(label, onClick, extraClass) {
    const k = document.createElement('button');
    k.className = "key";
    if (extraClass) k.classList.add(extraClass);
    k.textContent = label;
    k.onclick = onClick;
    return k;
}

function createSpacer() {
    const spacer = document.createElement('div');
    spacer.className = "key-spacer";
    return spacer;
}
renderKeyboard();

function press(ch) {
    if (gameOver || currentRow >= 6 || inputLocked) return;
    if (currentCol < 5) {
        grid[currentRow][currentCol++] = ch.toLowerCase();
        renderBoard();
    }
}

function backspace() {
    if (gameOver || currentRow >= 6 || inputLocked) return;
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
    if (gameOver || currentRow >= 6 || inputLocked) return;
    if (currentCol !== 5) { showMessage("5 Buchstaben nötig", "message-fail"); return; }
    const guess = grid[currentRow].join("");

    inputLocked = true;
    try {
        await connection.invoke("SubmitGuess", roomCode, guess);
        await revealDone;
    } finally {
        inputLocked = false;
    }
}

function applyResult(rowIdx, result) {
    const row = board.children[rowIdx];
    const rowClasses = [];

    for (let i = 0; i < 5; i++) {
        const cell = row.children[i];
        const state = STATE_CLASSES[result.letters[i]];
        cell.textContent = result.guess[i].toUpperCase();
        rowClasses[i] = "cell " + state;

        setTimeout(() => cell.classList.add("flip"), i * FLIP_STAGGER);
        setTimeout(() => cell.classList.add(state), i * FLIP_STAGGER + FLIP_DURATION / 2);
    }

    completedRows[rowIdx] = rowClasses;

    return new Promise((resolve) => {
        setTimeout(() => {
            for (let i = 0; i < 5; i++) {
                updateKey(result.guess[i], STATE_CLASSES[result.letters[i]]);
            }
            renderKeyboard();
            resolve();
        }, 4 * FLIP_STAGGER + FLIP_DURATION);
    });
}

function restoreState(guesses) {
    grid = Array.from({ length: 6 }, () => Array(5).fill(""));
    completedRows = [];
    stateKeys = {};
    gameOver = false;
    board.classList.remove("win", "fail");

    guesses.forEach((result, r) => {
        grid[r] = result.guess.split("");
        completedRows[r] = result.letters.map(st => "cell " + STATE_CLASSES[st]);
        for (let i = 0; i < 5; i++) {
            updateKey(result.guess[i], STATE_CLASSES[result.letters[i]]);
        }
    });

    currentRow = guesses.length;
    currentCol = 0;
    renderBoard();
    renderKeyboard();
}

function updateKey(ch, st) {
    const cur = stateKeys[ch.toUpperCase()];
    const rank = { off: 0, mid: 1, ok: 2 };
    if (!cur || rank[st] > rank[cur]) stateKeys[ch.toUpperCase()] = st;
}

const chatOpenBtn = document.getElementById('chat-open');
const chatBadge = document.getElementById('chat-badge');
const chatPreview = document.getElementById('chat-preview');
const chatPanel = document.getElementById('chat-panel');
const chatMessages = document.getElementById('chat-messages');
const chatForm = document.getElementById('chat-form');
const chatInput = document.getElementById('chat-input');

let chatOpen = false;
let unreadCount = 0;
let previewTimer = null;

function openChat() {
    chatOpen = true;
    chatPanel.classList.add('open');
    chatOpenBtn.setAttribute('aria-expanded', 'true');
    unreadCount = 0;
    updateBadge();
    hidePreview();
    chatInput.focus();
}

function closeChat() {
    chatOpen = false;
    chatPanel.classList.remove('open');
    chatOpenBtn.setAttribute('aria-expanded', 'false');
    document.activeElement.blur();
}

function updateBadge() {
    chatBadge.textContent = unreadCount;
    chatBadge.hidden = unreadCount === 0;
}

function showPreview(name, text) {
    chatPreview.textContent = name + ": " + text;
    chatPreview.classList.add('visible');
    clearTimeout(previewTimer);
    previewTimer = setTimeout(hidePreview, 4000);
}

function hidePreview() {
    chatPreview.classList.remove('visible');
}

function appendChatMessage(senderId, name, text) {
    const item = document.createElement('div');
    item.className = "chat-msg";
    if (senderId === playerId) item.classList.add("mine");

    const nameEl = document.createElement('span');
    nameEl.className = "chat-name";
    nameEl.textContent = name;

    const textEl = document.createElement('span');
    textEl.textContent = text;

    item.append(nameEl, textEl);
    chatMessages.appendChild(item);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}


chatOpenBtn.onclick = openChat;
document.getElementById('chat-close').onclick = closeChat;

chatForm.onsubmit = async (e) => {
    e.preventDefault();
    const text = chatInput.value.trim();
    if (!connection || !text) return;

    try {
        await connection.invoke("SendChat", roomCode, text);
        chatInput.value = "";
    } catch {
        showMessage("Nachricht konnte nicht gesendet werden", "message-fail");
    }
};

const joinBtn = document.getElementById('join');

async function leaveRoom() {
    if (!connection) return;
    await connection.stop();
    connection = null;
    joinBtn.disabled = false;
    sessionStorage.removeItem("roomCode");
}

joinBtn.onclick = async () => {
    if (connection) return;
    roomCode = document.getElementById('room').value.trim().toUpperCase();
    myName = document.getElementById('me').value.trim() || "Spieler";

    if (!roomCode) { showMessage("Bitte Session-Code angeben", "message-fail"); return; }

    joinBtn.disabled = true;

    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamehub")
        .withAutomaticReconnect()
        .build();

    connection.on("PlayersUpdated", (a, b) => {
        p1.textContent = a || "–";
        p2.textContent = b || "–";
    });

    connection.on("GameReady", (maxRows, wordLen) => {
        showMessage("Spiel bereit – viel Erfolg!", "message-success");
    });

    connection.on("InvalidGuess", () => showMessage("Ungültiges Wort", "message-fail"));
    connection.on("NoGuessesLeft", () => showMessage("Keine Versuche mehr", "message-fail"));
    connection.on("RoomFull", () => showMessage("Raum ist schon voll!", "message-fail"));

    connection.on("PlayerLeft", (name) => {
        showMessage(name + " hat die Verbindung verloren", "message-fail");
    });

    connection.on("ChatMessage", (senderId, name, text) => {
        appendChatMessage(senderId, name, text);
        if (chatOpen || senderId === playerId) return;
        unreadCount++;
        updateBadge();
        showPreview(name, text);
    });

    connection.on("RestoreState", restoreState);

    connection.onreconnected(async () => {
        await connection.invoke("CreateOrJoin", roomCode, playerId, myName);
    });

    connection.on("GuessAccepted", (res) => {
        revealDone = applyResult(currentRow, res);
        if (!res.isWin) {
            currentRow++; currentCol = 0;
        } else {
            gameOver = true;
        }
    });

    connection.on("OpponentGuessed", (name, count) => {
        showMessage(name + " hat Versuch " + count + "/6 abgegeben");
    });

    connection.on("GameOver", async (data) => {
        gameOver = true;
        await revealDone;
        const { winnerName, winnerId, byWin, targetWord, playerAGuesses, playerBGuesses, playerAName, playerBName } = data;
        
        let message = "";
        let messageClass = "message-fail";
        
        if (byWin) {
            if (winnerId === playerId) {
                message = `Gewonnen mit ${currentRow + 1} Versuchen!`;
                messageClass = "message-success";
                board.classList.add("win");
            } else {
                const winnerGuesses = winnerName === playerAName ? playerAGuesses : playerBGuesses;
                message = `Verloren! ${winnerName} war mit ${winnerGuesses} Versuchen besser. Das Wort war: ${targetWord?.toUpperCase()}`;
                board.classList.add("fail");
            }
        } else {
            message = `Alle Versuche verbraucht! Das Lösungswort war: ${targetWord?.toUpperCase()}`;
            board.classList.add("fail");
        }
        
        showMessage(message, messageClass);
    });

    try {
        await connection.start();
        const mask = await connection.invoke("CreateOrJoin", roomCode, playerId, myName);
        if (mask) {
            chatOpenBtn.hidden = false;
            sessionStorage.setItem("roomCode", roomCode);
            sessionStorage.setItem("playerName", myName);
        } else {
            await leaveRoom();
        }
    } catch {
        showMessage("Verbindung fehlgeschlagen", "message-fail");
        await leaveRoom();
    }
};

document.addEventListener('keydown', (e) => {
    if (chatOpen) {
        if (e.key === "Escape") closeChat();
        return;
    }
    if (!connection) return;
    if (e.target.tagName === "INPUT") return;
    const k = e.key;
    if (/^[a-zA-Z]$/.test(k)) { press(k.toUpperCase()); }
    else if (k === "Backspace") { backspace(); }
    else if (k === "Enter") { e.preventDefault(); submit(); }
});

const savedRoom = sessionStorage.getItem("roomCode");
if (savedRoom) {
    document.getElementById('room').value = savedRoom;
    document.getElementById('me').value = sessionStorage.getItem("playerName") || "";
    joinBtn.click();
}
