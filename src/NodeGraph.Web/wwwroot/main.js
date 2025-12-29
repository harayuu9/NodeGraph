import { dotnet } from './_framework/dotnet.js';

// .NET WebAssemblyランタイムを初期化
const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

// JavaScript関数をエクスポート
setModuleImports('main.js', {
    // ファイルダウンロード
    downloadFile: (filename, content) => {
        const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    // ファイルアップロード
    uploadFile: () => {
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
                // Format: "filename\n---CONTENT---\ncontent"
                resolve(`${file.name}\n---CONTENT---\n${content}`);
            };

            input.oncancel = () => {
                resolve('');
            };

            input.click();
        });
    },

    // localStorage.length を取得するラッパー
    'globalThis.localStorage.length': () => {
        return localStorage.length;
    }
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

await dotnet.run();
