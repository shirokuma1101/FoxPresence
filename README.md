# FoxPresence

Firefoxで再生しているYouTube、YouTube Music、dアニメストア、U-NEXT、NetflixをDiscord DesktopのRich Presenceへ表示するWindows用アプリです。Bot、Discordユーザートークン、ブラウザCookieは使用しません。

> Version 0.4.0以降を推奨します。0.1.0のインストーラーはWindows PowerShell 5.1に対応していません。

## Installation

以下を上から順番に実行してください。管理者権限は不要です。

### 1. 必要なアプリを用意する

- Windows 10またはWindows 11（x64）
- 最新版Firefox
- Discord Desktop

Discord Web版だけではPresenceを表示できません。Release ZIPには.NET 10 Runtimeが含まれるため、.NETを別途インストールする必要はありません。

### 2. Discord Applicationを作成する

1. [Discord Developer Portal](https://discord.com/developers/applications)を開き、Discordへログインします。
2. 右上の「New Application」を押します。
3. 名前に`FoxPresence`または好みの名前を入力し、Applicationを作成します。この名前がDiscord上のアクティビティ名になります。
4. 左側の「General Information」を開き、「APPLICATION ID」の「Copy」を押します。後の手順で使うため、数字を保存しておきます。
5. Botは作成しません。Bot Token、Client Secret、OAuth認証も不要です。

サムネイルURLをDiscordが利用できない場合に備えて固定画像を登録する場合は、左側の「Rich Presence」→「Art Assets」で画像を追加し、それぞれ次の名前を付けます。この手順は任意です。
登録した画像は、作品サムネイルの右下に表示されるサービスバッジにも使用されます。

- `youtube`
- `youtube_music`
- `d_anime`
- `unext`
- `netflix`

### 3. FoxPresenceをダウンロードする

1. [最新のGitHub Release](https://github.com/shirokuma1101/FoxPresence/releases/latest)を開きます。
2. 「Assets」から`FoxPresence-win-x64.zip`をダウンロードします。
3. ZIPを右クリックして「すべて展開」を選び、書き込み可能な固定場所へ展開します。例: `C:\Users\<ユーザー名>\Apps\FoxPresence`
4. インストール後にこのフォルダを移動・削除しないでください。FirefoxのNative Messaging登録がBridgeの絶対パスを参照します。

### 4. セットアップスクリプトを実行する

展開した`FoxPresence`フォルダをエクスプローラーで開き、アドレスバーへ`powershell`と入力してEnterを押します。開いたPowerShellで次を実行します。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

`Enter the Discord Application ID`と表示されたら、手順2でコピーしたApplication IDを貼り付けてEnterを押します。

このスクリプトは次を自動的に行います。

1. `%APPDATA%\FirefoxDiscordPresence\settings.json`へApplication IDを保存
2. Firefox Native Messaging Hostを現在のWindowsユーザーへ登録
3. Firefox拡張機能を`firefox-extension`フォルダへ展開
4. FoxPresence Tray Applicationを起動

成功すると`FoxPresence setup completed.`と表示され、Windowsの通知領域にFoxPresenceのアイコンが現れます。

Application IDをコマンドへ直接渡すこともできます。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1 -DiscordApplicationId "123456789012345678"
```

### 5. Firefox拡張機能を読み込む

現在のReleaseに含まれる拡張機能はMozilla未署名の開発版です。通常版Firefoxでは一時的なアドオンとして読み込みます。

1. Firefoxのアドレスバーへ`about:debugging#/runtime/this-firefox`と入力します。
2. 「一時的なアドオンを読み込む」を押します。
3. FoxPresenceを展開したフォルダにある`firefox-extension\manifest.json`を選択します。
4. 一覧に「Firefox Discord Presence」が表示されたことを確認します。

一時的なアドオンはFirefoxを終了すると解除されます。Firefoxを再起動した場合は、この手順だけ再実行してください。通常版Firefoxへ恒久インストールするにはMozillaによる署名が必要です。

### 6. Presenceを確認する

1. Discord Desktopを起動し、ログインします。
2. FirefoxでYouTube、YouTube Music、dアニメストア、U-NEXT、またはNetflixを開いて再生します。
3. 数秒待ち、自分のDiscordプロフィールにタイトルと再生時間が表示されることを確認します。
4. 一時停止するとPresenceは消え、再生を再開すると再表示されます。

YouTube Musicは`Listening`、その他は`Watching`として表示されます。Presenceには作品サムネイル、サービスバッジ、タイトル、作者またはシリーズ名、再生位置、視聴ページへのリンクが含まれます。

タスクトレイのFoxPresenceアイコンを右クリックすると、状態の確認、Presenceの無効化、Windows起動時の自動起動、終了ができます。

### 7. Windows起動時に自動起動する（任意）

タスクトレイのFoxPresenceアイコンを右クリックし、「Start with Windows」をチェックします。ユーザー単位の設定なので管理者権限は不要です。

注意: Tray Applicationは自動起動できますが、Mozilla署名前のFirefox拡張機能はFirefox再起動後に手順5の再読み込みが必要です。

## Updates

### 手動アップデート

FoxPresenceの展開先でPowerShellを開き、次を実行します。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update.ps1
```

最新版があれば、GitHub ReleaseからダウンロードしてSHA-256を検証し、Tray終了、ファイル更新、Native Host再登録、Tray再起動まで自動実行します。最新版ならファイルは変更しません。更新後はFirefoxの`about:debugging`で一時アドオンの「再読み込み」を押してください。

Version 0.3.1以前から更新する場合は、旧UpdaterがBridgeを終了できないため、最初の1回だけFirefoxを終了してから実行してください。Version 0.3.2以降のUpdaterは、更新中にNative HostとBridgeを安全に停止するため、Firefoxを終了する必要はありません。

### 自動アップデートを有効にする

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\enable-auto-update.ps1
```

Windowsへサインインしたとき、バックグラウンドでGitHub Releasesの最新版を確認し、更新があれば自動適用します。初期状態ではOFFです。無効にするには次を実行します。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\disable-auto-update.ps1
```

自動更新後も、Mozilla未署名の一時アドオンはFirefox側で再読み込みまたはFirefox再起動後の再登録が必要です。更新処理はGitHub APIが返すRelease assetのSHA-256 digestとダウンロードファイルを照合し、一致しないファイルは適用しません。

### アンインストール

展開したFoxPresenceフォルダをエクスプローラーで開き、アドレスバーへ`powershell`と入力してEnterを押します。開いたPowerShellで次を実行します。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1
```

このスクリプトは次を自動的に行います。

1. 実行中のFoxPresence Tray Applicationを終了
2. Firefox Native Messaging Hostの登録とmanifestを削除
3. Windows自動起動と自動更新の登録を削除
4. `%APPDATA%\FirefoxDiscordPresence`の設定を削除
5. `%LOCALAPPDATA%\FirefoxDiscordPresence`のログを削除

続いてFirefoxの`about:debugging#/runtime/this-firefox`を開き、「Firefox Discord Presence」の「削除」を押します。一時アドオンなのでFirefoxを再起動するだけでも解除されます。最後に、展開したFoxPresenceフォルダをエクスプローラーから削除してください。

再インストールに備えて設定またはログを残す場合は、次のオプションを使用できます。

```powershell
# 設定を残す
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1 -KeepUserData

# ログを残す
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1 -KeepLogs

# 設定とログを両方残す
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall.ps1 -KeepUserData -KeepLogs
```

## Troubleshooting

- `Discord disconnected`: Discord Desktopが起動しているか、`%APPDATA%\FirefoxDiscordPresence\settings.json`の`discordApplicationId`が正しいか確認し、Trayを再起動します。
- `waiting for Firefox`: 手順5で拡張機能が読み込まれているか確認します。FoxPresenceフォルダを移動した場合は手順4を再実行します。
- タイトルが表示されない: 対象ページを再読み込みしてから再生します。ログは`%LOCALAPPDATA%\FirefoxDiscordPresence\logs\yyyy-MM-dd.log`にあります。
- サムネイルだけ表示されない: Presence本体には影響しません。Discord Developer Portalへ手順2の固定Art Assetを追加してください。
- ボタンが自分から見えない: Discordの仕様上、Presenceボタンは本人ではなく他のユーザーから見た場合に表示されます。
- PowerShellがファイルをブロックする: ZIPのプロパティに「許可する」があればチェックして再展開するか、記載の`-ExecutionPolicy Bypass`付きコマンドを使います。

## Supported services

- YouTube
- YouTube Music
- dアニメストア
- U-NEXT
- Netflix

複数タブで同時に再生している場合、最後に再生を開始したタブがPresence対象になります。別タブの操作中やFirefox最小化中も追跡します。

## Architecture

```text
Firefox WebExtension
  -> Firefox Native Messaging
  -> C# Bridge
  -> Current-user-only Named Pipe
  -> C# WinForms Tray Application
  -> Discord Desktop RPC
```

再生状態はイベント駆動で取得し、30秒ごとのheartbeatを送ります。再生時間は毎秒送信せず、Discord timestampで進行させます。45秒以上heartbeatが届かなければstale状態としてPresenceを消去します。

Discord連携には`DiscordRichPresence` 1.6.1.70を使用しています。ユーザートークン認証をせず、起動中のDiscord DesktopとローカルRPCで通信します。

## Development

必要環境は.NET 10 SDK、Node.js、PowerShellです。

```powershell
dotnet restore FirefoxDiscordPresence.slnx
dotnet build FirefoxDiscordPresence.slnx
dotnet test FirefoxDiscordPresence.slnx
dotnet publish src/FirefoxDiscordPresence.Tray -c Release -r win-x64 --self-contained true -o artifacts/tray
dotnet publish src/FirefoxDiscordPresence.Bridge -c Release -r win-x64 --self-contained true -o artifacts/bridge
```

開発ビルドのNative Hostを登録する場合:

```powershell
.\scripts\install-native-host.ps1 -BridgePath ".\artifacts\bridge\FirefoxDiscordPresence.Bridge.exe"
```

## GitHub Actions and releases

`.github/workflows/build.yml`は`main`へのpush、Pull Request、`v*`タグ、手動実行で、Releaseビルド、テスト、JavaScript・manifest・PowerShell検証、win-x64パッケージ作成を行います。

`v*`タグをpushすると、ビルド済み`FoxPresence-win-x64.zip`を添付したGitHub Releaseも自動作成します。

## Security and privacy

拡張機能のhost permissionは対応するYouTube、YouTube Music、dアニメストア、U-NEXT、Netflixの公式ドメインだけです。Discordユーザートークン、Bot Token、ブラウザCookie、各サービスの認証情報は取得・保存しません。Native Pipeは同じWindowsユーザーからのみ接続できます。
