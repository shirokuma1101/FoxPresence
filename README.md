# Firefox Discord Presence

Firefox上のYouTube / YouTube Music / dアニメストアの再生情報を、Windowsのタスクトレイアプリ経由でDiscord DesktopのRich Presenceへ表示します。Bot、Discordユーザートークン、ブラウザCookieは使用しません。

## 構成

`extension`（イベント駆動のWebExtension）→ Firefox Native Messaging → `Bridge`（短命なstdioホスト）→ 同一ユーザー限定Named Pipe → `Tray`（WinForms / Discord RPC）という構成です。複数タブはbackground scriptが保持し、最後に再生開始した再生中タブを選びます。再生位置は毎秒送らず、Discord timestampで進行させます。

## 必要環境

- Windows 10 / 11、Firefox、Discord Desktop
- ビルドには .NET 10 SDK
- Discord Developer Portalで作成したApplication ID

## Discord Applicationの準備

1. [Discord Developer Portal](https://discord.com/developers/applications)でApplicationを1つ作成します（例: `Firefox Media`）。
2. Rich Presence Assetsに任意で `youtube`、`youtube_music`、`d_anime` を登録します。外部サムネイルがDiscord側で拒否された場合のfallbackです。
3. Trayを一度起動すると `%APPDATA%\FirefoxDiscordPresence\settings.json` が生成されます。終了後、`discordApplicationId` にApplication IDを設定して再起動します。

Discord連携には `DiscordRichPresence` 1.6.1.70を採用しました。管理されたNamed Pipe実装、自動再接続、同一Presence抑制、dispose時のclearを備え、2025年8月に更新された.NET 9対応パッケージで.NET 10からも利用可能なためです。Discord公式のSocial SDKは直接RPCを正式サポートしますが、現行のネイティブC++ SDKをC# WinFormsへ組み込むより、この用途では配布と保守が簡潔です。

## ビルドとpublish

```powershell
dotnet build
dotnet test
dotnet publish src/FirefoxDiscordPresence.Tray -c Release -r win-x64 --self-contained false -o artifacts/tray
dotnet publish src/FirefoxDiscordPresence.Bridge -c Release -r win-x64 --self-contained false -o artifacts/bridge
```

## GitHub Actions

`.github/workflows/build.yml` は、`main`へのpush、`main`宛てPull Request、`v*`タグ、手動実行で動作します。Windows runnerで.NET 10のrestore・Releaseビルド・テスト、拡張機能とPowerShellの構文検証、win-x64向けpublishを実行します。

成功するとActionsの実行画面から次の成果物をダウンロードできます。

- `FoxPresence-win-x64-<commit>`: Tray、Bridge、Firefox拡張機能ZIP、登録スクリプト、README
- `test-results`: Visual Studio Test Result形式のテスト結果

GitHubへ初めて登録する例:

```powershell
git init
git add .
git commit -m "Initial implementation"
git branch -M main
git remote add origin https://github.com/<owner>/<repository>.git
git push -u origin main
```

`<owner>/<repository>`は作成したGitHubリポジトリへ置き換えてください。`bin`、`obj`、`artifacts`、IDEユーザー設定は`.gitignore`によりcommitされません。

## Native Messaging Hostの登録

Bridgeをpublishした後、通常ユーザーのPowerShellで実行します。HKCUのみを使うため管理者権限は不要です。

```powershell
.\scripts\install-native-host.ps1
```

別の出力先なら `-BridgePath 'C:\absolute\path\FirefoxDiscordPresence.Bridge.exe'` を渡します。解除は `.\scripts\uninstall-native-host.ps1` です。登録時に `%APPDATA%\FirefoxDiscordPresence\native-host` へFirefox用manifestが生成されます。

## Firefox Extensionのインストール

Firefoxで `about:debugging#/runtime/this-firefox` を開き、「一時的なアドオンを読み込む」から `extension/manifest.json` を選びます。恒久配布時は同じ固定ID `firefox-discord-presence@shiro1103.local` で署名してください。

## 起動

`artifacts/tray/FirefoxDiscordPresence.Tray.exe` を起動します。タスクトレイメニューでPresenceとWindows自動起動を切り替えられます。pause / ended時はPresenceをclearします。Firefoxから45秒以上heartbeatが届かなければstaleとしてclearします。

## トラブルシューティング

- `Discord disconnected`: Discord Desktopが起動済みか、Application IDが正しいか確認します。起動後の再接続はライブラリが行い、最新状態を復元します。
- `waiting for Firefox`: Native Hostを再登録し、Firefoxの拡張機能を再読み込みします。Bridgeを移動した場合も再登録が必要です。
- 情報が空: 対象サイトのDOM変更が考えられます。selectorは `extension/content` 以下のサイト別ファイルに集約されています。
- ログ: `%LOCALAPPDATA%\FirefoxDiscordPresence\logs\yyyy-MM-dd.log`。Native Hostの診断はFirefox側のstderrログへ出ます。
- 外部サムネイルが出ない: Discord側の外部URL対応に依存します。Presence本体は継続し、登録済み固定Assetへ切り替えるには抽出側のURLを空にして確認できます。

## セキュリティ

拡張機能のhost permissionはYouTubeの2ドメインとdアニメストア公式ドメインだけです。Native Pipeは `CurrentUserOnly` で作成し、JSONサイズを1 MiBに制限します。Discord Application IDは秘密情報ではありません。ユーザートークンやCookieは取得・保存しません。
