const accountOpenBtn = document.getElementById('account-open');
const accountPanel = document.getElementById('account-panel');

function openAccount() {
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
