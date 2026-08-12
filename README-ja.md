# Unity Object Link

Unity Object Linkは、Unityのasset、sub-asset、Prefab内object、保存済みかつロード済みのScene内objectへの安定したリンクを生成します。リンクを開くと、同じProject IDを持つ起動中のUnity Editorで対象を選択してPingします。

このpackageはEditor専用で、Unity 2022.3 LTS以降に対応します。Windows/macOSのprotocol handlerは現在のユーザーaccount内だけで動作し、管理者権限や追加runtimeを必要としません。

> [!IMPORTANT]
> repository URL、licenseは公開前の仮情報です。このrepositoryを公開する前に置き換えてください。package IDは`com.eorfeorf.unity-object-link`、作者は`eorfeorf`です。

## 導入

このrepositoryはrootがUPM packageです。ローカル開発では **Window > Package Management > Package Manager > + > Add package from disk** を選び、`package.json`を指定します。公開後は **Add package from git URL** でrepository URLを指定します。

続いて次の操作を行います。

1. **Edit > Project Settings > Unity Object Link** を開きます。
2. 安定した共有用Project IDを設定します。自動生成値はローカル利用には安全ですが、リンク共有前に識別しやすい値へ変更してください。
3. 既定の`unity-object-link` schemeを使用するか、組織専用のschemeを指定します。
4. **Apply Settings**、続いて **Register** を押します。

登録はOSユーザー単位です。リンクを開く各workstationで同じschemeを1回ずつ登録してください。リンクを受け取れるのは起動中のUnity Editorだけです。

## リンクのコピー

objectを1つ選択し、次のいずれかを実行します。

- **Assets > Copy Unity Object Link**
- **GameObject > Copy Unity Object Link**
- **Tools > Unity Object Link > Copy Link for Active Selection**

公開APIも利用できます。

```csharp
if (UnityObjectLink.UnityObjectLinkApi.TryCreateLink(target, out string uri, out string error))
{
    GUIUtility.systemCopyBuffer = uri;
}
```

version 1のリンクは次の形式です。

```text
unity-object-link://select?v=1&project=sample-project&object=<URL-encoded GlobalObjectId>
```

## 対応対象と制限

| 対象 | 対応 | 条件 |
| --- | --- | --- |
| Asset | 対応 | Asset Databaseへ保存済み |
| Sub-asset | 対応 | 永続的な`GlobalObjectId`を持つ |
| Prefab asset内object | 対応 | Prefabが保存済み |
| Scene object | 対応 | Sceneに未保存の変更がなく、リンクを開いた時点でロード済み |
| 未ロードScene内object | 自動ロードしない | objectが見つからないことを通知し、Scene構成は変更しない |
| 未保存Sceneまたは一時object | 非対応 | Unityが共有可能な永続IDを提供できない |
| Play Modeだけのobject | 非対応 | そのsession以外では同一性を維持できない |

リンクに絶対ファイルpathは含まれません。Project IDが配送先を決め、Unityの`GlobalObjectId`が対象を識別します。

## アンインストール

packageを削除する前に **Project Settings > Unity Object Link** を開き、**Unregister** を押します。Unityを利用できない場合は、同梱scriptを手動実行してください。

Windows PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Editor\Platform\Windows\UnityObjectLinkProtocol.ps1 -Command uninstall -Scheme unity-object-link
```

macOS:

```bash
/bin/bash Editor/Platform/macOS/unity-object-link-protocol.sh uninstall unity-object-link
```

## セキュリティとプライバシー

OS handlerは`select` actionと、size制限された正確に3つのparameterだけを受理します。schemeとProject IDは検証後にのみlocal pathへ使用します。requestからexecutable、command、file pathを指定することはできません。対象projectが起動中であることを新しいheartbeatで確認できた場合だけ、固定されたユーザー単位の受信箱へ一意なrequestを書き込みます。

完全な検証仕様は[セキュリティモデル](Documentation~/Security-ja.md)と[URI仕様](Documentation~/UriSpecification-ja.md)を参照してください。

## クライアント互換性

独自URI linkの動作はapplicationとそのsecurity policyに依存します。独自schemeをplain textとして表示するclientや、確認promptを求めるclientもあります。[互換性](Documentation~/Compatibility-ja.md)と[手動チェックリスト](Documentation~/ClientCompatibilityChecklist-ja.md)を参照してください。HTTPS redirect serviceは意図的にversion 1の対象外としており、将来検討できます。

## 開発

`DevelopmentProject~`はUnity 6用の対話的な開発projectです。`TestProject~`と`TestProject2022~`はUnity 6およびUnity 2022.3用の最小command-line検証projectです。packageのEditMode testは、URI検証、storage、受信箱動作、`GlobalObjectId`の往復変換を対象とします。[テスト](Documentation~/Testing-ja.md)、[アーキテクチャ](Documentation~/Architecture-ja.md)、[プラットフォーム連携](Documentation~/PlatformIntegration-ja.md)を参照してください。

English documentation: [README.md](README.md)  
HTML版: [Documentation~/README-ja.html](Documentation~/README-ja.html)
