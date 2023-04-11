DynamicBoneSetupHelper
==============================

# 1. これは何ですか？

VRChat のアバターに DynamicBone を設定する際の支援スクリプトです。UnityEditor の拡張として動作します。


# 2. 前提

Unity           2018.4.20f1
DynamicBone     1.2.1


# 3. 導入

DynamicBoneSetupHelper は unitypackage で提供されています。導入先の Unity プロジェクトに import してください。


# 4. 使い方

## 4-1. 基本操作

ツールウィンドウは、Unity メニューの『Tools/DynamicBone Setup Helper』から開くことができます。
開く際に Hierarchy にてアバターが選択されているならば、そのオブジェクトを編集対象として扱うことができます。

Avatar Root に VRC_AvatarDescriptor の付いたオブジェクトを指定してください。
指定したオブジェクト配下に “Armature” という名前のオブジェクトがある場合、Armature も同時に設定されます。
自動設定されない場合は Armature にもオブジェクトを指定してください。


## 4-2. Verify

DynamicBoneの設定をチェックしてConsoleログに出力します。次のような設定を見つけた場合にはログに警告が出力されます。

- Root, Colliders, Exclusions に None(null) が含まれている
- Root, Colliders, Exclusions, ReferenceObject に Armature 外のオブジェクトが指定されている
- 複数の DynamicBone がひとつのボーンを揺らしている
- リーフボーンがなく、EndOffset も EndLength も未設定
- どの DynamicBone からも使われていない DynamicBoneCollider


## 4-3. 参照の再設定

Avatar Root 配下の全ての DynamicBone について、参照先の Transform が Armature 外にある場合、同名のボーンを検索して参照を付け替えます。
同名のボーンが見つからなかった場合は何もしません。
この機能はDynamicBone の値コピーなどによってコピー元の Transform を参照し続けている場合に、コピー先の参照へと付け替えるための機能です。


## 4-4. アタッチ先オブジェクトの変更

DynamicBone のコンポーネントを付ける先の GameObject を変更することができます。
このツールでは以下のパターンに対応しています。
この機能はパターンの相互変換に対応している他、変更する DynamicBone は Avatar Root から検索されるため、他のオブジェクトにアタッチされている DynamicBone を対応パターンへと整理することも可能です。

### 揺らすボーン自体

DynamicBone の Root に指定されている(つまり揺らす対象の)ボーンオブジェクトにコンポーネントをアタッチします。

### 独立したオブジェクト

Hierarchy に “DBone” という名称のオブジェクトを作成し、さらに下に揺らす対象と同名のオブジェクトを作成し、それにコンポーネントをアタッチします。

### Avatarオブジェクト

Avatar Root に指定したオブジェクトに、全ての DynamicBone コンポーネントをアタッチします。

### Armatureオブジェクト

Armature に指定したオブジェクトに、全ての DynamicBone コンポーネントをアタッチします。


## 4-5. DynamicBoneコンポーネントの削除

Avatar Root 配下の DynamicBone コンポーネント (Colliderを含む) を全て削除します。


# 5. LICENSE および CopyRight

The MIT License

Copyright 2020 whiteflare.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
