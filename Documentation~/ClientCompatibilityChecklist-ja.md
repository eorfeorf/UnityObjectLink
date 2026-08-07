# クライアント互換性チェックリスト

English: [ClientCompatibilityChecklist.md](ClientCompatibilityChecklist.md)

権限のあるprivateなtest channel、draft、page、またはprojectを使用してください。リンク処理を確認する目的だけで、他人や共有のproduction環境へtest linkを投稿しないでください。

## 準備

1. packageの`DevelopmentProject~`、または使い捨てのUnity projectを開きます。
2. **Project Settings > Unity Object Link** で識別しやすいtest用Project IDを設定し、schemeを登録します。
3. 保存済みassetを選択し、**Tools > Unity Object Link > Copy Link for Active Selection** でリンクをコピーします。
4. receiver heartbeatが **Active** であることを確認します。

## クライアントごとの確認

1. 生のURIをprivate draftまたはtest画面へ貼り付けます。
2. clientがclick可能なリンクへ変換するかを記録します。
3. clientが明示的なlink markupに対応する場合は、URIをリンク先に指定して再度確認します。
4. リンクを開き、clientまたはOSが確認promptを表示するかを記録します。
5. 許可後に想定したUnity projectがactiveになり、正確なobjectが選択・Pingされることを確認します。
6. 保存済みかつロード済みのScene objectでも繰り返し、未ロードSceneが自動で開かれないことを確認します。

client/versionごとに1行を記録します。

| 日付 | OS | クライアントとversion | 生URIをclick可能 | 明示linkをclick可能 | 確認表示 | Asset選択 | Scene動作 | 備考 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | | |

version 1の対象clientはChromium系browser、Slack Desktop/Web、Jira、Confluenceです。clientのsecurity policyはこのpackageと無関係に変わる可能性があるため、すべての結果に日付とversionを残してください。

## 後片付け

一時schemeを **Project Settings > Unity Object Link** から解除します。使い捨てprojectを使用した場合は、heartbeatが削除されるようprojectを閉じます。リンク表示だけを確認し、handler起動とUnityでの選択を確認していない状態を「対応済み」として公開しないでください。
