# URI仕様

English: [UriSpecification.md](UriSpecification.md)

## Version 1

```text
<scheme>://select?v=1&project=<project-id>&object=<percent-encoded-global-object-id>
```

生成時の順序は`v`、`project`、`object`です。parserは任意の順序を受理しますが、各parameterが正確に1回ずつ必要であり、未知のparameterはすべて拒否します。

## フィールド

- `scheme`: RFC 3986のscheme構文に従う1～32文字のASCII文字列です。小文字へ正規化します。
- action/host: 正確に`select`である必要があり、大文字・小文字を区別せず比較します。
- `v`: 10進数の`1`だけを許可します。
- `project`: 1～64文字のASCII英字、数字、`.`、`_`、`-`を許可します。先頭は英字または数字である必要があり、`..`は使用できません。
- `object`: percent encodeされたUnityの`GlobalObjectId_V1-...`文字列です。decode後の長さは最大512文字です。

URI全体は、Unityでは8192 UTF-16文字、各handlerでは8192 shell文字に制限されます。control character、不正なpercent escape、任意の`/`以外のpath、user information、port、fragmentは拒否します。

## 一致条件

UnityはURI schemeと現在の設定を大文字・小文字を区別せず比較し、Project IDは完全一致で比較します。不一致時に別の起動中projectへfallbackすることはありません。version 1が提供するのは選択だけです。command、path、method名、任意のpayloadを運ぶことはできません。

## Encode例

```text
unity-object-link://select?v=1&project=sample-project&object=GlobalObjectId_V1-1-0123456789abcdef0123456789abcdef-123-0
```

現在の`GlobalObjectId`がURI-safeな文字だけで構成されている場合も、APIはparameter値に常に`Uri.EscapeDataString`を使用します。
