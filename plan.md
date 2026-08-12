# Unity Object Link 実装計画

## 目的

独自URIをクリックすると、起動済みの対象Unityプロジェクトで指定されたアセットまたはオブジェクトを選択・Pingし、可能な範囲でUnity Editorを前面化する。

個人で管理する単独のOSS向けUnity Packageとして開発し、特定企業、製品、業務リポジトリ、非公開ライブラリには依存させない。

初期スコープはWindows/macOS、起動済みUnityのみとする。通常アセット、サブアセット、Prefab内オブジェクト、ロード済みScene内オブジェクトを対象にし、未ロードSceneは自動で開かない。

## 個人プロジェクト化で削除した情報

- 特定企業、製品、案件、業務リポジトリの名称
- 非公開package、社内ライブラリ、社内ツールを前提とする記述
- 業務リポジトリ内のファイルパス、manifest、既存差分に関する記述
- 企業固有のpackage namespace、URI scheme、Project ID
- 社内法務、社内公開フロー、社内URLを前提とする記述
- 特定の業務プロジェクトへ組み込むタスク

## 技術的前提

- Unity Editorには、起動済みEditorへOSの独自URIを直接配送する公開APIがない。
- `Application.deepLinkActivated` はPlayer向けであり、Editor用にはOSプロトコルハンドラとローカル配送処理が必要。
- `GlobalObjectId` はアセット、サブアセット、Prefab内オブジェクト、Scene内オブジェクトを識別できる。
- Scene内オブジェクトは、Sceneが保存済みで、URI受信時にロード済みの場合のみ解決できる。
- 対応下限はUnity 2022.3 LTSとし、Unity 6でも同じ公開APIとURI形式を維持する。

## 決定事項

- Editor専用の独立UPMパッケージとして、単独Gitリポジトリのルートへ配置する。
- 仮package IDは `com.<owner>.unity-object-link` とし、所有者名が決まるまで実値をplanへ記載しない。
- namespace、assembly名、メニュー名、ログprefixは `UnityObjectLink` に統一する。
- 既定URI schemeは `unity-object-link` とし、Project Settingsで変更可能にする。
- URI形式はバージョン付きとし、例を `unity-object-link://select?v=1&project=sample-project&object=<URL encoded GlobalObjectId>` とする。
- 端末の絶対パスではなく、利用プロジェクトごとに設定する安定したProject IDで配送先を識別する。
- OSハンドラからUnityへの配送には、プロジェクト別heartbeatと一時ファイル受信箱を使う。
- OS登録はUnityメニューと手動スクリプトの両方を提供し、ユーザー単位・管理者権限不要とする。
- 未ロードSceneは開かず、通知して終了する。
- README、CHANGELOG、Documentationは日本語版と英語版を用意し、Tests、Samplesとともに公開リポジトリへそのまま配置できる構成にする。
- リポジトリ公開、package配布、正式なpackage ID、repository URL、ライセンス選定は実装と分離した公開前作業とする。作者は`eorfeorf`とする。

## 設計

### 1. リポジトリとパッケージ境界

- package rootに `package.json` を置き、Git URLから直接導入できる構成にする。
- Runtime assemblyを持たないEditor専用パッケージとし、`UnityObjectLink.Editor` と `UnityObjectLink.Editor.Tests` のasmdefを用意する。
- Unity標準APIと.NET Standardで利用可能なAPIだけを使い、private packageやproprietary dependencyを追加しない。
- 公開APIはURI生成、URI解析、設定参照、処理結果の通知境界に絞る。
- OSスクリプト、受信箱、heartbeat、ファイル配置の詳細はinternalに閉じる。
- package metadataの作者は`eorfeorf`とする。repository URLとlicenseは、公開前に個人プロジェクトの正式情報へ置き換える。

### 2. Project Settings

- 利用側Unityプロジェクトの `ProjectSettings/UnityObjectLinkSettings.asset` にschemeとProject IDを保存する。
- Project Settings画面でscheme、Project ID、プロトコル登録状態、heartbeat、受信状態を確認できるようにする。
- Project IDは空文字、予約文字、パストラバーサルに利用できる文字を拒否する。
- schemeはRFC 3986のscheme構文へ制限する。
- scheme変更時は旧schemeの登録状態を表示し、再登録または解除を促す。
- 初回導入時にProject IDを自動生成する場合でも、共有前に人が識別可能な値へ変更できるようにする。

### 3. URI生成

- `GlobalObjectId.GetGlobalObjectIdSlow` で選択対象を識別し、version、Project ID、GlobalObjectIdをURIへ格納する。
- `Assets/Copy Unity Object Link`、`GameObject/Copy Unity Object Link`、`Tools/Unity Object Link/Copy Link for Active Selection` を用意する。
- 対象 `UnityEngine.Object` からURIを生成する公開Editor APIを提供する。
- 未保存Scene内オブジェクト、永続化されていない一時オブジェクト、null選択、デフォルトGlobalObjectIdはコピー不可として理由を通知する。
- 初期版はアクティブな単一選択を対象とし、複数選択の一括リンク生成は対象外とする。

### 4. Unity側の受信と選択

- `[InitializeOnLoad]` のEditorサービスがプロジェクト別heartbeatを定期更新し、受信箱を監視する。
- OSハンドラは一意なリクエストファイルへURIを原子的に書き込み、Unity側は処理後に削除する。
- 古いheartbeatには送信せず、古いリクエスト、巨大入力、重複、壊れたファイルは拒否する。
- URIのscheme、action、version、Project ID、パラメータ数、入力長を厳密に検証する。
- URIから任意コマンド、任意プロセス、任意ファイルパスを実行できない設計にする。
- `GlobalObjectId.TryParse` と `GlobalObjectId.GlobalObjectIdentifierToObjectSlow` で対象を解決する。
- 成功時は `Selection.activeObject` と `EditorGUIUtility.PingObject` を実行する。
- 公開Editor APIの範囲でProject/Hierarchy表示をフォーカスし、Unity Editorをベストエフォートで前面化する。
- Sceneオブジェクトが解決できない場合は「Scene未ロードまたは対象消失」と通知し、Sceneを自動で開かない。

