# FlowLog

FlowLog は、NAS上の **Git bare リポジトリ** を正本として、  
申請から承認までの履歴を **月次CSV（append-only）** で管理する軽量な申請・承認ツールです。  
ユーザーは Git を意識する必要がなく、単一の EXE アプリで操作します。

---

## 特徴

- **Git + CSV による監査性**  
  すべての申請/承認操作は append-only CSV と Git 履歴に記録され、改ざんが困難。
- **シンプルな運用**  
  NAS 上の bare リポジトリが正本。各PCは自動で `%LocalAppData%\FlowLog\repo` にクローン。
- **UI 操作のみ**  
  タブで「申請」「承認」「履歴検索」が可能。ユーザーは Git 操作不要。
- **承認は最終確定**  
  承認後のキャンセルはできず、再申請のみ可能。
- **軽量設計**  
  月次CSV + pending JSON のみを管理。ファイル肥大化を防ぐため自動ローテーション。

---

## アプリ要件

- OS: Windows 10/11  
- .NET 8 Runtime または SDK  
- 社内NASアクセス権限（読み書き可能な共有フォルダ）  
- SMTP サーバー利用権限（承認・申請通知用）

### 利用ライブラリ

- [LibGit2Sharp](https://www.nuget.org/packages/LibGit2Sharp)  
  NAS上の bare Git リポジトリとの同期を実装
- [MailKit](https://www.nuget.org/packages/MailKit)  
  承認/申請通知メール送信に利用
- System.Text.Json (.NET 標準)  
  pending JSON 管理に利用
- System.Windows.Forms (.NET 標準)  
  GUI 実装に利用

---

## ディレクトリ構成（NAS リポジトリ）

```
flowlog.git          # bare リポジトリ (NAS上)
working/
  logs/
    2025/
      2025-09.csv    # 月次CSV (監査の正本)
  requests/
    pending/         # 進行中リクエストのみ
```

- `logs/YYYY/YYYY-MM.csv`  
  - 月ごとの履歴ファイル  
  - append-only, ヘッダ固定
- `requests/pending/*.json`  
  - 承認/却下されるまでの一時ファイル  
  - 承認/却下後に削除

---

## CSV スキーマ

```csv
at,actor,action,req_id,title,requester,note
2025-09-18T11:05:12+09:00,managerA,APPROVE,REQ-20250918-0001,出張申請,user01,"OK"
```

- **at**: ISO-8601 タイムスタンプ
- **actor**: 操作者（承認者/申請者）
- **action**: CREATE / APPROVE / REJECT
- **req_id**: リクエストID（`REQ-YYYYMMDD-hhmmss-rand`）
- **title**: タイトル
- **requester**: 申請者ID
- **note**: 任意コメント

---

## 動作フロー

### 申請 (CREATE)
1. pending JSON を作成
2. 月次 CSV に CREATE 行を追加
3. Git Commit/Push
4. 承認者へメール通知

### 承認 (APPROVE) / 却下 (REJECT)
1. 月次 CSV に APPROVE/REJECT 行を追加
2. pending JSON を削除
3. Git Commit/Push
4. 申請者へメール通知

---

## 履歴検索

- 「履歴検索」タブで月次CSVを全文検索可能
- フィルタ文字列（req_id, actor, title 等）に一致する行を抽出

---

## 運用ルール

- **ブランチ**: `main` 固定。Fast-Forward only。
- **承認確定**: 一度承認したらキャンセル不可。再申請で対応。
- **競合解消**: Push 失敗時は自動で Pull → 再追記 → Push をリトライ。
- **月次ローテーション**: 月が変われば次回操作時に新規 CSV を自動作成。
- **監査性**: 改ざん禁止。履歴は CSV + Git commit log の両方に残る。

---

## 初期セットアップ

1. **NASに bare リポジトリを作成**  
   ```bash
   git init --bare --shared=group \\nas\git\flowlog.git
   ```
   （EXE から自動作成も可能）

2. **ユーザー初回起動時**  
   - Remote パス（NAS上の bare リポジトリ）
   - ユーザー名（actor）
   - メールアドレス  
   → 一度入力すると `%AppData%\FlowLog\config.json` に保存され、以後自動利用。

3. **ローカルリポジトリ**  
   自動で `%LocalAppData%\FlowLog\repo` にクローンされ、以後の操作はすべてここで処理。

---

## メール通知

- **ライブラリ**: MailKit  
- **サーバー**: `smtp.example.co.jp` (例)  
- 承認/申請完了時に宛先へプレーンテキスト通知

---

## 想定規模と拡張

- 想定: 100件/日 → 年間 ≈ 36,500件 → 月次 CSV ≈ 3,000行  
- 年単位でも数MB程度に収まり、Git 運用は十分軽量。  
- 件数が増えた場合は「月次分割 + gzip 圧縮」で対応可能。  

---

## ライセンス

LICENSE.txt参照
