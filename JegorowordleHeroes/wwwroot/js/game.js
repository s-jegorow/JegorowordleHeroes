const board = document.getElementById('board');
const keyboardEl = document.getElementById('keyboard');
const toast = document.getElementById('toast');
const overlay = document.getElementById('overlay');
const overlayTitle = document.getElementById('overlay-title');
const overlaySub = document.getElementById('overlay-sub');
const overlayExtra = document.getElementById('overlay-extra');
const duel = document.getElementById('duel');
const p1 = document.getElementById('p1'), p2 = document.getElementById('p2');
const slots = [
    { box: document.getElementById('duel-p1'), name: p1, avatar: document.getElementById('p1-avatar'), info: document.getElementById('p1-info'), id: null },
    { box: document.getElementById('duel-p2'), name: p2, avatar: document.getElementById('p2-avatar'), info: document.getElementById('p2-info'), id: null }
];
const START_OVERLAY_MS = 1500;
const TOAST_MS = 2500;
let overlayTimer = null;
let toastTimer = null;

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
let playerId = loadPlayerId();
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
    clearTimeout(toastTimer);
    if (!text) {
        toast.hidden = true;
        return;
    }
    toast.className = "toast " + (cls || "");
    toast.textContent = text;
    toast.hidden = false;
    toastTimer = setTimeout(() => { toast.hidden = true; }, TOAST_MS);
}

function showOverlay(type, title, sub) {
    clearTimeout(overlayTimer);
    overlay.className = "overlay overlay-" + type;
    overlayTitle.textContent = title;
    overlaySub.textContent = sub;
    overlayExtra.replaceChildren();
    overlay.hidden = false;
}

function hideOverlay() {
    clearTimeout(overlayTimer);
    overlay.hidden = true;
}

function showStartOverlay() {
    showOverlay("start", "DUELL!", `${p1.textContent} gegen ${p2.textContent}`);
    overlayTimer = setTimeout(hideOverlay, START_OVERLAY_MS);
}

function showPoints(points) {
    const pill = document.createElement('div');
    pill.className = "overlay-points";
    pill.textContent = `+${points} Punkte`;
    overlayExtra.append(pill);
}

