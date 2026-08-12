# アーキテクチャ

English: [Architecture.md](Architecture.md)

Unity Object LinkはEditor専用のUPMパッケージです。Runtime assemblyは持たず、Unity 2022.3が提供する公開Editor APIと.NET profile以外には依存しません。

## 処理フロー

1. `UnityObjectLinkApi.TryCreateLink`がUnityから`GlobalObjectId`を取得し、version、scheme、Project IDと組み合わせます。
2. ユーザーがURIを開くと、OSが登録済みのプロトコルハンドラを起動します。
3. ハンドラが配送情報を厳密に検証し、対象プロジェクトのheartbeatを確認します。
4. URIを一意な一時ファイルへ書き込み、対象の受信箱内で`*.request`へ原子的にrenameします。
5. `UnityObjectLinkReceiverService`がEditorのupdate loopで受信箱をpollingします。
6. `UnityObjectLinkInboxProcessor`が古い、空、巨大、重複、または読めないrequestを拒否し、処理したファイルをすべて削除します。
7. `UnityObjectLinkResolver`がURI全体を解析して`GlobalObjectId`を解決し、対象を選択してProjectまたはHierarchy viewへfocusし、Pingします。

## 境界

- `Editor/Public`には、他のEditor assemblyから利用できる設定、URI、結果、リンクAPIがあります。
- `Editor/Internal`には、検証、ローカルストレージ、受信、選択、UI、メニューの実装があります。
- `Editor/Platform`には、OS登録bridgeとscriptがあります。
- `Tests/Editor`は、packageがtestableとして指定された場合だけcompileされます。

受信箱processorのファイルシステムと時刻はconstructor境界から差し替えられるため、実際のプロトコル登録なしで配送動作をテストできます。選択処理は`UnityObjectLinkApi.HandleLink`の背後にあり、公開される結果とeventがintegration向けの通知境界になります。

## オブジェクトの識別

このpackageは`GlobalObjectId.GetGlobalObjectIdSlow`が返す文字列をそのまま保存します。曖昧で端末依存になるため、pathやnameによるfallbackは作りません。`GlobalObjectId.GlobalObjectIdentifierToObjectSlow`でScene objectを解決できるのは、その保存済みSceneがロードされている場合だけです。このpackageが副作用としてSceneをロードすることはありません。
