# 変更履歴

English: [CHANGELOG.md](CHANGELOG.md)

このprojectの注目すべき変更はすべて、このファイルに記録します。

形式は[Keep a Changelog](https://keepachangelog.com/ja/1.1.0/)に基づき、このprojectは[Semantic Versioning](https://semver.org/lang/ja/spec/v2.0.0.html)に従います。

## [Unreleased]

### 追加

- Unity objectへのversion付きリンクを生成するEditor専用UPM package。
- URI scheme、安定したProject ID、receiver状態、protocol登録を扱うProject Settings。
- リンク生成、解析、処理結果、通知用の公開API。
- active selectionを対象とするAsset、GameObject、Tools menu command。
- TTL、size、重複、破損checkを備えたheartbeatと、projectごとの原子的な受信箱transport。
- Windowsのユーザー単位protocol登録・dispatch script。
- macOSのAppleScript application生成、Launch Services登録・dispatch script。
- URI検証、storage、受信箱処理、asset、sub-asset、Prefab、Scene objectのEditMode test。
- 自己cleanupを行うWindows/macOS protocol handler E2E scriptと、portableなmacOS dispatch・installer logic test。
- WindowsのOS起動からUnityでの選択までを通すEditMode E2E test、登録所有権の保護、受信箱の状態表示。
- 安全なProject ID自動生成、旧scheme解除の強制、未保存変更があるScene objectの拒否。
- architecture、URI、公開API、security、platform、client compatibilityを扱う英語・日本語のdocumentation。

### 変更

- ユーザー向けdocumentationをrootのREADME（Markdown/HTML）へ集約。
