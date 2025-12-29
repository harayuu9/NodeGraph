import { dotnet } from './_framework/dotnet.js';

// ブラウザのデフォルトコンテキストメニューを抑制（Avaloniaアプリ用）
document.addEventListener('contextmenu', (e) => {
    // #out内（Avaloniaコンテナ）での右クリックのみ抑制
    const out = document.getElementById('out');
    if (out && out.contains(e.target)) {
        e.preventDefault();
    }
});

// localStorage の長さを取得
globalThis.getLocalStorageLength = () => {
    return localStorage.length;
};

// ファイルダウンロード機能
globalThis.downloadFile = (filename, content) => {
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// ファイルアップロード機能
globalThis.uploadFile = () => {
    return new Promise((resolve) => {
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.graph.yml,.yml,.yaml';

        input.onchange = async (e) => {
            const file = e.target.files?.[0];
            if (!file) {
                resolve('');
                return;
            }

            const content = await file.text();
            resolve(`${file.name}\n---CONTENT---\n${content}`);
        };

        input.oncancel = () => {
            resolve('');
        };

        input.click();
    });
};

// .NET ランタイムを起動 (.NET 9 WASM)
const runtime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

// ローディング画面を非表示にする
const hideLoading = () => {
    const loading = document.getElementById('loading');
    if (loading) {
        loading.classList.add('hidden');
        // フェードアウト後に完全に削除
        setTimeout(() => loading.remove(), 300);
    }
};

// Avaloniaが最初のフレームをレンダリングした後にローディングを非表示
// MutationObserverで#out内にコンテンツが追加されたことを検知
const observer = new MutationObserver((mutations, obs) => {
    const out = document.getElementById('out');
    if (out && out.children.length > 0) {
        hideLoading();
        obs.disconnect();
    }
});

observer.observe(document.getElementById('out'), {
    childList: true,
    subtree: true
});

// フォールバック: 5秒後に強制的に非表示
setTimeout(hideLoading, 5000);

await runtime.runMain();
