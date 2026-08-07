# テスト

English: [Testing.md](Testing.md)

このpackageはUnity Test FrameworkのEditMode testだけを使用します。`TestProject~`はUnity 6、`TestProject2022~`は対応下限のUnity 2022.3 LTSを対象とします。どちらもrepository rootのpackageを参照し、testableとして指定しています。

Windowsでのcommand例:

```powershell
& '<Unity 6 path>\Editor\Unity.exe' -batchmode -nographics `
  -projectPath '<repository>\TestProject~' `
  -runTests -testPlatform EditMode -testResults '<output>\unity6.xml' -logFile '<output>\unity6.log'

& '<Unity 2022.3 path>\Editor\Unity.exe' -batchmode -nographics `
  -projectPath '<repository>\TestProject2022~' `
  -runTests -testPlatform EditMode -testResults '<output>\unity2022.xml' -logFile '<output>\unity2022.log'
```

対象範囲:

- 正規URI生成、percent decode、version、scheme、Project ID、parameter数、長さ制限
- traversal拒否と原子的なローカル書き込み
- heartbeatの経過時間とclock skewの制限
- 受信箱のTTL、size、重複、不正なUTF-8、削除、差し替え可能なclock/file system境界
- asset、sub-asset、Prefab child、保存済みかつロード済みのScene object、未保存Scene、未ロードScene、削除済み対象
- `GlobalObjectId`の厳密な解析と差し替え可能な選択境界
- WindowsのOS標準URI起動から起動中receiverを経由し、正確なUnity selectionへ到達する完全な往復処理

## OSプロトコルE2E

platform scriptは一意な一時schemeとProject IDを使用し、traversal requestが拒否されることを確認します。そのうえで実際のOS URL dispatch pathを実行し、配送されたrequestを検証し、`finally`/trapのcleanup処理で登録とファイルを削除します。

Windows:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\Platform\WindowsProtocolE2E.ps1
```

macOS:

```bash
/bin/bash Tests/Platform/macOSProtocolE2E.sh
```

package rootから実行してください。成功時は`E2E_PASS=True`と表示されます。

macOS handlerのURI検証、heartbeat、原子的な受信箱書き込みは、Launch Servicesなしでも確認できます。このportable testは、LinuxまたはGit Bashで実行する場合だけBSDの`stat`呼び出しを調整します。

```bash
/bin/bash Tests/Platform/macOSDispatchLogicTest.sh
```

macOS helperの構成、衝突しないbundle ID encode、AppleScript escape、plist操作、登録呼び出し、権限、status、uninstall、cleanupは、system commandをstub化して任意のBash環境で確認できます。

```bash
/bin/bash Tests/Platform/macOSInstallerLogicTest.sh
```

このinstaller logic testは、macOS上で実際の`osacompile`によりapplicationをcompileし、Launch Servicesへ登録してURIを開く実機確認の代替ではありません。

2026-08-01時点で、Windows 11上のUnity 2022.3.62f1とUnity 6000.3.20f1により、EditMode testは52/52件成功しました。Windows testでは、一時的な隔離schemeを使ったユーザー単位のinstall/status、他application登録の保護、実際のOS URI起動、受信箱への原子的配送、起動中Unity Editorでの選択、uninstall、cleanupも成功しました。macOSとthird-party link clientについては、[互換性](Compatibility-ja.md)へ記録するplatform固有の手動E2E確認が残っています。