### 5. Windowsプロトコルハンドラ

- 設定されたschemeを `HKCU\Software\Classes\<scheme>` に登録するPowerShellインストーラを用意する。
- install、uninstall、status、dispatchを手動実行できるようにする。
- Unityメニューからも同じ処理を呼び出せるようにする。
- 実行スクリプトは `%LOCALAPPDATA%\UnityObjectLink` 配下へコピーし、package配置変更後も登録が壊れないようにする。
- ハンドラはURI検証、heartbeat確認、原子的な受信箱書き込みだけを行う。
- 管理者権限、追加PowerShell module、外部runtimeを要求しない。

### 6. macOSプロトコルハンドラ

- `CFBundleURLTypes` で設定されたschemeを登録する最小ヘルパー `.app` を生成する。
- macOS標準の `osacompile` とAppleScriptを使い、Xcodeや追加runtimeを必須にしない。
- `open location` で受けたURIを共通の受信箱へ渡す。
- install、uninstall、status、Launch Services登録を手動スクリプトとUnityメニューの双方から実行可能にする。
- ヘルパーは `~/Library/Application Support/UnityObjectLink` 配下へ配置する。
- Windows版と同じURI検証、heartbeat、TTL、原子的書き込みのルールを適用する。

### 7. テスト

- URI生成・解析、URLエンコード、version、Project ID不一致、欠落・重複パラメータ、入力長制限をEditModeテストする。
- 一時アセット、サブアセット、Prefab内オブジェクト、保存済みSceneオブジェクトでGlobalObjectIdの往復解決をテストする。
- 未保存Scene、未ロードScene、削除済み対象、壊れたGlobalObjectIdが安全に失敗することをテストする。
- 受信箱、heartbeat、TTL、重複処理、原子的ファイル移動を、時計・ファイルシステム・選択処理を差し替え可能な境界でテストする。
- テストはUnity Test Frameworkだけで実行可能にする。
- Unity 2022.3 LTSとUnity 6でコンパイル・EditModeテストを実行する。
- Windowsでは登録、状態確認、URI起動、解除を実機確認する。
- macOSではヘルパー生成、Launch Services登録、URI起動、解除を実機確認する。
- ブラウザ、Slack Desktop/Web、Jira、Confluenceについてリンク化可否を互換性表へ記録する。

### 8. ドキュメントとSamples

- `README.md`と`README-ja.md`に、セットアップ、アンインストール、URI形式、コピー方法、対応対象、Scene制約、セキュリティ方針を英語・日本語で記載する。
- `Documentation~/`に、アーキテクチャ、URI仕様、公開API、セキュリティモデル、プラットフォーム別実装の英語版と日本語版を記載する。
- `Samples~/Basic Usage` に、公開APIからURIを生成する最小Editor拡張例を置く。
- `CHANGELOG.md`と`CHANGELOG-ja.md`をKeep a Changelog形式で開始する。
- package内に個人情報、ローカル絶対パス、private URL、秘密情報、不要なバイナリが含まれないことを確認する。
- 独自schemeをリンク化しないクライアントがあることと、必要なら将来HTTPS中継を検討することを明記する。

## 想定リポジトリ構成

- `package.json`
- `Editor/`
  - `Public/`
  - `Internal/`
  - `Platform/Windows/`
  - `Platform/macOS/`
- `Tests/Editor/`
- `Samples~/Basic Usage/`
- `Documentation~/`
- `README.md`
- `CHANGELOG.md`
- `LICENSE.md`（ライセンス決定後）
- `.github/`（公開・CI方針決定後）

## Todos

1. 単独GitリポジトリとしてUPM package、asmdef、namespace、公開API境界を構成する。
2. Project Settings、URIモデル、schemeとProject IDの検証を実装する。
3. GlobalObjectIdベースのURI生成APIとコピー用メニューを実装する。
4. heartbeat、受信箱、URI検証、オブジェクト解決、選択・Ping・前面化を実装する。
5. Windowsの登録・解除・状態確認・URI配送スクリプトとUnityメニュー連携を実装する。
6. macOSのヘルパー生成・登録・解除・状態確認・URI配送とUnityメニュー連携を実装する。
7. URI、GlobalObjectId、受信箱、失敗ケースを自己完結したEditModeテストで網羅する。
8. README、CHANGELOG、Documentationの英語版と日本語版、Samplesを追加し、公開前の情報漏えいチェック項目を整備する。
9. Unity 2022.3/Unity 6、Windows/macOS、各リンク貼り付け先でE2E確認し、互換性表を完成させる。

## 注意点

- 「全てのオブジェクト」はUnityが永続IDを作れる範囲に限られる。
- 未保存Scene、一時生成オブジェクト、Play Modeだけに存在するオブジェクトは共有リンク化できない。
- macOSはURLハンドラとしてアプリバンドルが必要なため、Windowsよりセットアップ実装が多い。
- カスタムURIのクリック可否はブラウザやチャットツールのセキュリティ設定に左右される。
- 作者表記は`eorfeorf`、package IDは`com.eorfeorf.unity-object-link`とする。repository URL、licenseは公開前に確定する。
- 実装・テスト・ドキュメントには、業務リポジトリ由来のコードや非公開情報を持ち込まない。
