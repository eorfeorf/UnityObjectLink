# プラットフォーム連携

English: [PlatformIntegration.md](PlatformIntegration.md)

## 共通の配置

```text
UnityObjectLink/
  bin/
  instances/<scheme>/<project-id>/
    heartbeat.json
    inbox/
      <unique-id>.request
```

rootはWindowsでは`%LOCALAPPDATA%`、macOSでは`~/Library/Application Support`です。handler scriptは、検証済みのURI値だけからすべてのpathを組み立てます。heartbeat payloadは情報表示用であり、scriptはそこに記載された受信箱pathを信用しません。

## Windows

`UnityObjectLinkProtocol.ps1 install`は自身を安定した`bin` pathへコピーし、小さなownership markerを記録して、`HKCU\Software\Classes\<scheme>`へ登録します。open commandはWindows PowerShellを`-NoProfile`付きで使用し、`%1`をscriptの`dispatch` commandだけへ渡します。`status`は登録がない、このUnity Object Linkが所有している、または別applicationが所有している、のいずれかを報告します。`uninstall`は別applicationの登録を削除せず、追跡対象schemeが残っていない場合だけ安定配置したscriptを削除します。

手動command:

```powershell
./UnityObjectLinkProtocol.ps1 -Command install -Scheme unity-object-link
./UnityObjectLinkProtocol.ps1 -Command status -Scheme unity-object-link
./UnityObjectLinkProtocol.ps1 -Command dispatch -Uri 'unity-object-link://select?...'
./UnityObjectLinkProtocol.ps1 -Command uninstall -Scheme unity-object-link
```

## macOS

`unity-object-link-protocol.sh install`は自身を安定した`bin` pathへコピーし、system標準の`osacompile`で最小限のAppleScript URL handlerをcompileし、`CFBundleURLTypes`を追加してLaunch Servicesへhelperを登録します。Xcodeやthird-party runtimeは必要ありません。

手動command:

```bash
./unity-object-link-protocol.sh install unity-object-link
./unity-object-link-protocol.sh status unity-object-link
./unity-object-link-protocol.sh dispatch '' 'unity-object-link://select?...'
./unity-object-link-protocol.sh uninstall unity-object-link
```

## 前面化の動作

解決後、assetではProject windowをfocusします。Scene objectではHierarchyへのfocusを試み、失敗時は最後のScene viewへfallbackします。`PingObject`と選択は確実に実行されますが、OSが別applicationの前面化を拒否する場合があります。そのため、foreground activationはbest-effortです。