function showSolution(word) {
    const label = document.createElement('div');
    label.className = "overlay-label";
    label.textContent = "Das Wort war";
    const tiles = document.createElement('div');
    tiles.className = "overlay-word";
    for (const ch of word.toUpperCase()) {
        const tile = document.createElement('span');
        tile.textContent = ch;
        tiles.append(tile);
    }
    overlayExtra.append(label, tiles);
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

function shakeRow(rowIdx) {
    const row = board.children[rowIdx];
    row.classList.add("shake");
    row.addEventListener("animationend", () => row.classList.remove("shake"), { once: true });
}

function restoreState(guesses) {
    grid = Array.from({ length: 6 }, () => Array(5).fill(""));
    completedRows = [];
    stateKeys = {};
    gameOver = false;
    board.classList.remove("win", "fail");
    rematchBtn.hidden = true;
    newOpponentBtn.hidden = true;
    hideOverlay();
    for (const slot of slots) {
        slot.box.classList.remove("winner", "loser");
        if (slot.id) slot.info.textContent = "0/6";
    }

    guesses.forEach((result, r) => {
        grid[r] = result.guess.split("");
        completedRows[r] = result.letters.map(st => "cell " + STATE_CLASSES[st]);
        for (let i = 0; i < 5; i++) {
            updateKey(result.guess[i], STATE_CLASSES[result.letters[i]]);
        }
    });

    currentRow = guesses.length;
    currentCol = 0;
    setGuessCount(playerId, guesses.length);
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

function avatarUrl(avatar) {
    return avatar > 0 ? `/img/avatars/avatar${avatar}.jpg` : "/img/avatars/avatarguest.jpg";
}

function showPlayer(slot, player) {
    slot.id = player ? player.id : null;
    slot.name.textContent = player ? player.name : "wartet …";
    slot.avatar.src = avatarUrl(player ? player.avatar : 0);
    slot.info.textContent = player ? `${player.guesses}/6` : "";
    slot.box.classList.toggle("empty", !player);
}

function findSlot(id) {
    return slots.find(slot => slot.id === id);
}

function setGuessCount(id, count) {
    const slot = findSlot(id);
    if (slot) slot.info.textContent = `${count}/6`;
}

function appendChatMessage(senderId, name, text, avatar) {
    const item = document.createElement('div');
    item.className = "chat-msg";
    if (senderId === playerId) item.classList.add("mine");

    const nameEl = document.createElement('span');
    nameEl.className = "chat-name";
    const avatarEl = document.createElement('img');
    avatarEl.className = "avatar avatar-chat";
    avatarEl.alt = "";
    avatarEl.src = avatarUrl(avatar);
    nameEl.append(avatarEl, name);

    const textEl = document.createElement('span');
    textEl.textContent = text;

    item.append(nameEl, textEl);
    chatMessages.appendChild(item);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

// spielstatus im chat
function appendSystemMessage(text) {
    const item = document.createElement('div');
    item.className = "chat-system";
    item.textContent = text;
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
const searchBtn = document.getElementById('search');
const roomInput = document.getElementById('room');
const modeFriendBtn = document.getElementById('mode-friend');
const modeRandomBtn = document.getElementById('mode-random');
const rematchBtn = document.getElementById('rematch');
const newOpponentBtn = document.getElementById('new-opponent');
const meInput = document.getElementById('me');
const topSection = document.querySelector('.top');
const searchInfo = document.getElementById('search-info');
const searchTime = document.getElementById('search-time');
const roomInfo = document.getElementById('room-info');
const roomLabel = document.getElementById('room-label');
const copyLinkBtn = document.getElementById('copy-link');
const leaveRoomBtn = document.getElementById('leave-room');
const currentIdentity = meInput.readOnly ? meInput.value : "";
const pageTitle = document.title;
let searching = false;
let searchTimer = null;
let isRandomRoom = false;

function setMode(random) {
    modeFriendBtn.classList.toggle('active', !random);
    modeRandomBtn.classList.toggle('active', random);
    modeFriendBtn.setAttribute('aria-pressed', String(!random));
    modeRandomBtn.setAttribute('aria-pressed', String(random));
    roomInput.hidden = random;
    joinBtn.hidden = random;
    searchBtn.hidden = !random;
}

modeFriendBtn.onclick = () => setMode(false);
modeRandomBtn.onclick = () => setMode(true);

function showRematchButton() {
    rematchBtn.hidden = false;
    rematchBtn.disabled = false;
    rematchBtn.textContent = "Revanche";
    rematchBtn.classList.remove("rematch-requested");
}

rematchBtn.onclick = async () => {
    rematchBtn.disabled = true;
    rematchBtn.textContent = "Warte auf Gegner …";
    try {
        await connection.invoke("RequestRematch", roomCode);
    } catch {
        showMessage("Revanche konnte nicht angefragt werden", "message-fail");
        showRematchButton();
    }
};

copyLinkBtn.onclick = async () => {
    try {
        await navigator.clipboard.writeText(location.href);
        showMessage("Link kopiert", "message-success");
    } catch {
        showMessage("Kopieren nicht möglich, bitte Link aus der Adresszeile nehmen", "message-fail");
    }
};

async function leaveRoom() {
    stopSearchTimer();
    searching = false;
    if (!connection) return;
    await connection.stop();
    connection = null;
    joinBtn.disabled = false;
    searchBtn.disabled = false;
    sessionStorage.removeItem("roomCode");
    sessionStorage.removeItem("randomRoom");
}

async function connect() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamehub")
        .withAutomaticReconnect()
        .build();

    connection.on("PlayersUpdated", (a, b) => {
        showPlayer(slots[0], a);
        showPlayer(slots[1], b);
    });

    connection.on("GameReady", (maxRows, wordLen) => {
        if (currentRow === 0 && !gameOver) showStartOverlay();
    });

    connection.on("InvalidGuess", () => {
        showMessage("Ungültiges Wort", "message-fail");
        shakeRow(currentRow);
    });
    connection.on("NoGuessesLeft", () => showMessage("Keine Versuche mehr", "message-fail"));
    connection.on("RoomFull", () => showMessage("Raum ist schon voll!", "message-fail"));

    connection.on("PlayerLeft", (name) => {
        showMessage(name + " hat die Verbindung verloren", "message-fail");
    });

    connection.on("ChatMessage", (senderId, name, text, avatar) => {
        appendChatMessage(senderId, name, text, avatar);
        if (chatOpen || senderId === playerId) return;
        unreadCount++;
        updateBadge();
        showPreview(name, text);
    });

    connection.on("RestoreState", restoreState);

    connection.on("RematchRequested", (name) => {
        showMessage(name + " will eine Revanche!", "message-success");
        appendSystemMessage(name + " will eine Revanche");
        rematchBtn.classList.add("rematch-requested");
    });

    connection.on("RematchUnavailable", () => {
        showMessage("Dein Gegner ist nicht mehr da", "message-fail");
        rematchBtn.textContent = "Revanche";
    });

    connection.on("Matched", (code) => {
        if (document.hidden) document.title = "Gegner gefunden!";
        joinRoom(code, true).catch(() => showMessage("Verbindung fehlgeschlagen", "message-fail"));
    });

    connection.onreconnected(async () => {
        if (searching) {
            await connection.invoke("FindOpponent", playerId);
        } else {
            await connection.invoke("CreateOrJoin", roomCode, playerId, myName);
        }
    });

    connection.on("GuessAccepted", (res) => {
        setGuessCount(playerId, currentRow + 1);
        revealDone = applyResult(currentRow, res);
        if (!res.isWin) {
            currentRow++; currentCol = 0;
        } else {
            gameOver = true;
        }
    });

    connection.on("OpponentGuessed", (name, count, id) => {
        setGuessCount(id, count);
        appendSystemMessage(name + " hat Versuch " + count + "/6 abgegeben");
    });

    connection.on("GameOver", async (data) => {
        gameOver = true;
        await revealDone;
        const { winnerName, winnerId, winnerGuesses, points, byWin, targetWord, restored } = data;

        let message = "";

        if (byWin) {
            for (const slot of slots) {
                slot.box.classList.add(slot.id === winnerId ? "winner" : "loser");
            }

            if (winnerId === playerId) {
                message = `Gewonnen im ${winnerGuesses}. Versuch! +${points} Punkte`;
                showOverlay("win", "Gewonnen!", `im ${winnerGuesses}. Versuch`);
                showPoints(points);
                if (!restored) addPoints(points);
                board.classList.add("win");
            } else {
                message = `Verloren! ${winnerName} hat es im ${winnerGuesses}. Versuch geschafft. Das Wort war: ${targetWord?.toUpperCase()}`;
                showOverlay("loss", "Verloren", `${winnerName} hat es im ${winnerGuesses}. Versuch geschafft`);
                showSolution(targetWord);
                board.classList.add("fail");
            }
        } else {
            message = `Alle Versuche verbraucht! Das Lösungswort war: ${targetWord?.toUpperCase()}`;
            showOverlay("draw", "Keiner hat's geschafft", "Alle Versuche verbraucht");
            showSolution(targetWord);
            board.classList.add("fail");
        }

        appendSystemMessage(message);
        showRematchButton();
        newOpponentBtn.hidden = !isRandomRoom;
    });

    await connection.start();
}

async function joinRoom(code, random) {
    roomCode = code;
    const joinedId = await connection.invoke("CreateOrJoin", roomCode, playerId, myName);
    if (!joinedId) {
        await leaveRoom();
        return;
    }

    playerId = joinedId;
    isRandomRoom = random;
    searching = false;
    stopSearchTimer();
    searchInfo.hidden = true;
    topSection.hidden = true;
    duel.hidden = false;
    chatOpenBtn.hidden = false;
    roomInfo.hidden = false;
    sessionStorage.setItem("roomCode", roomCode);
    sessionStorage.setItem("playerName", myName);
    sessionStorage.setItem("identity", currentIdentity);

    if (random) {
        sessionStorage.setItem("randomRoom", "1");
        roomLabel.textContent = "Zufallsspiel";
        copyLinkBtn.hidden = true;
        history.replaceState(null, "", location.pathname);
    } else {
        sessionStorage.removeItem("randomRoom");
        roomLabel.textContent = "Raum " + roomCode;
        copyLinkBtn.hidden = false;
        history.replaceState(null, "", "?room=" + encodeURIComponent(roomCode));
    }
}

async function startRoom(code, random) {
    joinBtn.disabled = true;
    try {
        await connect();
        await joinRoom(code, random);
    } catch {
        showMessage("Verbindung fehlgeschlagen", "message-fail");
        await leaveRoom();
    }
}

joinBtn.onclick = async () => {
    if (connection) return;
    const code = roomInput.value.trim().toUpperCase();
    myName = meInput.value.trim() || "Spieler";

    if (!code) { showMessage("Bitte Session-Code angeben", "message-fail"); return; }

    await startRoom(code, false);
};

function startSearchTimer() {
    const startedAt = Date.now();
    searchTime.textContent = "0:00";
    searchTimer = setInterval(() => {
        const seconds = Math.floor((Date.now() - startedAt) / 1000);
        const restSeconds = String(seconds % 60).padStart(2, "0");
        searchTime.textContent = `${Math.floor(seconds / 60)}:${restSeconds}`;
    }, 1000);
}

function stopSearchTimer() {
    clearInterval(searchTimer);
    searchTimer = null;
}

async function cancelSearch() {
    await leaveRoom();
    searchInfo.hidden = true;
    topSection.hidden = false;
}

async function beginSearch() {
    searchBtn.disabled = true;
    searching = true;
    topSection.hidden = true;
    roomInfo.hidden = true;
    searchInfo.hidden = false;
    showMessage("");
    startSearchTimer();
    try {
        await connect();
        await connection.invoke("FindOpponent", playerId);
    } catch {
        showMessage("Verbindung fehlgeschlagen", "message-fail");
        await cancelSearch();
    }
}

searchBtn.onclick = async () => {
    if (connection) return;
    myName = meInput.value.trim() || "Spieler";
    await beginSearch();
};

document.getElementById('search-cancel').onclick = cancelSearch;

function resetGameView() {
    if (chatOpen) closeChat();
    restoreState([]);
    showPlayer(slots[0], null);
    showPlayer(slots[1], null);
    duel.hidden = true;
    chatMessages.innerHTML = "";
    chatOpenBtn.hidden = true;
    roomInfo.hidden = true;
    showMessage("");
}

newOpponentBtn.onclick = async () => {
    await leaveRoom();
    resetGameView();
    await beginSearch();
};

leaveRoomBtn.onclick = async () => {
    await leaveRoom();
    resetGameView();
    topSection.hidden = false;
    history.replaceState(null, "", location.pathname);
};

document.addEventListener('visibilitychange', () => {
    if (!document.hidden) document.title = pageTitle;
});

document.addEventListener('keydown', (e) => {
    if (chatOpen) {
        if (e.key === "Escape") closeChat();
        return;
    }
    if (!connection || searching) return;
    if (e.target.tagName === "INPUT") return;
    const k = e.key;
    if (/^[a-zA-Z]$/.test(k)) { press(k.toUpperCase()); }
    else if (k === "Backspace") { backspace(); }
    else if (k === "Enter") { e.preventDefault(); submit(); }
});

const savedRoom = sessionStorage.getItem("roomCode");
const sameIdentity = sessionStorage.getItem("identity") === currentIdentity;
// reload-check - sonst url-check ( - sonst nix)
if (savedRoom && sameIdentity) {
    if (!meInput.readOnly) meInput.value = sessionStorage.getItem("playerName") || "";
    myName = meInput.value.trim() || "Spieler";
    startRoom(savedRoom, sessionStorage.getItem("randomRoom") === "1");
} else {
    sessionStorage.removeItem("roomCode");
    sessionStorage.removeItem("randomRoom");
    const linkRoom = new URLSearchParams(location.search).get("room");
    if (linkRoom) {
        roomInput.value = linkRoom;
        meInput.focus();
    }
}
