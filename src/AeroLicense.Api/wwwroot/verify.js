"use strict";
const h = React.createElement;

const STATUS = {
  Valid:    { cls: "ok",   label: "Geçerli",            text: "Belge otorite kayıtlarıyla eşleşiyor ve lisans geçerli." },
  Expired:  { cls: "warn", label: "Süresi dolmuş",      text: "Belge gerçek, ancak lisansın geçerlilik süresi sona ermiş." },
  Revoked:  { cls: "bad",  label: "İptal edilmiş",      text: "Belge gerçek, ancak lisans otorite tarafından iptal edilmiş." },
  Tampered: { cls: "bad",  label: "Değiştirilmiş",      text: "Belge içeriği imzayla uyuşmuyor. Bu belgeye güvenmeyin." }
};

const fmt = iso => new Date(iso).toLocaleDateString("tr-TR", { year: "numeric", month: "long", day: "numeric", timeZone: "UTC" });

function codeFromPath() {
  const last = decodeURIComponent(location.pathname.split("/").filter(Boolean).pop() || "");
  return /^[A-Z0-9]{10,64}$/.test(last) ? last : "";
}

function Result({ state }) {
  if (state.kind === "idle") return null;
  if (state.kind === "loading") return h("p", null, "Doğrulanıyor…");
  if (state.kind === "notfound") return h("div", { className: "card" },
    h("span", { className: "badge bad" }, "Bulunamadı"),
    h("p", null, "Bu doğrulama koduna ait bir belge yok. Kodu kontrol edin."));
  if (state.kind === "error") return h("div", { className: "card" },
    h("span", { className: "badge warn" }, "Doğrulanamadı"),
    h("p", null, state.message));

  const r = state.result;
  const s = STATUS[r.status] || STATUS.Tampered;
  return h("div", { className: "card", role: "status" },
    h("span", { className: "badge " + s.cls }, s.label),
    h("p", null, s.text),
    // Tampered durumunda API içerik alanlarını zaten boş döner; sayfa da göstermez.
    r.licenseNumber && h("dl", null,
      h("dt", null, "Lisans no"), h("dd", null, r.licenseNumber),
      h("dt", null, "Tür"), h("dd", null, r.licenseType),
      h("dt", null, "Sahibi"), h("dd", null, r.holderNameMasked),
      h("dt", null, "Geçerlilik sonu"), h("dd", null, fmt(r.expiresAtUtc)),
      h("dt", null, "Sorgu zamanı"), h("dd", null, new Date(r.checkedAtUtc).toLocaleString("tr-TR"))));
}

function App() {
  const [code, setCode] = React.useState(codeFromPath());
  const [state, setState] = React.useState({ kind: "idle" });

  const verify = React.useCallback(async value => {
    if (!value) return;
    setState({ kind: "loading" });
    try {
      const res = await fetch("/api/v1/verify/" + encodeURIComponent(value), { headers: { Accept: "application/json" } });
      if (res.status === 404) return setState({ kind: "notfound" });
      if (res.status === 429) return setState({ kind: "error", message: "Çok fazla sorgu yapıldı. Biraz sonra tekrar deneyin." });
      if (!res.ok) return setState({ kind: "error", message: "Sunucu hatası. Daha sonra tekrar deneyin." });
      setState({ kind: "result", result: await res.json() });
    } catch {
      setState({ kind: "error", message: "Sunucuya ulaşılamadı." });
    }
  }, []);

  React.useEffect(() => { verify(codeFromPath()); }, [verify]);

  const submit = e => {
    e.preventDefault();
    const value = code.trim().toUpperCase();
    history.replaceState(null, "", "/dogrula/" + encodeURIComponent(value));
    verify(value);
  };

  return h(React.Fragment, null,
    h("h1", null, "Lisans Belgesi Doğrulama"),
    h("p", { className: "sub" }, "AeroLicense · Kurgusal Sivil Havacılık Otoritesi prototipi"),
    h(Result, { state }),
    h("form", { onSubmit: submit },
      h("input", { value: code, onChange: e => setCode(e.target.value), placeholder: "Doğrulama kodu",
                   "aria-label": "Doğrulama kodu", maxLength: 64, autoComplete: "off", spellCheck: false }),
      h("button", { type: "submit" }, "Doğrula")),
    h("p", { className: "foot" }, "Kişisel verilerin korunması için yalnızca doğrulama için gereken bilgiler maskelenerek gösterilir."));
}

ReactDOM.createRoot(document.getElementById("root")).render(h(App));
