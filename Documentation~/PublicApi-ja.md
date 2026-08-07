# 公開Editor API

English: [PublicApi.md](PublicApi.md)

すべての公開型は`UnityObjectLink` namespaceと`UnityObjectLink.Editor` assemblyに含まれます。

## `UnityObjectLinkApi`

- `TryCreateLink(Object target, out string uri, out string error)`は、1つの永続objectへのリンクを生成します。null、一時object、未保存Scene、または識別できない対象では、ユーザー向けの理由とともに`false`を返します。
- `HandleLink(string uri)`は対象を検証・解決・選択・focus・Pingします。通常はreceiverにこのmethodを呼ばせてください。
- `LinkHandled`は、明示的または受信箱経由のリンクから処理結果が生成された後に発火します。

## `UnityObjectLinkUri`

- `TryCreate`は各fieldを検証し、不変のversion 1 modelを生成します。
- `TryParse`はURIを厳密に解析し、想定するschemeとProject IDの一致も強制できます。
- `ToString`は正規化・encode済みの表現を生成します。

## `UnityObjectLinkSettings`

`UnityObjectLinkSettings.instance`は`Scheme`と`ProjectId`を公開します。`TryUpdate`はURI生成と同じ検証を行い、`ProjectSettings/UnityObjectLinkSettings.asset`へ保存します。

## `UnityObjectLinkResult`

結果には`Status`、`Succeeded`、`Message`、`Uri`、`Target`があります。status値によって、不正な入力、別project、object不在、内部failureを区別できます。

すべてのAPIはEditor専用です。利用側codeはEditor assemblyへ配置するか、`#if UNITY_EDITOR`で囲む必要があります。
