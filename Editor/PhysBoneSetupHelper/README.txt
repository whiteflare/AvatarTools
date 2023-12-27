PhysBoneSetupHelper
==============================

# 1. これは何ですか？

VRChat のアバターに PhysBone を設定する際の支援スクリプトです。UnityEditor の拡張として動作します。


# 2. 使い方

## 2-1. 基本操作

ツールウィンドウは、Unity メニューの『Tools/whiteflare/PhysBone Setup Helper』から開くことができます。
開く際に Hierarchy にてアバターが選択されているならば、そのオブジェクトを編集対象として扱うことができます。

Avatar Root に VRC_AvatarDescriptor の付いたオブジェクトを指定してください。
指定したオブジェクト配下に “Armature” という名前のオブジェクトがある場合、Armature も同時に設定されます。
自動設定されない場合は Armature にもオブジェクトを指定してください。


## 2-2. Verify

PhysBoneの設定をチェックしてConsoleログに出力します。次のような設定を見つけた場合にはログに警告が出力されます。

- Colliders, IgnoreTransforms に None(null) が含まれている
- Root, Colliders, IgnoreTransforms に Armature 外のオブジェクトが指定されている
- 複数の PhysBone がひとつのボーンを揺らしている
- リーフボーンがなく EndPoint も未設定
- どの PhysBone からも使われていない PhysBoneCollider


## 2-3. 参照の再設定

Avatar Root 配下の全ての PhysBone について、参照先の Transform が Armature 外にある場合、同名のボーンを検索して参照を付け替えます。
同名のボーンが見つからなかった場合は何もしません。
この機能はPhysBone の値コピーなどによってコピー元の Transform を参照し続けている場合に、コピー先の参照へと付け替えるための機能です。


## 2-4. アタッチ先オブジェクトの変更

PhysBone のコンポーネントを付ける先の GameObject を変更することができます。

### 揺らすボーン自体

PhysBone の Root に指定されている(つまり揺らす対象の)ボーンオブジェクトにコンポーネントをアタッチします。

### 独立したオブジェクト

Hierarchy に “PhysBone” という名称のオブジェクトを作成し、さらに下に揺らす対象と同名のオブジェクトを作成し、それにコンポーネントをアタッチします。


# 3. LICENSE および CopyRight

The MIT License

Copyright 2023-2024 whiteflare.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
