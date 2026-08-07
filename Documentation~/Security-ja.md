# セキュリティモデル

English: [Security.md](Security.md)

独自URIの起動は信頼できない入力です。Unity Object LinkはOS handlerとUnity receiverの両方を検証境界として扱います。

## 保証事項

- `select` actionとURI version 1だけを受理します。
- 許可するparameterは`v`、`project`、`object`だけです。欠落、重複、未知のparameterは安全側に倒して拒否します。
- 入力長、percent encoding、scheme構文、Project ID構文を制限し、二重に検証します。
- Project IDにはseparatorや`..`を含められないため、固定のinstance directory外へ移動できません。
- URIからexecutable、command、script、method、ローカルファイルpathを指定することはできません。
- OS handlerが書き込むのは、Windowsでは`%LOCALAPPDATA%\UnityObjectLink`、macOSでは`~/Library/Application Support/UnityObjectLink`配下だけです。
- 正確に一致するscheme/Project IDのheartbeatが15秒以内の場合だけrequestを受理します。
- 一時ファイルからrequestへのrenameにより、Unityが書き込み途中のrequestを読むことを防ぎます。
- Unityは空、古い（60秒超）、巨大、重複、不正なUTF-8、不正なURIのrequestを拒否し、その後に削除を試みます。
- packageがリンクに応じてSceneを開いたり保存したりすることはありません。

## 登録の所有権

Windowsのuninstallは、そのcommandがUnity Object Link handlerを指している場合だけ`HKCU\Software\Classes\<scheme>`を削除します。別applicationが所有するschemeは削除しません。macOSでは、検証済みschemeごとに固定のproduct directory下へ生成したhelperを1つ保存し、そのbundleだけを登録解除します。

## プライバシー

生成リンクにはProject IDとUnityの`GlobalObjectId`が含まれます。端末path、ユーザー名、asset名、Scene名、source内容は含まれません。heartbeatにはscheme、Project ID、process ID、version、timestampが含まれます。requestは現在のユーザーprofile内だけに保持され、処理後に削除されます。

## プラットフォームに残る挙動

OSやclient applicationは確認promptを表示したり、独自schemeの起動を拒否したりする場合があります。また、OSのforeground制限によってUnityが最前面にならない場合があります。配送に成功していれば、packageは該当Unity windowをfocusし、objectを選択します。
