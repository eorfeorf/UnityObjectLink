# クライアント・プラットフォーム互換性

English: [Compatibility.md](Compatibility.md)

独自URIの処理は、OSへの登録とリンクを掲載するapplicationの両方に依存します。次の表は現在の検証状況です。「手動確認」は、外部handlerの起動前にclientが確認を求める場合があることを示します。

| 環境 | リンク認識 | Handler起動 | 状況 |
| --- | --- | --- | --- |
| Windows 11「ファイル名を指定して実行」/ shell | OS標準の独自protocol | 対応 | 2026-08-01にinstall/status、拒否、他application登録の保護、OS起動、受信箱配送、Unityでの選択、uninstall、cleanupを検証済み |
| macOS handler dispatch | ローカルBash/file transport | 対応 | 2026-08-01にportable testでURI拒否、heartbeat、原子的配送を検証済み |
| macOS helper installer logic | macOS system commandをstub化 | 対象外 | 2026-08-01にapp構成、衝突しないbundle ID、AppleScript escape、plist操作、登録呼び出し、権限、status、uninstall、cleanupをportableに検証済み |
| macOS `open` command | OS標準のURL handler | 対応見込み | helper生成とLaunch ServicesはmacOS上で`macOSProtocolE2E.sh`による検証が必要 |
| Chromium系browser | anchorとして表示されれば通常click可能 | clientごとに確認が異なる | 手動検証が必要 |
| Codexアプリ内Browser | Browser URL policyによりprivateな`data:`検証pageがnavigation前にblock | 未到達 | 2026-08-01に確認した実行環境の制約であり、製品の検証結果ではない |
| Slack Desktop | version/workspaceごとにsecurity policyが異なる | 未確認 | 手動検証が必要 |
| Slack Web | browserのpolicyに従う | 未確認 | 手動検証が必要 |
| Jira | renderer/security policyが異なる | 未確認 | 手動検証が必要 |
| Confluence | renderer/security policyが異なる | 未確認 | 手動検証が必要 |

plain text欄では、HTTP以外のschemeが自動でlink化されない場合があります。clientが独自schemeを拒否するときは、URIをOS launcherまたは信頼できるlink対応画面へコピーしてください。将来versionでは任意のHTTPS redirect serviceを検討できますが、ローカルだけで完結するversion 1のsecurity modelには含まれません。

権限のあるprivateな場所の外へtest linkを投稿せずに残りの手動確認を完了・記録するには、[クライアント互換性チェックリスト](ClientCompatibilityChecklist-ja.md)を使用してください。
