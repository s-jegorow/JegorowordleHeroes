const accountOpenBtn = document.getElementById('account-open');
const accountPanel = document.getElementById('account-panel');
const accountScore = document.getElementById('account-score');
const isGuest = accountScore.dataset.guest === "true";
const GUEST_SCORE_KEY = "guestScore";
const ranking = document.getElementById('ranking');

// lädt beim öffnen der leiste, könnte später per hub gepusht werden, damit es immer live ist
async function loadRanking() {
    try {
        const response = await fetch("/Ranking");
        if (!response.ok) throw new Error();
        ranking.innerHTML = await response.text();
    } catch {
        ranking.textContent = "Rangliste konnte nicht geladen werden.";
    }
}

function getGuestScore() {
    return Number(localStorage.getItem(GUEST_SCORE_KEY)) || 0;
}

function addPoints(points) {
    if (isGuest) {
        localStorage.setItem(GUEST_SCORE_KEY, getGuestScore() + points);
    }
    accountScore.textContent = Number(accountScore.textContent) + points;
    if (accountPanel.classList.contains('open')) loadRanking();
}

if (isGuest) {
    accountScore.textContent = getGuestScore();
}

function openAccount() {
    loadRanking();
    accountPanel.classList.add('open');
    accountOpenBtn.setAttribute('aria-expanded', 'true');
}

function closeAccount() {
    accountPanel.classList.remove('open');
    accountOpenBtn.setAttribute('aria-expanded', 'false');
    accountOpenBtn.focus();
}

accountOpenBtn.onclick = openAccount;
document.getElementById('account-close').onclick = closeAccount;

document.addEventListener('keydown', (e) => {
    if (e.key === "Escape" && accountPanel.classList.contains('open')) closeAccount();
});
